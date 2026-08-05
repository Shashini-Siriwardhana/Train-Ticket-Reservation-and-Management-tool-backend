using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Models.Entities;

namespace TrainTicketReservationSystem.Repositories.Bookings
{
  public sealed class EfBookingRepository : IBookingRepository
  {
    private readonly ApplicationDBContext _dbContext;

    public EfBookingRepository(
        ApplicationDBContext dbContext)
    {
      _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Booking>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
      return await _dbContext.Bookings
          .AsNoTracking()
          .OrderByDescending(booking => booking.Date)
          .ThenBy(booking => booking.DepartureTime)
          .ToListAsync(cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
      return await _dbContext.Bookings
          .AsNoTracking()
          .SingleOrDefaultAsync(
              booking =>
                  booking.BookingId == bookingId,
              cancellationToken);
    }

    public async Task<Booking?> GetForUpdateAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
      // No AsNoTracking here.
      // EF Core must track the entity for update/delete.
      return await _dbContext.Bookings
          .SingleOrDefaultAsync(
              booking =>
                  booking.BookingId == bookingId,
              cancellationToken);
    }

    public async Task<bool> IsSeatBookedAsync(
        Guid scheduleId,
        Guid seatId,
        Guid? excludedBookingId,
        CancellationToken cancellationToken = default)
    {
      return await _dbContext.Bookings
          .AsNoTracking()
          .AnyAsync(
              booking =>
                  booking.ScheduleId == scheduleId &&
                  booking.SeatId == seatId &&
                  booking.Status != "Cancelled" &&
                  (!excludedBookingId.HasValue ||
                   booking.BookingId !=
                   excludedBookingId.Value),
              cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>>
        GetBookedSeatIdsAsync(
            Guid scheduleId,
            CancellationToken cancellationToken = default)
    {
      return await _dbContext.Bookings
          .AsNoTracking()
          .Where(
              booking =>
                  booking.ScheduleId == scheduleId &&
                  booking.Status != "Cancelled")
          .Select(booking => booking.SeatId)
          .Distinct()
          .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
      await _dbContext.Bookings.AddAsync(
          booking,
          cancellationToken);
    }

    public void Delete(Booking booking)
    {
      _dbContext.Bookings.Remove(booking);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
      await _dbContext.SaveChangesAsync(
          cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetForPeriodAsync(
    DateTime? fromDate,
    DateTime? toDate,
    CancellationToken cancellationToken = default)
    {
      IQueryable<Booking> query =
          _dbContext.Bookings
              .AsNoTracking();

      if (fromDate.HasValue)
      {
        var startDate =
            fromDate.Value.Date;

        query = query.Where(
            booking =>
                booking.Date >= startDate);
      }

      if (toDate.HasValue)
      {
        /*
         * Use an exclusive upper boundary so all times on
         * the selected To Date are included.
         */
        var endDateExclusive =
            toDate.Value.Date.AddDays(1);

        query = query.Where(
            booking =>
                booking.Date < endDateExclusive);
      }

      return await query
          .OrderBy(booking => booking.Date)
          .ThenBy(booking => booking.DepartureTime)
          .ToListAsync(cancellationToken);
    }
  }
}