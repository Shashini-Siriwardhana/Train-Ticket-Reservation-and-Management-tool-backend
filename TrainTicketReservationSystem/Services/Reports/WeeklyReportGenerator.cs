using System.Diagnostics;
using System.Text.Json;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Models.Reports;
using TrainTicketReservationSystem.Repositories.Bookings;
using TrainTicketReservationSystem.Repositories.SpecialRequests;

namespace TrainTicketReservationSystem.Services.Reports
{
  public sealed class WeeklyReportGenerator
      : IWeeklyReportGenerator
  {
    private static readonly JsonSerializerOptions
        JsonOptions =
            new()
            {
              WriteIndented = true,
              PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };

    private readonly IBookingRepository
        _bookingRepository;

    private readonly ISpecialRequestRepository
        _specialRequestRepository;

    private readonly IConfiguration
        _configuration;

    private readonly ILogger<WeeklyReportGenerator>
        _logger;

    public WeeklyReportGenerator(
        IBookingRepository bookingRepository,
        ISpecialRequestRepository specialRequestRepository,
        IConfiguration configuration,
        ILogger<WeeklyReportGenerator> logger)
    {
      _bookingRepository =
          bookingRepository;

      _specialRequestRepository =
          specialRequestRepository;

      _configuration =
          configuration;

      _logger =
          logger;
    }

    public async Task<WeeklyReportGenerationResult>
        GenerateAsync(
            Guid reportJobId,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
    {
      if (reportJobId == Guid.Empty)
      {
        throw new ArgumentException(
            "A valid report job ID is required.",
            nameof(reportJobId));
      }

      ValidateDateRange(
          fromDate,
          toDate);

      var stopwatch =
          Stopwatch.StartNew();

      _logger.LogInformation(
          "Starting weekly report generation for job {ReportJobId}.",
          reportJobId);

      var bookingsTask =
          _bookingRepository.GetForPeriodAsync(
              fromDate,
              toDate,
              cancellationToken);

      var specialRequestsTask =
          _specialRequestRepository.GetAllRequests(
              cancellationToken);

      await Task.WhenAll(
          bookingsTask,
          specialRequestsTask);

      var bookings =
          await bookingsTask;

      var allSpecialRequests =
          await specialRequestsTask;

      var bookingIds =
          bookings
              .Select(booking =>
                  booking.BookingId)
              .ToHashSet();

      var relevantSpecialRequests =
          allSpecialRequests
              .Where(request =>
                  bookingIds.Contains(
                      request.BookingId))
              .ToList();

      var reportDocument =
          CreateReportDocument(
              reportJobId,
              fromDate,
              toDate,
              bookings,
              relevantSpecialRequests);

      var outputDirectory =
          GetOutputDirectory();

      Directory.CreateDirectory(
          outputDirectory);

      var outputFileName =
          $"weekly-report-{reportJobId:N}.json";

      var finalFilePath =
          Path.Combine(
              outputDirectory,
              outputFileName);

      var temporaryFilePath =
          finalFilePath + ".tmp";

      try
      {
        await WriteReportFileAsync(
            temporaryFilePath,
            reportDocument,
            cancellationToken);

        File.Move(
            temporaryFilePath,
            finalFilePath,
            overwrite: true);
      }
      catch
      {
        DeleteTemporaryFileIfPresent(
            temporaryFilePath);

        throw;
      }

      stopwatch.Stop();

      var fileInfo =
          new FileInfo(finalFilePath);

      _logger.LogInformation(
          "Weekly report job {ReportJobId} generated {BookingCount} bookings and {SpecialRequestCount} special requests in {ProcessingMilliseconds} ms.",
          reportJobId,
          bookings.Count,
          relevantSpecialRequests.Count,
          stopwatch.ElapsedMilliseconds);

      return new WeeklyReportGenerationResult
      {
        OutputFileName =
              outputFileName,

        ProcessingMilliseconds =
              stopwatch.ElapsedMilliseconds,

        BookingCount =
              bookings.Count,

        SpecialRequestCount =
              relevantSpecialRequests.Count,

        FileSizeBytes =
              fileInfo.Exists
                  ? fileInfo.Length
                  : 0
      };
    }

    private static WeeklyReportDocument
        CreateReportDocument(
            Guid reportJobId,
            DateTime? fromDate,
            DateTime? toDate,
            IReadOnlyList<Booking> bookings,
            IReadOnlyList<
                Models.SpecialRequests.SpecialRequestRecord>
                specialRequests)
    {
      var routeSummaries =
          bookings
              .GroupBy(booking =>
                  GetRouteName(booking))
              .OrderBy(group =>
                  group.Key)
              .Select(group =>
                  new RouteReportSummary
                  {
                    Route =
                          group.Key,

                    BookingCount =
                          group.Count(),

                    Revenue =
                          group.Sum(booking =>
                              booking.Price),

                    RecurringBookingCount =
                          group.Count(booking =>
                              booking.IsRecurring),

                    OneTimeBookingCount =
                          group.Count(booking =>
                              !booking.IsRecurring)
                  })
              .ToList();

      var statusSummaries =
          specialRequests
              .GroupBy(request =>
                  string.IsNullOrWhiteSpace(
                      request.Status)
                      ? "Unknown"
                      : request.Status.Trim(),
                  StringComparer.OrdinalIgnoreCase)
              .OrderBy(group =>
                  group.Key)
              .Select(group =>
                  new SpecialRequestStatusSummary
                  {
                    Status =
                          group.Key,

                    Count =
                          group.Count()
                  })
              .ToList();

      return new WeeklyReportDocument
      {
        ReportJobId =
              reportJobId,

        FromDate =
              fromDate?.Date,

        ToDate =
              toDate?.Date,

        GeneratedAtUtc =
              DateTimeOffset.UtcNow,

        TotalBookings =
              bookings.Count,

        RecurringBookings =
              bookings.Count(booking =>
                  booking.IsRecurring),

        OneTimeBookings =
              bookings.Count(booking =>
                  !booking.IsRecurring),

        TotalRevenue =
              bookings.Sum(booking =>
                  booking.Price),

        TotalSpecialRequests =
              specialRequests.Count,

        RouteSummaries =
              routeSummaries,

        SpecialRequestStatusSummaries =
              statusSummaries
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

    private static async Task WriteReportFileAsync(
        string filePath,
        WeeklyReportDocument report,
        CancellationToken cancellationToken)
    {
      await using var fileStream =
          new FileStream(
              filePath,
              FileMode.Create,
              FileAccess.Write,
              FileShare.None,
              bufferSize: 81920,
              options:
                  FileOptions.Asynchronous |
                  FileOptions.SequentialScan);

      await JsonSerializer.SerializeAsync(
          fileStream,
          report,
          JsonOptions,
          cancellationToken);

      await fileStream.FlushAsync(
          cancellationToken);
    }

    private static string GetRouteName(
        Booking booking)
    {
      if (!string.IsNullOrWhiteSpace(
              booking.Route))
      {
        return booking.Route.Trim();
      }

      return
          $"{booking.DepartureStation} → " +
          $"{booking.DestinationStation}";
    }

    private static void ValidateDateRange(
        DateTime? fromDate,
        DateTime? toDate)
    {
      if (fromDate.HasValue &&
          toDate.HasValue &&
          fromDate.Value.Date >
          toDate.Value.Date)
      {
        throw new ArgumentException(
            "The From Date cannot be later than the To Date.");
      }

      if (fromDate.HasValue &&
          toDate.HasValue &&
          toDate.Value.Date >
          fromDate.Value.Date.AddYears(5))
      {
        throw new ArgumentException(
            "The report period cannot exceed five years.");
      }
    }

    private static void
        DeleteTemporaryFileIfPresent(
            string temporaryFilePath)
    {
      try
      {
        if (File.Exists(
                temporaryFilePath))
        {
          File.Delete(
              temporaryFilePath);
        }
      }
      catch
      {
      }
    }
  }
}