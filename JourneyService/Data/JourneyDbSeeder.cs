using JourneyService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Data
{
  public class JourneyDbSeeder
  {
    public static async Task SeedAsync(
        JourneyDbContext dbContext)
    {
      // Prevents the same data from being inserted every time the application starts
      if (await dbContext.Routes.AnyAsync())
      {
        return;
      }

      var matara = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "MTR",
        Name = "Matara"
      };

      var weligama = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "WLG",
        Name = "Weligama"
      };

      var galle = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "GLE",
        Name = "Galle"
      };

      var hikkaduwa = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "HIK",
        Name = "Hikkaduwa"
      };

      var kalutara = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "KLT",
        Name = "Kalutara"
      };

      var colombo = new Station
      {
        StationId = Guid.NewGuid(),
        Code = "CMB",
        Name = "Colombo"
      };

      var route = new TrainRoute
      {
        RouteId = Guid.NewGuid(),
        Name = "Matara → Colombo"
      };

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = matara,
        StopOrder = 1
      });

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = weligama,
        StopOrder = 2
      });

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = galle,
        StopOrder = 3
      });

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = hikkaduwa,
        StopOrder = 4
      });

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = kalutara,
        StopOrder = 5
      });

      route.RouteStations.Add(new RouteStation
      {
        Route = route,
        Station = colombo,
        StopOrder = 6
      });

      var train = new Train
      {
        TrainId = Guid.NewGuid(),
        Name = "Ruhunu Kumari"
      };

      AddSeats(train, "FC", "First Class", 6);
      AddSeats(train, "SC", "Second Class", 8);
      AddSeats(train, "TC", "Third Class", 10);

      var schedule = new JourneySchedule
      {
        ScheduleId = Guid.NewGuid(),
        Route = route,
        Train = train,
        DepartureDateTime =
              DateTime.Today.AddDays(1).AddHours(6)
      };

      dbContext.AddRange(route, train, schedule);

      await dbContext.SaveChangesAsync();
    }

    private static void AddSeats(
        Train train,
        string prefix,
        string classType,
        int count)
    {
      for (var number = 1; number <= count; number++)
      {
        train.Seats.Add(new Seat
        {
          SeatId = Guid.NewGuid(),
          SeatNumber = $"{prefix}{number}",
          ClassType = classType,
          IsActive = true
        });
      }
    }
  }
}
