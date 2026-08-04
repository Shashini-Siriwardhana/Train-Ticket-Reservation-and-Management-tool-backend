using JourneyService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JourneyService.Data
{
  public class JourneyDbContext : DbContext
  {
    public JourneyDbContext(DbContextOptions<JourneyDbContext> options) : base(options)
    {

    }
    public DbSet<Station> Stations => Set<Station>();

    public DbSet<TrainRoute> Routes => Set<TrainRoute>();

    public DbSet<RouteStation> RouteStations => Set<RouteStation>();

    public DbSet<Train> Trains => Set<Train>();

    public DbSet<Seat> Seats => Set<Seat>();

    public DbSet<JourneySchedule> Schedules => Set<JourneySchedule>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Station>()
        .HasKey(x => x.StationId);

      modelBuilder.Entity<TrainRoute>()
          .HasKey(x => x.RouteId);

      modelBuilder.Entity<Train>()
          .HasKey(x => x.TrainId);

      modelBuilder.Entity<Seat>()
          .HasKey(x => x.SeatId);

      modelBuilder.Entity<JourneySchedule>()
          .HasKey(x => x.ScheduleId);

      modelBuilder.Entity<RouteStation>()
          .HasKey(x => new // primary key
          {
            x.RouteId,
            x.StationId
          });

      modelBuilder.Entity<RouteStation>()
          .HasOne(x => x.Route)
          .WithMany(x => x.RouteStations)
          .HasForeignKey(x => x.RouteId);

      modelBuilder.Entity<RouteStation>()
          .HasOne(x => x.Station)
          .WithMany(x => x.RouteStations)
          .HasForeignKey(x => x.StationId);

      modelBuilder.Entity<RouteStation>()
          .HasIndex(x => new
          {
            x.RouteId,
            x.StopOrder
          })
          .IsUnique();

      modelBuilder.Entity<Seat>()
          .HasOne(x => x.Train)
          .WithMany(x => x.Seats)
          .HasForeignKey(x => x.TrainId);

      modelBuilder.Entity<Seat>()
          .HasIndex(x => new
          {
            x.TrainId,
            x.SeatNumber
          })
          .IsUnique();

      modelBuilder.Entity<JourneySchedule>()
          .HasOne(x => x.Route)
          .WithMany(x => x.Schedules)
          .HasForeignKey(x => x.RouteId);

      modelBuilder.Entity<JourneySchedule>()
          .HasOne(x => x.Train)
          .WithMany(x => x.Schedules)
          .HasForeignKey(x => x.TrainId);
    }
  }
}
