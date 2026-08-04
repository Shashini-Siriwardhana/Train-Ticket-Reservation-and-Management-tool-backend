using JourneyService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SchedulesController : ControllerBase
  {
    private readonly JourneyDbContext dBContext;

    public SchedulesController(JourneyDbContext dBContext)
    {
      this.dBContext = dBContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] Guid routeId,
        [FromQuery] DateTime date)
    {
      var dayStart = date.Date;
      var dayEnd = dayStart.AddDays(1);

      var schedules = await dBContext.Schedules
          .AsNoTracking()
          .Where(schedule =>
              schedule.RouteId == routeId &&
              schedule.IsActive &&
              schedule.DepartureDateTime >= dayStart &&
              schedule.DepartureDateTime < dayEnd)
          .OrderBy(schedule =>
              schedule.DepartureDateTime)
          .Select(schedule => new
          {
            schedule.ScheduleId,
            schedule.TrainId,
            TrainName = schedule.Train.Name,
            schedule.DepartureDateTime
          })
          .ToListAsync();

      return Ok(schedules);
    }
  }
}
