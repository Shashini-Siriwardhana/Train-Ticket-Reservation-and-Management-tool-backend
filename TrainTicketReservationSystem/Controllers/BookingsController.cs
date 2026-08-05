using Microsoft.AspNetCore.Mvc;
using TrainTicketReservationSystem.Models;
using TrainTicketReservationSystem.Services.Bookings;

namespace TrainTicketReservationSystem.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public sealed class BookingsController : ControllerBase
  {
    private readonly IBookingService _bookingService;

    public BookingsController(
        IBookingService bookingService)
    {
      _bookingService = bookingService;
    }

    // GET: api/Bookings
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<BookingResponseDto>>>
        GetAllBookings(
            CancellationToken cancellationToken)
    {
      var bookings =
          await _bookingService.GetAllAsync(
              cancellationToken);

      return Ok(bookings);
    }

    // GET: api/Bookings/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponseDto>>
        GetBookingById(
            Guid id,
            CancellationToken cancellationToken)
    {
      if (id == Guid.Empty)
      {
        return BadRequest(new
        {
          message =
                "A valid booking ID is required."
        });
      }

      var booking =
          await _bookingService.GetByIdAsync(
              id,
              cancellationToken);

      if (booking is null)
      {
        return NotFound(new
        {
          message =
                $"Booking {id} was not found."
        });
      }

      return Ok(booking);
    }

    // POST: api/Bookings
    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>>
        CreateBooking(
            AddBookingDto dto,
            CancellationToken cancellationToken)
    {
      var result =
          await _bookingService.CreateAsync(
              dto,
              cancellationToken);

      if (!result.Succeeded)
      {
        return MapFailure(result);
      }

      var booking = result.Value!;

      return CreatedAtAction(
          nameof(GetBookingById),
          new
          {
            id = booking.BookingId
          },
          booking);
    }

    // PUT: api/Bookings/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookingResponseDto>>
        EditBooking(
            Guid id,
            UpdateBookingDto dto,
            CancellationToken cancellationToken)
    {
      var result =
          await _bookingService.UpdateAsync(
              id,
              dto,
              cancellationToken);

      if (!result.Succeeded)
      {
        return MapFailure(result);
      }

      return Ok(result.Value);
    }

    // DELETE: api/Bookings/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
      var result =
          await _bookingService.DeleteAsync(
              id,
              cancellationToken);

      if (!result.Succeeded)
      {
        return MapFailure(result);
      }

      return NoContent();
    }

    // GET:
    // api/Bookings/available-seats?...
    [HttpGet("available-seats")]
    public async Task<ActionResult<
        IReadOnlyList<SeatOptionDto>>>
        GetAvailableSeats(
            [FromQuery] Guid scheduleId,
            [FromQuery] string classType,
            CancellationToken cancellationToken)
    {
      var result =
          await _bookingService
              .GetAvailableSeatsAsync(
                  scheduleId,
                  classType,
                  cancellationToken);

      if (!result.Succeeded)
      {
        return MapFailure(result);
      }

      return Ok(result.Value);
    }

    private ActionResult MapFailure<T>(
    BookingServiceResult<T> result)
    {
      var message =
          result.Errors.FirstOrDefault()
          ?? "The operation could not be completed.";

      return result.FailureType switch
      {
        BookingFailureType.Validation =>
            BadRequest(new
            {
              message =
                    "Validation failed.",
              errors =
                    result.Errors
            }),

        BookingFailureType.NotFound =>
            NotFound(new
            {
              message
            }),

        BookingFailureType.Conflict =>
            Conflict(new
            {
              message
            }),

        BookingFailureType.DependencyUnavailable =>
            StatusCode(
                StatusCodes
                    .Status503ServiceUnavailable,
                new
                {
                  message
                }),

        _ =>
            StatusCode(
                StatusCodes
                    .Status500InternalServerError,
                new
                {
                  message =
                        "An unexpected error occurred."
                })
      };
    }
  }
}