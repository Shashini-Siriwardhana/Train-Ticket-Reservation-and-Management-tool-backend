using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Models.Entities;

namespace TrainTicketReservationSystem.Repositories.Reports
{
  public sealed class EfReportJobRepository
      : IReportJobRepository
  {
    private readonly ApplicationDBContext _dbContext;

    public EfReportJobRepository(
        ApplicationDBContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task AddAsync(
        ReportJob reportJob,
        CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(reportJob);

      await _dbContext.ReportJobs.AddAsync(
          reportJob,
          cancellationToken);
    }

    public async Task<ReportJob?> GetByIdAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default)
    {
      if (reportJobId == Guid.Empty)
      {
        return null;
      }

      return await _dbContext.ReportJobs
          .AsNoTracking()
          .SingleOrDefaultAsync(
              job =>
                  job.ReportJobId == reportJobId,
              cancellationToken);
    }

    public async Task<ReportJob?> GetForUpdateAsync(
        Guid reportJobId,
        CancellationToken cancellationToken = default)
    {
      if (reportJobId == Guid.Empty)
      {
        return null;
      }

      /*
       * Do not use AsNoTracking here.
       * The background worker needs EF Core to track
       * changes to status, progress and output filename.
       */
      return await _dbContext.ReportJobs
          .SingleOrDefaultAsync(
              job =>
                  job.ReportJobId == reportJobId,
              cancellationToken);
    }

    public async Task<IReadOnlyList<ReportJob>>
        GetRecoverableJobsAsync(
            CancellationToken cancellationToken = default)
    {
      return await _dbContext.ReportJobs
          .AsNoTracking()
          .Where(job =>
              job.Status == ReportJobStatus.Queued ||
              job.Status == ReportJobStatus.Processing)
          .OrderBy(job => job.CreatedAtUtc)
          .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
      await _dbContext.SaveChangesAsync(
          cancellationToken);
    }
  }
}