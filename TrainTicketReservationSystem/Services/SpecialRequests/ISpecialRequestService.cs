using TrainTicketReservationSystem.Models.SpecialRequests;

namespace TrainTicketReservationSystem.Services.SpecialRequests
{
  public interface ISpecialRequestService
  {
    Task<IReadOnlyList<SpecialRequestResponseDto>> GetAllRequests(CancellationToken cancellationToken = default);

    Task<SpecialRequestResponseDto?> GetRequestById(Guid specialRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialRequestResponseDto>> GetRequestByBookingId(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns null when the supplied BookingId does not exist in SQL.
    /// </summary>
    Task<SpecialRequestResponseDto?> CreateRequest(CreateSpecialRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns false when the special request does not exist.
    /// </summary>
    Task<bool> UpdateRequest(Guid specialRequestId, UpdateSpecialRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns false when the special request does not exist.
    /// </summary>
    Task<bool> DeleteRequest(Guid specialRequestId, CancellationToken cancellationToken = default);
  }
}
