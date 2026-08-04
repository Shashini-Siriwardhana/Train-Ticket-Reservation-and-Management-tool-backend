using System.ComponentModel.DataAnnotations;

namespace JourneyService.Models.Entities
{
  public class Station
  {
    public Guid StationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<RouteStation> RouteStations { get; set; }
        = new List<RouteStation>();
  }
}
