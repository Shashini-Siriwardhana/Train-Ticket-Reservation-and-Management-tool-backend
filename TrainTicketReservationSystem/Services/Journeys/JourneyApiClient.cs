using TrainTicketReservationSystem.Models;

namespace TrainTicketReservationSystem.Services.Journeys
{
  public class JourneyApiClient : IJourneyApiClient
  {
    private readonly HttpClient _httpClient;

    public JourneyApiClient(HttpClient httpClient)
    {
      _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SeatOptionDto>> GetSeats(
        Guid scheduleId,
        string classType)
    {
      var encodedClassType = Uri.EscapeDataString(classType);
      var url = $"api/Seats/{scheduleId}/seats" + $"?classType={encodedClassType}";
      var seats = await _httpClient.GetFromJsonAsync<List<SeatOptionDto>>(url);

      return seats ?? new List<SeatOptionDto>();
    }
  }
}
