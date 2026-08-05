namespace TrainTicketReservationSystem.Models.Reports
{
  public sealed class RouteReportSummary
  {
    public string Route { get; set; } =
        string.Empty;

    public int BookingCount { get; set; }

    public decimal Revenue { get; set; }

    public int RecurringBookingCount { get; set; }

    public int OneTimeBookingCount { get; set; }
  }
}