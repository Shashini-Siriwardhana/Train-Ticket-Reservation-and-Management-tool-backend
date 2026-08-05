using System.ComponentModel.DataAnnotations;

namespace TrainTicketReservationSystem.Models
{
  public sealed class AddBookingDto
  {
    [Required(ErrorMessage = "Journey date is required.")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Departure time is required.")]
    public DateTime DepartureTime { get; set; }

    [Required(ErrorMessage = "Departure station is required.")]
    [StringLength(150)]
    public string DepartureStation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination station is required.")]
    [StringLength(150)]
    public string DestinationStation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seat number is required.")]
    [StringLength(30)]
    public string SeatNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Route is required.")]
    [StringLength(200)]
    public string Route { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "1000000",
        ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public bool IsRecurring { get; set; }

    public string? RecurringType { get; set; }

    [Required(ErrorMessage = "Class is required.")]
    public string ClassType { get; set; } = string.Empty;

    public Guid ScheduleId { get; set; }

    public Guid SeatId { get; set; }

    [Required(ErrorMessage = "Passenger name is required.")]
    [StringLength(
        150,
        MinimumLength = 2,
        ErrorMessage =
            "Passenger name must contain between 2 and 150 characters.")]
    public string PassengerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "NIC is required.")]
    [StringLength(20)]
    public string NIC { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telephone number is required.")]
    [StringLength(20)]
    public string TelephoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(
        500,
        MinimumLength = 5,
        ErrorMessage =
            "Address must contain between 5 and 500 characters.")]
    public string Address { get; set; } = string.Empty;
  }
}