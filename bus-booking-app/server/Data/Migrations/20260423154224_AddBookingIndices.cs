using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeatLocks_LockedByUserId",
                table: "SeatLocks");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_SeatLocks_LockedByUserId_JourneyId",
                table: "SeatLocks",
                columns: new[] { "LockedByUserId", "JourneyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_JourneyId",
                table: "Bookings",
                columns: new[] { "CustomerId", "JourneyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeatLocks_LockedByUserId_JourneyId",
                table: "SeatLocks");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId_JourneyId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_SeatLocks_LockedByUserId",
                table: "SeatLocks",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");
        }
    }
}
