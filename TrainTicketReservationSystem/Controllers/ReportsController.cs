using Microsoft.AspNetCore.Mvc;
using TrainTicketReservationSystem.BackgroundJobs.Reports;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Models.Reports;
using TrainTicketReservationSystem.Repositories.Reports;

namespace TrainTicketReservationSystem.Controllers
{
  [ApiController]
  [Route("api/reports")]
  public sealed class ReportsController : ControllerBase
  {
    private readonly IReportJobRepository
        _reportJobRepository;

    private readonly IReportJobQueue
        _reportJobQueue;

    private readonly IConfiguration
        _configuration;

    private readonly ILogger<ReportsController>
        _logger;

    public ReportsController(
        IReportJobRepository reportJobRepository,
        IReportJobQueue reportJobQueue,
        IConfiguration configuration,
        ILogger<ReportsController> logger)
    {
      _reportJobRepository =
          reportJobRepository;

      _reportJobQueue =
          reportJobQueue;

      _configuration =
          configuration;

      _logger =
          logger;
    }

    /// <summary>
    /// Creates a queued weekly report job.
    /// </summary>
    [HttpPost("weekly")]
    [ProducesResponseType(
        typeof(ReportJobResponse),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult>
        CreateWeeklyReport(
            CreateWeeklyReportRequest request,
            CancellationToken cancellationToken)
    {
      ValidateDateRange(request);

      if (!ModelState.IsValid)
      {
        return ValidationProblem(ModelState);
      }

      var reportJob =
          new ReportJob
          {
            ReportJobId =
                  Guid.NewGuid(),

            FromDate =
                  request.FromDate!.Value.Date,

            ToDate =
                  request.ToDate!.Value.Date,

            Status =
                  ReportJobStatus.Queued,

            ProgressPercentage = 0,

            CreatedAtUtc =
                  DateTimeOffset.UtcNow
          };

      /*
       * Save the SQL job record before adding it to
       * the in-memory queue.
       *
       * This means the worker can always load the job
       * after receiving its ID.
       */
      await _reportJobRepository.AddAsync(
          reportJob,
          cancellationToken);

      await _reportJobRepository.SaveChangesAsync(
          cancellationToken);

      /*
       * TryQueue returns immediately.
       * The HTTP request does not wait for report
       * generation.
       */
      var queued =
          _reportJobQueue.TryQueue(
              reportJob.ReportJobId);

      if (!queued)
      {
        reportJob.Status =
            ReportJobStatus.Failed;

        reportJob.ErrorMessage =
            "The report queue is currently full. " +
            "Please try again shortly.";

        reportJob.CompletedAtUtc =
            DateTimeOffset.UtcNow;

        await _reportJobRepository.SaveChangesAsync(
            cancellationToken);

        _logger.LogWarning(
            "Report job {ReportJobId} could not be queued because the report queue is full.",
            reportJob.ReportJobId);

        Response.Headers["Retry-After"] = "10";

        var problem =
            new ProblemDetails
            {
              Title =
                    "The report queue is full.",

              Detail =
                    "The report could not be queued. " +
                    "Please try again shortly.",

              Status =
                    StatusCodes
                        .Status503ServiceUnavailable,

              Instance =
                    HttpContext.Request.Path
            };

        problem.Extensions["reportJobId"] =
            reportJob.ReportJobId;

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            problem);
      }

      _logger.LogInformation(
          "Report job {ReportJobId} was queued for period {FromDate} to {ToDate}.",
          reportJob.ReportJobId,
          reportJob.FromDate,
          reportJob.ToDate);

      var response =
          MapToResponse(reportJob);

      /*
       * Returns:
       * 202 Accepted
       *
       * The Location header points to:
       * GET /api/reports/jobs/{jobId}
       */
      return AcceptedAtAction(
          nameof(GetReportJob),
          new
          {
            jobId = reportJob.ReportJobId
          },
          response);
    }

    /// <summary>
    /// Gets the current state of a report job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(
        typeof(ReportJobResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult>
        GetReportJob(
            Guid jobId,
            CancellationToken cancellationToken)
    {
      var reportJob =
          await _reportJobRepository.GetByIdAsync(
              jobId,
              cancellationToken);

      if (reportJob is null)
      {
        return NotFound(
            new ProblemDetails
            {
              Title =
                    "Report job not found.",

              Detail =
                    $"No report job exists with ID {jobId}.",

              Status =
                    StatusCodes.Status404NotFound,

              Instance =
                    HttpContext.Request.Path
            });
      }

      return Ok(
          MapToResponse(reportJob));
    }

    /// <summary>
    /// Downloads a completed report JSON file.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/download")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult>
        DownloadReport(
            Guid jobId,
            CancellationToken cancellationToken)
    {
      var reportJob =
          await _reportJobRepository.GetByIdAsync(
              jobId,
              cancellationToken);

      if (reportJob is null)
      {
        return NotFound(
            new ProblemDetails
            {
              Title =
                    "Report job not found.",

              Detail =
                    $"No report job exists with ID {jobId}.",

              Status =
                    StatusCodes.Status404NotFound,

              Instance =
                    HttpContext.Request.Path
            });
      }

      if (reportJob.Status !=
              ReportJobStatus.Completed ||
          string.IsNullOrWhiteSpace(
              reportJob.OutputFileName))
      {
        return Conflict(
            new ProblemDetails
            {
              Title =
                    "The report is not ready.",

              Detail =
                    $"The report status is " +
                    $"{reportJob.Status}.",

              Status =
                    StatusCodes.Status409Conflict,

              Instance =
                    HttpContext.Request.Path
            });
      }

      /*
       * Path.GetFileName prevents directory traversal
       * through a filename stored in the database.
       */
      var safeFileName =
          Path.GetFileName(
              reportJob.OutputFileName);

      if (!string.Equals(
              safeFileName,
              reportJob.OutputFileName,
              StringComparison.Ordinal))
      {
        _logger.LogError(
            "Report job {ReportJobId} contains an invalid output filename.",
            reportJob.ReportJobId);

        return Problem(
            title:
                "The report filename is invalid.",

            statusCode:
                StatusCodes
                    .Status500InternalServerError);
      }

      var outputDirectory =
          GetOutputDirectory();

      var fullFilePath =
          Path.Combine(
              outputDirectory,
              safeFileName);

      if (!System.IO.File.Exists(
              fullFilePath))
      {
        _logger.LogWarning(
            "The output file for report job {ReportJobId} was not found at {FilePath}.",
            reportJob.ReportJobId,
            fullFilePath);

        return NotFound(
            new ProblemDetails
            {
              Title =
                    "Report file not found.",

              Detail =
                    "The report job completed, but its " +
                    "output file is unavailable.",

              Status =
                    StatusCodes.Status404NotFound,

              Instance =
                    HttpContext.Request.Path
            });
      }

      var fileStream =
          new FileStream(
              fullFilePath,
              FileMode.Open,
              FileAccess.Read,
              FileShare.Read,
              bufferSize: 81920,
              options:
                  FileOptions.Asynchronous |
                  FileOptions.SequentialScan);

      return File(
          fileStream,
          "application/json",
          safeFileName);
    }

    private void ValidateDateRange(
        CreateWeeklyReportRequest request)
    {
      if (!request.FromDate.HasValue ||
          !request.ToDate.HasValue)
      {
        return;
      }

      var fromDate =
          request.FromDate.Value.Date;

      var toDate =
          request.ToDate.Value.Date;

      if (fromDate > toDate)
      {
        ModelState.AddModelError(
            nameof(request.ToDate),
            "To Date cannot be earlier than From Date.");
      }

      if (toDate >
          fromDate.AddYears(5))
      {
        ModelState.AddModelError(
            nameof(request.ToDate),
            "The report period cannot exceed five years.");
      }
    }

    private ReportJobResponse MapToResponse(
        ReportJob reportJob)
    {
      var canDownload =
          reportJob.Status ==
              ReportJobStatus.Completed &&
          !string.IsNullOrWhiteSpace(
              reportJob.OutputFileName);

      return new ReportJobResponse
      {
        ReportJobId =
              reportJob.ReportJobId,

        FromDate =
              reportJob.FromDate,

        ToDate =
              reportJob.ToDate,

        Status =
              reportJob.Status.ToString(),

        ProgressPercentage =
              reportJob.ProgressPercentage,

        CreatedAtUtc =
              reportJob.CreatedAtUtc,

        StartedAtUtc =
              reportJob.StartedAtUtc,

        CompletedAtUtc =
              reportJob.CompletedAtUtc,

        OutputFileName =
              reportJob.OutputFileName,

        ErrorMessage =
              reportJob.ErrorMessage,

        ProcessingMilliseconds =
              reportJob.ProcessingMilliseconds,

        CanDownload =
              canDownload,

        StatusUrl =
              Url.Action(
                  nameof(GetReportJob),
                  "Reports",
                  new
                  {
                    jobId =
                          reportJob.ReportJobId
                  }),

        DownloadUrl =
              canDownload
                  ? Url.Action(
                      nameof(DownloadReport),
                      "Reports",
                      new
                      {
                        jobId =
                              reportJob.ReportJobId
                      })
                  : null
      };
    }

    private string GetOutputDirectory()
    {
      var configuredDirectory =
          _configuration[
              "Reports:OutputDirectory"];

      if (string.IsNullOrWhiteSpace(
              configuredDirectory))
      {
        configuredDirectory =
            Path.Combine(
                "App_Data",
                "reports");
      }

      if (Path.IsPathRooted(
              configuredDirectory))
      {
        return configuredDirectory;
      }

      return Path.Combine(
          AppContext.BaseDirectory,
          configuredDirectory);
    }
  }
}