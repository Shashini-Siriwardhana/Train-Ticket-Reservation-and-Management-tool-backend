namespace JourneyService.Models.Entities
{
  public class JourneySchedule
  {
    public Guid ScheduleId { get; set; }
    public Guid RouteId { get; set; }
    public TrainRoute Route { get; set; } = null!;
    public Guid TrainId { get; set; }
    public Train Train { get; set; } = null!;
    public DateTime DepartureDateTime { get; set; }
    public bool IsActive { get; set; } = true;
  }
}
