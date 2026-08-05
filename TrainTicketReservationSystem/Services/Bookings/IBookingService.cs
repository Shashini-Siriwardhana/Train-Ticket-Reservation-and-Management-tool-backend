using TrainTicketReservationSystem.Models;

namespace TrainTicketReservationSystem.Services.Bookings
{
  public interface IBookingService
  {
    Task<IReadOnlyList<BookingResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<BookingResponseDto?> GetByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<BookingServiceResult<BookingResponseDto>> CreateAsync(
        AddBookingDto dto,
        CancellationToken cancellationToken = default);

    Task<BookingServiceResult<BookingResponseDto>> UpdateAsync(
        Guid bookingId,
        UpdateBookingDto dto,
        CancellationToken cancellationToken = default);

    Task<BookingServiceResult<bool>> DeleteAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<BookingServiceResult<
        IReadOnlyList<SeatOptionDto>>> GetAvailableSeatsAsync(
        Guid scheduleId,
        string classType,
        CancellationToken cancellationToken = default);
  }
}