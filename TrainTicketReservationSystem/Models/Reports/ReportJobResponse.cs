namespace TrainTicketReservationSystem.Models.Reports
{
  public sealed class ReportJobResponse
  {
    public Guid ReportJobId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string Status { get; set; }
        = string.Empty;

    public int ProgressPercentage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? OutputFileName { get; set; }

    public string? ErrorMessage { get; set; }

    public long? ProcessingMilliseconds { get; set; }

    public bool CanDownload { get; set; }

    public string? StatusUrl { get; set; }

    public string? DownloadUrl { get; set; }
  }
}