using TrainTicketReservationSystem.Models.Reports;

namespace TrainTicketReservationSystem.Services.Reports
{
  public interface IWeeklyReportGenerator
  {
    Task<WeeklyReportGenerationResult> GenerateAsync(
        Guid reportJobId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
  }
}