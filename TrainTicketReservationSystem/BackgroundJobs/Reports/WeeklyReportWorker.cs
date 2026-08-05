using Microsoft.Extensions.DependencyInjection;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Repositories.Reports;
using TrainTicketReservationSystem.Services.Reports;

namespace TrainTicketReservationSystem
    .BackgroundJobs.Reports
{
  public sealed class WeeklyReportWorker
      : BackgroundService
  {
    private readonly IReportJobQueue _reportJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeeklyReportWorker> _logger;

    public WeeklyReportWorker(
        IReportJobQueue reportJobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<WeeklyReportWorker> logger)
    {
      _reportJobQueue = reportJobQueue;
      _scopeFactory = scopeFactory;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
      _logger.LogInformation(
          "Weekly report background worker started.");

      try
      {
        /*
         * Reprocess jobs that were Queued or Processing
         * when the application previously stopped.
         */
        await RecoverIncompleteJobsAsync(
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
          Guid reportJobId;

          try
          {
            /*
             * This waits asynchronously.
             * It does not block a thread while the
             * queue is empty.
             */
            reportJobId =
                await _reportJobQueue.DequeueAsync(
                    stoppingToken);
          }
          catch (OperationCanceledException)
              when (stoppingToken
                  .IsCancellationRequested)
          {
            break;
          }

          try
          {
            await ProcessJobSafelyAsync(
                reportJobId,
                stoppingToken);
          }
          catch (OperationCanceledException)
              when (stoppingToken
                  .IsCancellationRequested)
          {
            /*
             * Keep the current job as Processing.
             * On the next application startup it will
             * be found and recovered.
             */
            break;
          }
        }
      }
      finally
      {
        _logger.LogInformation(
            "Weekly report background worker stopped.");
      }
    }

    private async Task RecoverIncompleteJobsAsync(
        CancellationToken cancellationToken)
    {
      await using var scope =
          _scopeFactory.CreateAsyncScope();

      var repository =
          scope.ServiceProvider
              .GetRequiredService<
                  IReportJobRepository>();

      var recoverableJobs =
          await repository.GetRecoverableJobsAsync(
              cancellationToken);

      if (recoverableJobs.Count == 0)
      {
        _logger.LogInformation(
            "No incomplete report jobs were found.");
        return;
      }

      _logger.LogWarning(
          "Recovering {ReportJobCount} incomplete report jobs.",
          recoverableJobs.Count);

      /*
       * Process recovered jobs directly before beginning
       * normal queue consumption.
       *
       * This avoids trying to place more recovered jobs
       * into the bounded channel than it can hold.
       */
      foreach (var reportJob in recoverableJobs)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          await ProcessJobSafelyAsync(
              reportJob.ReportJobId,
              cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
          throw;
        }
      }
    }

    private async Task ProcessJobSafelyAsync(
        Guid reportJobId,
        CancellationToken cancellationToken)
    {
      try
      {
        await ProcessJobAsync(
            reportJobId,
            cancellationToken);
      }
      catch (OperationCanceledException)
          when (cancellationToken
              .IsCancellationRequested)
      {
        _logger.LogInformation(
            "Report job {ReportJobId} was interrupted because the application is stopping.",
            reportJobId);

        throw;
      }
      catch (Exception exception)
      {
        /*
         * Log the complete technical exception.
         * Do not expose this stack trace to the MVC user.
         */
        _logger.LogError(
            exception,
            "Report job {ReportJobId} failed.",
            reportJobId);

        await TryMarkJobAsFailedAsync(
            reportJobId);
      }
    }

    private async Task ProcessJobAsync(
        Guid reportJobId,
        CancellationToken cancellationToken)
    {
      /*
       * A hosted service is registered as a singleton.
       * Create an independent scope for each job so the
       * repositories receive a fresh DbContext.
       */
      await using var scope =
          _scopeFactory.CreateAsyncScope();

      var reportJobRepository =
          scope.ServiceProvider
              .GetRequiredService<
                  IReportJobRepository>();

      var reportGenerator =
          scope.ServiceProvider
              .GetRequiredService<
                  IWeeklyReportGenerator>();

      var reportJob =
          await reportJobRepository
              .GetForUpdateAsync(
                  reportJobId,
                  cancellationToken);

      if (reportJob is null)
      {
        _logger.LogWarning(
            "Report job {ReportJobId} was not found.",
            reportJobId);

        return;
      }

      /*
       * Completed, failed and cancelled jobs must not be
       * processed again if the same ID enters the queue
       * more than once.
       */
      if (reportJob.Status is
          ReportJobStatus.Completed or
          ReportJobStatus.Failed or
          ReportJobStatus.Cancelled)
      {
        _logger.LogWarning(
            "Report job {ReportJobId} was skipped because its status is {ReportJobStatus}.",
            reportJobId,
            reportJob.Status);

        return;
      }

      reportJob.Status =
          ReportJobStatus.Processing;

      reportJob.ProgressPercentage = 10;

      reportJob.StartedAtUtc =
          DateTimeOffset.UtcNow;

      reportJob.CompletedAtUtc = null;
      reportJob.OutputFileName = null;
      reportJob.ErrorMessage = null;
      reportJob.ProcessingMilliseconds = null;

      await reportJobRepository.SaveChangesAsync(
          cancellationToken);

      _logger.LogInformation(
          "Processing report job {ReportJobId} for period {FromDate} to {ToDate}.",
          reportJob.ReportJobId,
          reportJob.FromDate,
          reportJob.ToDate);

      /*
       * WeeklyReportGenerator performs the SQL query,
       * XML read and asynchronous file write.
       */
      var generationResult =
          await reportGenerator.GenerateAsync(
              reportJob.ReportJobId,
              reportJob.FromDate,
              reportJob.ToDate,
              cancellationToken);

      reportJob.Status =
          ReportJobStatus.Completed;

      reportJob.ProgressPercentage = 100;

      reportJob.CompletedAtUtc =
          DateTimeOffset.UtcNow;

      reportJob.OutputFileName =
          generationResult.OutputFileName;

      reportJob.ProcessingMilliseconds =
          generationResult
              .ProcessingMilliseconds;

      reportJob.ErrorMessage = null;

      await reportJobRepository.SaveChangesAsync(
          cancellationToken);

      _logger.LogInformation(
          "Report job {ReportJobId} completed in {ProcessingMilliseconds} ms. Output file: {OutputFileName}.",
          reportJob.ReportJobId,
          reportJob.ProcessingMilliseconds,
          reportJob.OutputFileName);
    }

    private async Task TryMarkJobAsFailedAsync(
        Guid reportJobId)
    {
      try
      {
        /*
         * Use a new scope. The scope that encountered
         * the failure may contain a DbContext that is
         * no longer safe to reuse.
         */
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<
                    IReportJobRepository>();

        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(15));

        var reportJob =
            await repository.GetForUpdateAsync(
                reportJobId,
                timeout.Token);

        if (reportJob is null)
        {
          return;
        }

        /*
         * Do not overwrite a successfully completed job
         * if an unrelated operation failed afterward.
         */
        if (reportJob.Status ==
            ReportJobStatus.Completed)
        {
          return;
        }

        reportJob.Status =
            ReportJobStatus.Failed;

        reportJob.ProgressPercentage = 0;

        reportJob.CompletedAtUtc =
            DateTimeOffset.UtcNow;

        reportJob.OutputFileName = null;

        /*
         * Keep the database message safe for display.
         * Detailed technical information remains in logs.
         */
        reportJob.ErrorMessage =
            "Report generation failed. Please try again.";

        await repository.SaveChangesAsync(
            timeout.Token);
      }
      catch (Exception exception)
      {
        _logger.LogError(
            exception,
            "Report job {ReportJobId} failed and its Failed status could not be saved.",
            reportJobId);
      }
    }
  }
}