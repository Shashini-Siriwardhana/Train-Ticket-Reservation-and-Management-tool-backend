using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainTicketReservationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerDetailsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NIC",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassengerName",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TelephoneNo",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NIC",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PassengerName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TelephoneNo",
                table: "Bookings");
        }
    }
}
