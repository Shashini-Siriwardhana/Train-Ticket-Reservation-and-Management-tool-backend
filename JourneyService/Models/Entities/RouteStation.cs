namespace JourneyService.Models.Entities
{
  public class RouteStation
  {
    public Guid RouteId { get; set; }
    public TrainRoute Route { get; set; } = null!;
    public Guid StationId { get; set; }
    public Station Station { get; set; } = null!;
    public int StopOrder { get; set; }
  }
}
