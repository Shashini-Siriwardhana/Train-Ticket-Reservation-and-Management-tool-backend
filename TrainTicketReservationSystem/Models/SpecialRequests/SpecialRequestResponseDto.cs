namespace TrainTicketReservationSystem.Models.SpecialRequests
{
  public class SpecialRequestResponseDto
  {
    public Guid SpecialRequestId { get; set; }
    public Guid BookingId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
  }
}
