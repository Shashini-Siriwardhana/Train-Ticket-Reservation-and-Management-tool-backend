using System.ComponentModel.DataAnnotations;

namespace TrainTicketReservationSystem.Models.Reports
{
  public sealed class CreateWeeklyReportRequest
  {
    [Required(
        ErrorMessage = "From Date is required.")]
    public DateTime? FromDate { get; set; }

    [Required(
        ErrorMessage = "To Date is required.")]
    public DateTime? ToDate { get; set; }
  }
}