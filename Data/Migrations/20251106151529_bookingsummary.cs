using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class bookingsummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Recommendations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NextSteps = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SpecialistPrivateNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ParentAcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingOutcomes_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingOutcomes_BookingId",
                table: "BookingOutcomes",
                column: "BookingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingOutcomes");
        }
    }
}
