using System.ComponentModel.DataAnnotations;

namespace JourneyService.Models.Entities
{
  public class TrainRoute
  {
    public Guid RouteId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public ICollection<RouteStation> RouteStations { get; set; }
        = new List<RouteStation>();

    public ICollection<JourneySchedule> Schedules { get; set; }
        = new List<JourneySchedule>();
  }
}
