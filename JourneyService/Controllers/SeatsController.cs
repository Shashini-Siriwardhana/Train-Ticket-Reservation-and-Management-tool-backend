using JourneyService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SeatsController : ControllerBase
  {
    private readonly JourneyDbContext dBContext;

    public SeatsController(JourneyDbContext dBContext)
    {
      this.dBContext = dBContext;
    }


    [HttpGet("{scheduleId:guid}/seats")]
    public async Task<IActionResult> GetSeats(
        Guid scheduleId,
        [FromQuery] string classType)
    {
      var trainId = await dBContext.Schedules
          .AsNoTracking()
          .Where(schedule =>
              schedule.ScheduleId == scheduleId)
          .Select(schedule =>
              (Guid?)schedule.TrainId)
          .SingleOrDefaultAsync();

      if (trainId is null)
      {
        return NotFound();
      }

      var seats = await dBContext.Seats
          .AsNoTracking()
          .Where(seat =>
              seat.TrainId == trainId &&
              seat.ClassType == classType &&
              seat.IsActive)
          .OrderBy(seat => seat.SeatNumber)
          .Select(seat => new
          {
            seat.SeatId,
            seat.SeatNumber,
            seat.ClassType
          })
          .ToListAsync();

      return Ok(seats);
    }
  }
}
