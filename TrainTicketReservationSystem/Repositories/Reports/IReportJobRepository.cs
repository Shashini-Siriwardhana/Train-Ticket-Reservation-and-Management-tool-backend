using TrainTicketReservationSystem.Models.Entities;

namespace TrainTicketReservationSystem.Repositories.Reports
{
  public interface IReportJobRepository
  {
    Task AddAsync(
        ReportJob reportJob,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a read-only report job.
    /// Used by status endpoints.
    /// </summary>
    Task<ReportJob?> GetByIdAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked report job.
    /// Used when the worker must update status or progress.
    /// </summary>
    Task<ReportJob?> GetForUpdateAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns jobs that were queued or processing when
    /// the application stopped unexpectedly.
    /// </summary>
    Task<IReadOnlyList<ReportJob>> GetRecoverableJobsAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
  }
}