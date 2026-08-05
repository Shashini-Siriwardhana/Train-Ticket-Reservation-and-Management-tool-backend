using TrainTicketReservationSystem.Models.Entities;

namespace TrainTicketReservationSystem.Repositories.Bookings
{
  public interface IBookingRepository
  {
    Task<IReadOnlyList<Booking>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked Booking entity for update or delete.
    /// </summary>
    Task<Booking?> GetForUpdateAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<bool> IsSeatBookedAsync(
        Guid scheduleId,
        Guid seatId,
        Guid? excludedBookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetBookedSeatIdsAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    void Delete(Booking booking);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetForPeriodAsync(
      DateTime? fromDate,
      DateTime? toDate,
      CancellationToken cancellationToken = default);
  }
}