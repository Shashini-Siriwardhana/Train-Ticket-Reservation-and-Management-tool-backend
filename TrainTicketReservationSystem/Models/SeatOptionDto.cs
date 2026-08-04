namespace TrainTicketReservationSystem.Models
{
  public class SeatOptionDto
  {
    public Guid SeatId { get; set; }

    public string SeatNumber { get; set; }
        = string.Empty;

    public string ClassType { get; set; }
        = string.Empty;
  }
}
