namespace TrainTicketReservationSystem.Models.SpecialRequests
{
  public class SpecialRequestRecord
  {
    public Guid SpecialRequestId { get; set; }
    // References Booking.BookingId stored in Azure SQL.
    public Guid BookingId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
  }
}
