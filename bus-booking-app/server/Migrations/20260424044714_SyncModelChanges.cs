using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OperatorId",
                table: "Hubs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_OperatorId",
                table: "Hubs",
                column: "OperatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hubs_BusOperators_OperatorId",
                table: "Hubs",
                column: "OperatorId",
                principalTable: "BusOperators",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hubs_BusOperators_OperatorId",
                table: "Hubs");

            migrationBuilder.DropIndex(
                name: "IX_Hubs_OperatorId",
                table: "Hubs");

            migrationBuilder.DropColumn(
                name: "OperatorId",
                table: "Hubs");
        }
    }
}
