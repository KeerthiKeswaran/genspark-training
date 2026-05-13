using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NotificationSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: "notif123");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "f08170c0");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "f67b5f54");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { "f08170c0", "somu@gmail.com", "Somu", "9123456780" },
                    { "f67b5f54", "ramu@gmail.com", "Ramu", "9876543210" }
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "Message", "NotificationType", "ReceiverId", "SenderId", "SentDate", "Status" },
                values: new object[] { "notif123", "Hello Somu!", "Email", "f08170c0", "f67b5f54", new DateTime(2023, 10, 27, 10, 0, 0, 0, DateTimeKind.Utc), "Sent" });
        }
    }
}
