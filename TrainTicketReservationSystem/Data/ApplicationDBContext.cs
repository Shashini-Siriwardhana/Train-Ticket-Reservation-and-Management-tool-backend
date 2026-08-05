using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Models.Entities;

namespace TrainTicketReservationSystem.Data
{
  public class ApplicationDBContext : DbContext
  {
    public ApplicationDBContext(
        DbContextOptions<ApplicationDBContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }

    public DbSet<ReportJob> ReportJobs =>
    Set<ReportJob>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<Booking>()
          .HasIndex(booking => new
          {
            booking.ScheduleId,
            booking.SeatId
          })
          .IsUnique()
          .HasDatabaseName(
              "UX_Bookings_ScheduleId_SeatId")
          .HasFilter("[Status] <> 'Cancelled'");

      modelBuilder.Entity<Booking>()
          .Property(booking => booking.Price)
          .HasPrecision(18, 2);

      modelBuilder.Entity<ReportJob>(
          entity =>
          {
            entity.HasKey(job =>
                job.ReportJobId);

            entity.Property(job => job.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(job =>
                    job.OutputFileName)
                .HasMaxLength(260);

            entity.Property(job =>
                    job.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(job =>
                    job.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(job =>
                    job.CreatedAtUtc)
                .HasDatabaseName(
                    "IX_ReportJobs_CreatedAtUtc");

            entity.HasIndex(job =>
                    job.Status)
                .HasDatabaseName(
                    "IX_ReportJobs_Status");

            entity.ToTable(
                "ReportJobs",
                tableBuilder =>
                {
                  tableBuilder
                        .HasCheckConstraint(
                            "CK_ReportJobs_ProgressPercentage",
                            "[ProgressPercentage] >= 0 " +
                            "AND [ProgressPercentage] <= 100");
                });
          });
    }
  }
}