namespace TrainTicketReservationSystem.Models.Reports
{
  public sealed class WeeklyReportDocument
  {
    public Guid ReportJobId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public int TotalBookings { get; set; }

    public int RecurringBookings { get; set; }

    public int OneTimeBookings { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalSpecialRequests { get; set; }

    public IReadOnlyList<RouteReportSummary>
        RouteSummaries
    { get; set; } =
            Array.Empty<RouteReportSummary>();

    public IReadOnlyList<SpecialRequestStatusSummary>
        SpecialRequestStatusSummaries
    { get; set; } =
            Array.Empty<SpecialRequestStatusSummary>();
  }
}