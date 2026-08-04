using System.ComponentModel.DataAnnotations;

namespace JourneyService.Models.Entities
{
  public class Seat
  {
    public Guid SeatId { get; set; }
    public Guid TrainId { get; set; }
    public Train Train { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string SeatNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string ClassType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
  }
}
