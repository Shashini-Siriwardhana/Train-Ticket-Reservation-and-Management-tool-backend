using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Models;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Repositories.Bookings;
using TrainTicketReservationSystem.Repositories.SpecialRequests;
using TrainTicketReservationSystem.Services.Journeys;

namespace TrainTicketReservationSystem.Services.Bookings
{
  public sealed class BookingService : IBookingService
  {
    private static readonly HashSet<string> AllowedClassTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
                "First Class",
                "Second Class",
                "Third Class"
        };

    private static readonly HashSet<string>
        AllowedRecurringTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                    "Daily",
                    "Weekly",
                    "Monthly"
            };

    private static readonly Regex NicPattern =
        new(
            @"^(?:\d{9}[VvXx]|\d{12})$",
            RegexOptions.Compiled);

    private static readonly Regex TelephonePattern =
        new(
            @"^0\d{9}$",
            RegexOptions.Compiled);

    private readonly IBookingRepository _bookingRepository;
    private readonly IJourneyApiClient _journeyApiClient;
    private readonly ISpecialRequestRepository
        _specialRequestRepository;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IBookingRepository bookingRepository,
        IJourneyApiClient journeyApiClient,
        ISpecialRequestRepository specialRequestRepository,
        ILogger<BookingService> logger)
    {
      _bookingRepository = bookingRepository;
      _journeyApiClient = journeyApiClient;
      _specialRequestRepository =
          specialRequestRepository;
      _logger = logger;
    }

    public async Task<IReadOnlyList<BookingResponseDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
      var bookings =
          await _bookingRepository.GetAllAsync(
              cancellationToken);

      return bookings
          .Select(ToResponseDto)
          .ToList();
    }

    public async Task<BookingResponseDto?> GetByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
      if (bookingId == Guid.Empty)
      {
        return null;
      }

      var booking =
          await _bookingRepository.GetByIdAsync(
              bookingId,
              cancellationToken);

      return booking is null
          ? null
          : ToResponseDto(booking);
    }

    public async Task<
        BookingServiceResult<BookingResponseDto>> CreateAsync(
            AddBookingDto dto,
            CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(dto);

      var validationErrors =
          ValidateBooking(
              dto.Date,
              dto.DepartureTime,
              dto.DepartureStation,
              dto.DestinationStation,
              dto.Route,
              dto.Price,
              dto.IsRecurring,
              dto.RecurringType,
              dto.ClassType,
              dto.ScheduleId,
              dto.SeatId,
              dto.PassengerName,
              dto.NIC,
              dto.TelephoneNo,
              dto.Address);

      if (validationErrors.Count > 0)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Validation,
                validationErrors.ToArray());
      }

      var selectedSeatResult =
          await ValidateSelectedSeatAsync(
              dto.ScheduleId,
              dto.SeatId,
              dto.ClassType,
              cancellationToken);

      if (!selectedSeatResult.Succeeded)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                selectedSeatResult.FailureType,
                selectedSeatResult.Errors.ToArray());
      }

      var seatAlreadyBooked =
          await _bookingRepository.IsSeatBookedAsync(
              dto.ScheduleId,
              dto.SeatId,
              excludedBookingId: null,
              cancellationToken);

      if (seatAlreadyBooked)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Conflict,
                "The selected seat has already been booked.");
      }

      var selectedSeat =
          selectedSeatResult.Value!;

      var booking = new Booking
      {
        BookingId = Guid.NewGuid(),

        Date = dto.Date.Date,

        DepartureTime = dto.DepartureTime,

        DepartureStation =
              dto.DepartureStation.Trim(),

        DestinationStation =
              dto.DestinationStation.Trim(),

        Route = dto.Route.Trim(),

        ScheduleId = dto.ScheduleId,

        // Values are taken from JourneyService.
        SeatId = selectedSeat.SeatId,
        SeatNumber = selectedSeat.SeatNumber,
        ClassType = selectedSeat.ClassType,

        Price = dto.Price,

        IsRecurring = dto.IsRecurring,

        RecurringType = dto.IsRecurring
              ? dto.RecurringType?.Trim()
              : null,

        Status = "Confirmed",

        PassengerName =
              dto.PassengerName.Trim(),

        NIC = dto.NIC.Trim(),

        TelephoneNo =
              dto.TelephoneNo.Trim(),

        Address = dto.Address.Trim(),

        // Legacy SQL property.
        // New special requests are stored in XML.
        SpecialRequest = null
      };

      await _bookingRepository.AddAsync(
          booking,
          cancellationToken);

      try
      {
        await _bookingRepository.SaveChangesAsync(
            cancellationToken);
      }
      catch (DbUpdateException exception)
      {
        _logger.LogWarning(
            exception,
            "A database conflict occurred while creating a booking.");

        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Conflict,
                "The selected seat was booked by another customer.");
      }

      return BookingServiceResult<BookingResponseDto>
          .Success(ToResponseDto(booking));
    }

    public async Task<
        BookingServiceResult<BookingResponseDto>> UpdateAsync(
            Guid bookingId,
            UpdateBookingDto dto,
            CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(dto);

      if (bookingId == Guid.Empty)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Validation,
                "A valid booking ID is required.");
      }

      var booking =
          await _bookingRepository.GetForUpdateAsync(
              bookingId,
              cancellationToken);

      if (booking is null)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.NotFound,
                "Booking was not found.");
      }

      var validationErrors =
          ValidateBooking(
              dto.Date,
              dto.DepartureTime,
              dto.DepartureStation,
              dto.DestinationStation,
              dto.Route,
              dto.Price,
              dto.IsRecurring,
              dto.RecurringType,
              dto.ClassType,
              dto.ScheduleId,
              dto.SeatId,
              dto.PassengerName,
              dto.NIC,
              dto.TelephoneNo,
              dto.Address);

      if (validationErrors.Count > 0)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Validation,
                validationErrors.ToArray());
      }

      var selectedSeatResult =
          await ValidateSelectedSeatAsync(
              dto.ScheduleId,
              dto.SeatId,
              dto.ClassType,
              cancellationToken);

      if (!selectedSeatResult.Succeeded)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                selectedSeatResult.FailureType,
                selectedSeatResult.Errors.ToArray());
      }

      var seatAlreadyBooked =
          await _bookingRepository.IsSeatBookedAsync(
              dto.ScheduleId,
              dto.SeatId,
              bookingId,
              cancellationToken);

      if (seatAlreadyBooked)
      {
        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Conflict,
                "The selected seat has already been booked.");
      }

      var selectedSeat =
          selectedSeatResult.Value!;

      booking.Date = dto.Date.Date;

      booking.DepartureTime =
          dto.DepartureTime;

      booking.DepartureStation =
          dto.DepartureStation.Trim();

      booking.DestinationStation =
          dto.DestinationStation.Trim();

      booking.Route = dto.Route.Trim();

      booking.ScheduleId =
          dto.ScheduleId;

      booking.SeatId =
          selectedSeat.SeatId;

      booking.SeatNumber =
          selectedSeat.SeatNumber;

      booking.ClassType =
          selectedSeat.ClassType;

      booking.Price = dto.Price;

      booking.IsRecurring =
          dto.IsRecurring;

      booking.RecurringType =
          dto.IsRecurring
              ? dto.RecurringType?.Trim()
              : null;

      booking.PassengerName =
          dto.PassengerName.Trim();

      booking.NIC =
          dto.NIC.Trim();

      booking.TelephoneNo =
          dto.TelephoneNo.Trim();

      booking.Address =
          dto.Address.Trim();

      try
      {
        await _bookingRepository.SaveChangesAsync(
            cancellationToken);
      }
      catch (DbUpdateException exception)
      {
        _logger.LogWarning(
            exception,
            "A database conflict occurred while updating booking {BookingId}.",
            bookingId);

        return BookingServiceResult<BookingResponseDto>
            .Failure(
                BookingFailureType.Conflict,
                "The selected seat was booked by another customer.");
      }

      return BookingServiceResult<BookingResponseDto>
          .Success(ToResponseDto(booking));
    }

    public async Task<BookingServiceResult<bool>> DeleteAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
      if (bookingId == Guid.Empty)
      {
        return BookingServiceResult<bool>.Failure(
            BookingFailureType.Validation,
            "A valid booking ID is required.");
      }

      var booking =
          await _bookingRepository.GetForUpdateAsync(
              bookingId,
              cancellationToken);

      if (booking is null)
      {
        return BookingServiceResult<bool>.Failure(
            BookingFailureType.NotFound,
            "Booking was not found.");
      }

      /*
       * Prevent XML records from becoming orphaned.
       */
      var specialRequests =
          await _specialRequestRepository
              .GetRequestByBookingId(
                  bookingId,
                  cancellationToken);

      if (specialRequests.Count > 0)
      {
        return BookingServiceResult<bool>.Failure(
            BookingFailureType.Conflict,
            "Delete the booking's special requests before deleting the booking.");
      }

      _bookingRepository.Delete(booking);

      await _bookingRepository.SaveChangesAsync(
          cancellationToken);

      return BookingServiceResult<bool>.Success(true);
    }

    public async Task<BookingServiceResult<
        IReadOnlyList<SeatOptionDto>>>
        GetAvailableSeatsAsync(
            Guid scheduleId,
            string classType,
            CancellationToken cancellationToken = default)
    {
      if (scheduleId == Guid.Empty)
      {
        return BookingServiceResult<
            IReadOnlyList<SeatOptionDto>>.Failure(
                BookingFailureType.Validation,
                "A schedule must be selected.");
      }

      if (string.IsNullOrWhiteSpace(classType) ||
          !AllowedClassTypes.Contains(classType.Trim()))
      {
        return BookingServiceResult<
            IReadOnlyList<SeatOptionDto>>.Failure(
                BookingFailureType.Validation,
                "A valid class must be selected.");
      }

      IReadOnlyList<SeatOptionDto> allSeats;

      try
      {
        allSeats =
            await _journeyApiClient.GetSeats(
                scheduleId,
                classType.Trim(),
                cancellationToken);
      }
      catch (HttpRequestException exception)
      {
        _logger.LogError(
            exception,
            "JourneyService could not return seats for schedule {ScheduleId}.",
            scheduleId);

        return BookingServiceResult<
            IReadOnlyList<SeatOptionDto>>.Failure(
                BookingFailureType.DependencyUnavailable,
                "Journey information is temporarily unavailable.");
      }
      catch (TaskCanceledException)
          when (!cancellationToken.IsCancellationRequested)
      {
        return BookingServiceResult<
            IReadOnlyList<SeatOptionDto>>.Failure(
                BookingFailureType.DependencyUnavailable,
                "JourneyService timed out.");
      }

      var bookedSeatIds =
          await _bookingRepository
              .GetBookedSeatIdsAsync(
                  scheduleId,
                  cancellationToken);

      var bookedSeatSet =
          bookedSeatIds.ToHashSet();

      var availableSeats = allSeats
          .Where(
              seat =>
                  !bookedSeatSet.Contains(
                      seat.SeatId))
          .ToList();

      return BookingServiceResult<
          IReadOnlyList<SeatOptionDto>>.Success(
              availableSeats);
    }

    private async Task<
        BookingServiceResult<SeatOptionDto>>
        ValidateSelectedSeatAsync(
            Guid scheduleId,
            Guid seatId,
            string classType,
            CancellationToken cancellationToken)
    {
      IReadOnlyList<SeatOptionDto> seats;

      try
      {
        seats =
            await _journeyApiClient.GetSeats(
                scheduleId,
                classType.Trim(),
                cancellationToken);
      }
      catch (HttpRequestException exception)
      {
        _logger.LogError(
            exception,
            "JourneyService seat validation failed for schedule {ScheduleId}.",
            scheduleId);

        return BookingServiceResult<SeatOptionDto>
            .Failure(
                BookingFailureType.DependencyUnavailable,
                "Journey information is temporarily unavailable.");
      }
      catch (TaskCanceledException)
          when (!cancellationToken.IsCancellationRequested)
      {
        return BookingServiceResult<SeatOptionDto>
            .Failure(
                BookingFailureType.DependencyUnavailable,
                "JourneyService timed out.");
      }

      var selectedSeat =
          seats.FirstOrDefault(
              seat =>
                  seat.SeatId == seatId);

      if (selectedSeat is null)
      {
        return BookingServiceResult<SeatOptionDto>
            .Failure(
                BookingFailureType.Validation,
                "The selected seat does not belong to the selected schedule and class.");
      }

      return BookingServiceResult<SeatOptionDto>
          .Success(selectedSeat);
    }

    private static List<string> ValidateBooking(
        DateTime date,
        DateTime departureTime,
        string? departureStation,
        string? destinationStation,
        string? route,
        decimal price,
        bool isRecurring,
        string? recurringType,
        string? classType,
        Guid scheduleId,
        Guid seatId,
        string? passengerName,
        string? nic,
        string? telephoneNo,
        string? address)
    {
      var errors = new List<string>();

      if (date == default ||
          date.Date <= DateTime.UtcNow.Date)
      {
        errors.Add(
            "Journey date must be in the future.");
      }

      if (departureTime == default)
      {
        errors.Add(
            "Departure time is required.");
      }

      if (string.IsNullOrWhiteSpace(
              departureStation))
      {
        errors.Add(
            "Departure station is required.");
      }

      if (string.IsNullOrWhiteSpace(
              destinationStation))
      {
        errors.Add(
            "Destination station is required.");
      }

      if (!string.IsNullOrWhiteSpace(
              departureStation) &&
          !string.IsNullOrWhiteSpace(
              destinationStation) &&
          string.Equals(
              departureStation.Trim(),
              destinationStation.Trim(),
              StringComparison.OrdinalIgnoreCase))
      {
        errors.Add(
            "Departure and destination stations must be different.");
      }

      if (string.IsNullOrWhiteSpace(route))
      {
        errors.Add(
            "Route is required.");
      }

      if (price <= 0)
      {
        errors.Add(
            "Ticket price must be greater than zero.");
      }

      if (scheduleId == Guid.Empty)
      {
        errors.Add(
            "A valid schedule must be selected.");
      }

      if (seatId == Guid.Empty)
      {
        errors.Add(
            "A valid seat must be selected.");
      }

      if (string.IsNullOrWhiteSpace(classType) ||
          !AllowedClassTypes.Contains(
              classType.Trim()))
      {
        errors.Add(
            "Class must be First Class, Second Class or Third Class.");
      }

      if (isRecurring &&
          (string.IsNullOrWhiteSpace(
               recurringType) ||
           !AllowedRecurringTypes.Contains(
               recurringType.Trim())))
      {
        errors.Add(
            "Recurring type must be Daily, Weekly or Monthly.");
      }

      if (string.IsNullOrWhiteSpace(
              passengerName) ||
          passengerName.Trim().Length < 2)
      {
        errors.Add(
            "Passenger name is required.");
      }

      if (string.IsNullOrWhiteSpace(nic) ||
          !NicPattern.IsMatch(nic.Trim()))
      {
        errors.Add(
            "Enter a valid Sri Lankan NIC.");
      }

      if (string.IsNullOrWhiteSpace(
              telephoneNo) ||
          !TelephonePattern.IsMatch(
              telephoneNo.Trim()))
      {
        errors.Add(
            "Telephone number must contain 10 digits and begin with 0.");
      }

      if (string.IsNullOrWhiteSpace(address) ||
          address.Trim().Length < 5)
      {
        errors.Add(
            "Passenger address must contain at least 5 characters.");
      }

      return errors;
    }

    private static BookingResponseDto ToResponseDto(
        Booking booking)
    {
      return new BookingResponseDto
      {
        BookingId = booking.BookingId,
        Date = booking.Date,
        DepartureTime =
              booking.DepartureTime,
        DepartureStation =
              booking.DepartureStation,
        DestinationStation =
              booking.DestinationStation,
        SeatNumber =
              booking.SeatNumber,
        Route = booking.Route,
        Price = booking.Price,
        IsRecurring =
              booking.IsRecurring,
        RecurringType =
              booking.RecurringType,
        ClassType =
              booking.ClassType,
        ScheduleId =
              booking.ScheduleId,
        SeatId =
              booking.SeatId,
        Status =
              booking.Status,
        PassengerName =
              booking.PassengerName,
        NIC = booking.NIC,
        TelephoneNo =
              booking.TelephoneNo,
        Address =
              booking.Address
      };
    }
  }
}