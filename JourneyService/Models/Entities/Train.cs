using System.ComponentModel.DataAnnotations;

namespace JourneyService.Models.Entities
{
  public class Train
  {
    public Guid TrainId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Seat> Seats { get; set; }
        = new List<Seat>();

    public ICollection<JourneySchedule> Schedules { get; set; }
        = new List<JourneySchedule>();
  }
}
