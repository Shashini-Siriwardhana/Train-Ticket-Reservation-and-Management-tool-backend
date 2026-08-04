using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Models;
using TrainTicketReservationSystem.Models.Entities;
using TrainTicketReservationSystem.Services.Journeys;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TrainTicketReservationSystem.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class BookingsController : ControllerBase
  {
    private readonly ApplicationDBContext dBContext;
    private readonly IJourneyApiClient _journeyApiClient;

    public BookingsController(ApplicationDBContext dBContext, IJourneyApiClient journeyApiClient)
    {
      this.dBContext = dBContext;
      _journeyApiClient = journeyApiClient;
    }

    [HttpGet]
    public IActionResult GetAllBookings()
    {
      return Ok(dBContext.Bookings.ToList());
    }

    [HttpGet]
    [Route("{id:guid}")]
    public IActionResult GetBookingById(Guid id)
    {
      var booking = dBContext.Bookings.Find(id);
      if (booking is null) 
      { 
        return NotFound();
      }
      return Ok(booking);
    }

    [HttpPost]
    public IActionResult CreateBooking(AddBookingDto addBookingDto) 
    {
      var bookingEntity = new Booking()
      {
        Date = addBookingDto.Date,
        DepartureTime =  addBookingDto.DepartureTime,
        DepartureStation = addBookingDto.DepartureStation,
        DestinationStation = addBookingDto.DestinationStation,
        SeatNumber = addBookingDto.SeatNumber,
        Route = addBookingDto.Route,
        Price = addBookingDto.Price,
        SpecialRequest = addBookingDto.SpecialRequest,
        IsRecurring = addBookingDto.IsRecurring,
        RecurringType = addBookingDto.RecurringType,
        ClassType = addBookingDto.ClassType,
        ScheduleId = addBookingDto.ScheduleId,
        SeatId = addBookingDto.SeatId,
        Status = "Confirmed"
      };

      dBContext.Bookings.Add(bookingEntity);
      dBContext.SaveChanges();
      return Ok(bookingEntity);
    }

    [HttpPut]
    [Route("{id:guid}")]
    public async Task<IActionResult> EditBooking(Guid id, UpdateBookingDto updateBookingDto)
    {
      var booking = dBContext.Bookings.Find(id);
      if (booking is null)
      {
        return NotFound();
      }

      var seatAlreadyBooked = await dBContext.Bookings
        .AsNoTracking()
        .AnyAsync(otherBooking =>
            otherBooking.BookingId != id &&
            otherBooking.ScheduleId == updateBookingDto.ScheduleId &&
            otherBooking.SeatId == updateBookingDto.SeatId &&
            otherBooking.Status != "Cancelled");

      if (seatAlreadyBooked)
      {
        return Conflict(new
        {
          message = "The selected seat has already been booked."
        });
      }

      booking.Date = updateBookingDto.Date;
      booking.DepartureTime = updateBookingDto.DepartureTime;
      booking.DepartureStation = updateBookingDto.DepartureStation;
      booking.DestinationStation = updateBookingDto.DestinationStation;
      booking.SeatNumber = updateBookingDto.SeatNumber;
      booking.Route = updateBookingDto.Route;
      booking.Price = updateBookingDto.Price;
      booking.SpecialRequest = updateBookingDto.SpecialRequest;
      booking.IsRecurring = updateBookingDto.IsRecurring;
      booking.RecurringType = updateBookingDto.RecurringType;
      booking.ClassType = updateBookingDto.ClassType;

      dBContext.SaveChanges();
      return Ok(booking);
    }

    [HttpDelete]
    [Route("{id:guid}")]
    public IActionResult DeleteBooking(Guid id) 
    {
      var booking = dBContext.Bookings.Find(id);
      if (booking is null)
      {
        return NotFound();
      }
      dBContext.Bookings.Remove(booking);
      dBContext.SaveChanges();

      return Ok();
    }

    [HttpGet("available-seats")]
    public async Task<IActionResult> GetAvailableSeats(
        [FromQuery] Guid scheduleId,
        [FromQuery] string classType)
    {
      if (scheduleId == Guid.Empty)
      {
        return BadRequest("A schedule must be selected.");
      }

      if (string.IsNullOrWhiteSpace(classType))
      {
        return BadRequest("A class must be selected.");
      }
      // Call Journey microservice.
      var allSeats = await _journeyApiClient.GetSeats(scheduleId, classType);

      // Read already-booked seats from BookingDb.
      var bookedSeatIds = await dBContext.Bookings
          .Where(booking =>
              booking.ScheduleId == scheduleId &&
              booking.Status != "Cancelled")
          .Select(booking => booking.SeatId)
          .ToListAsync();

      var bookedSeatSet = bookedSeatIds.ToHashSet();

      var availableSeats = allSeats
          .Where(seat =>
              !bookedSeatSet.Contains(seat.SeatId))
          .ToList();

      return Ok(availableSeats);
    }
  }
}
