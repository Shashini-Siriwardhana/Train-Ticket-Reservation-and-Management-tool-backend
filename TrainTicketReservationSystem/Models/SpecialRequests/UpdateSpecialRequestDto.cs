using System.ComponentModel.DataAnnotations;

namespace TrainTicketReservationSystem.Models.SpecialRequests
{
  public class UpdateSpecialRequestDto
  {
    [Required(ErrorMessage = "Request type is required.")]
    [StringLength(
            100,
            ErrorMessage = "Request type cannot exceed 100 characters.")]
    public string RequestType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(
        500,
        MinimumLength = 5,
        ErrorMessage =
            "Description must contain between 5 and 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
  }
}
