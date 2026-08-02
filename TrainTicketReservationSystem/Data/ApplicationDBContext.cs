using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Models.Entities;
namespace TrainTicketReservationSystem.Data
{
  public class ApplicationDBContext : DbContext
  {
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
      
    }
    public DbSet<Booking> Bookings { get; set; }
  }
}
