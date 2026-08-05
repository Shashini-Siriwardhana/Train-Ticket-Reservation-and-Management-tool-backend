using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainTicketReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueScheduleSeatIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Bookings_ScheduleId_SeatId",
                table: "Bookings",
                columns: new[] { "ScheduleId", "SeatId" },
                unique: true,
                filter: "[Status] <> 'Cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Bookings_ScheduleId_SeatId",
                table: "Bookings");
        }
    }
}
