using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Schedules_DepartureTime",
                table: "Schedules",
                column: "DepartureTime");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Destination",
                table: "Routes",
                column: "Destination");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Source",
                table: "Routes",
                column: "Source");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_DepartureTime",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Destination",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Source",
                table: "Routes");
        }
    }
}
