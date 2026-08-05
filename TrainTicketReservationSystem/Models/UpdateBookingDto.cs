using System.ComponentModel.DataAnnotations;

namespace TrainTicketReservationSystem.Models
{
  public class UpdateBookingDto
  {
    public DateTime Date { get; set; }
    public DateTime DepartureTime { get; set; }
    public required string DepartureStation { get; set; }
    public required string DestinationStation { get; set; }
    public required string SeatNumber { get; set; }
    public required string Route { get; set; }
    public decimal Price { get; set; }
    public string? SpecialRequest { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurringType { get; set; }
    public required string ClassType
    {
      get; set;
    }
    public Guid ScheduleId { get; set; }
    public Guid SeatId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string NIC { get; set; } = string.Empty;
    public string TelephoneNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
  }
}
