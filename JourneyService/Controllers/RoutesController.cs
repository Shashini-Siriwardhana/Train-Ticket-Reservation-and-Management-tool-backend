using JourneyService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class RoutesController : ControllerBase
  {
    private readonly JourneyDbContext dBContext;

    public RoutesController(JourneyDbContext dBContext)
    {
      this.dBContext = dBContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes()
    {
      var routes = await dBContext.Routes
          .AsNoTracking()
          .OrderBy(route => route.Name)
          .Select(route => new
          {
            route.RouteId,
            route.Name
          })
          .ToListAsync();

      return Ok(routes);
    }

    [HttpGet("{routeId:guid}/stations")]
    public async Task<IActionResult> GetRouteStations(
        Guid routeId)
    {
      var routeExists = await dBContext.Routes
          .AnyAsync(route => route.RouteId == routeId);

      if (!routeExists)
      {
        return NotFound();
      }

      var stations = await dBContext.RouteStations
          .AsNoTracking()
          .Where(item => item.RouteId == routeId)
          .OrderBy(item => item.StopOrder)
          .Select(item => new
          {
            item.StationId,
            item.Station.Name,
            item.StopOrder
          })
          .ToListAsync();

      return Ok(stations);
    }
  }
}
