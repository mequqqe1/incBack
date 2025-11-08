using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrackerTweaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "daily_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_daily_sessions_CreatedAtUtc",
                table: "daily_sessions",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_daily_sessions_CreatedAtUtc",
                table: "daily_sessions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "daily_sessions");
        }
    }
}
