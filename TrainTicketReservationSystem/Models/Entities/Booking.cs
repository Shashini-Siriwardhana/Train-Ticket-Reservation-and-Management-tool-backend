using System.ComponentModel.DataAnnotations;

namespace TrainTicketReservationSystem.Models.Entities
{
  public class Booking
  {
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Journey date is required")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Departure time is required")]
    public DateTime DepartureTime { get; set; }

    [Required(ErrorMessage = "Departure station is required")]
    public required string DepartureStation { get; set; }

    [Required(ErrorMessage = "Destination station is required")]
    public required string DestinationStation { get; set; }

    [Required(ErrorMessage = "Seat number is required")]
    public required string SeatNumber { get; set; }

    [Required(ErrorMessage = "Route is required")]
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
    public string Status { get; set; } = "Confirmed";
  }
}
