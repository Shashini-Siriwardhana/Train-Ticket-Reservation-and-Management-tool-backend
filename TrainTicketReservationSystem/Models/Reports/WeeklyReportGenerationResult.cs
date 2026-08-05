namespace TrainTicketReservationSystem.Models.Reports
{
  public sealed class WeeklyReportGenerationResult
  {
    public string OutputFileName { get; set; } =
        string.Empty;

    public long ProcessingMilliseconds { get; set; }

    public int BookingCount { get; set; }

    public int SpecialRequestCount { get; set; }

    public long FileSizeBytes { get; set; }
  }
}