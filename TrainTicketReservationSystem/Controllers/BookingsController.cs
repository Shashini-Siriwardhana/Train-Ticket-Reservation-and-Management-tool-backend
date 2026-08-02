using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Models;
using TrainTicketReservationSystem.Models.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TrainTicketReservationSystem.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class BookingsController : ControllerBase
  {
    private readonly ApplicationDBContext dBContext;

    public BookingsController(ApplicationDBContext dBContext)
    {
      this.dBContext = dBContext;
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
      };

      dBContext.Bookings.Add(bookingEntity);
      dBContext.SaveChanges();
      return Ok(bookingEntity);
    }

    [HttpPut]
    [Route("{id:guid}")]
    public IActionResult EditBooking(Guid id, UpdateBookingDto updateBookingDto)
    {
      var booking = dBContext.Bookings.Find(id);
      if (booking is null)
      {
        return NotFound();
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
  }
}
