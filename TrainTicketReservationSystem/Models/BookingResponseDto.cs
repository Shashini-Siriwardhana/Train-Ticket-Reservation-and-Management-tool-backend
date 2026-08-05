namespace TrainTicketReservationSystem.Models
{
  public sealed class BookingResponseDto
  {
    public Guid BookingId { get; set; }

    public DateTime Date { get; set; }

    public DateTime DepartureTime { get; set; }

    public string DepartureStation { get; set; } = string.Empty;

    public string DestinationStation { get; set; } = string.Empty;

    public string SeatNumber { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsRecurring { get; set; }

    public string? RecurringType { get; set; }

    public string ClassType { get; set; } = string.Empty;

    public Guid ScheduleId { get; set; }

    public Guid SeatId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PassengerName { get; set; } = string.Empty;

    public string NIC { get; set; } = string.Empty;

    public string TelephoneNo { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
  }
}