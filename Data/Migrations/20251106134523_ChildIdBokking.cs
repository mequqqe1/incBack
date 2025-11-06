using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChildIdBokking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChildId",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ChildId",
                table: "bookings",
                column: "ChildId");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_children_ChildId",
                table: "bookings",
                column: "ChildId",
                principalTable: "children",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_children_ChildId",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_ChildId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "ChildId",
                table: "bookings");
        }
    }
}
