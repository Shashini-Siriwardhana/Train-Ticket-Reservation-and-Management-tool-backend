namespace TrainTicketReservationSystem.Models.Entities
{
  public class ReportJob
  {
    public Guid ReportJobId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public ReportJobStatus Status { get; set; }
        = ReportJobStatus.Queued;

    public int ProgressPercentage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? OutputFileName { get; set; }

    public string? ErrorMessage { get; set; }

    public long? ProcessingMilliseconds { get; set; }
  }
}
