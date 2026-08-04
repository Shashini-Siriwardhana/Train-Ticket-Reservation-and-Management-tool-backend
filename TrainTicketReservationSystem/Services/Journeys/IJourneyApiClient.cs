using TrainTicketReservationSystem.Models;

namespace TrainTicketReservationSystem.Services.Journeys
{
  public interface IJourneyApiClient
  {
    Task<IReadOnlyList<SeatOptionDto>> GetSeats(
        Guid scheduleId,
        string classType);
  }
}
