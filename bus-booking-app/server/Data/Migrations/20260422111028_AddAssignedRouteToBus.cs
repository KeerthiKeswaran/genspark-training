using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedRouteToBus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedRouteId",
                table: "Buses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buses_AssignedRouteId",
                table: "Buses",
                column: "AssignedRouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Buses_Routes_AssignedRouteId",
                table: "Buses",
                column: "AssignedRouteId",
                principalTable: "Routes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Buses_Routes_AssignedRouteId",
                table: "Buses");

            migrationBuilder.DropIndex(
                name: "IX_Buses_AssignedRouteId",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "AssignedRouteId",
                table: "Buses");
        }
    }
}
