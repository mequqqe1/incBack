using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Migrations
{
    /// <inheritdoc />
    public partial class BookingInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "availability_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBooked = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_slots", x => x.Id);
                    table.CheckConstraint("CK_Availability_Time", "\"EndsAtUtc\" > \"StartsAtUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MessageFromParent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AvailabilitySlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.CheckConstraint("CK_Booking_Time", "\"EndsAtUtc\" > \"StartsAtUtc\"");
                    table.ForeignKey(
                        name: "FK_bookings_availability_slots_AvailabilitySlotId",
                        column: x => x.AvailabilitySlotId,
                        principalTable: "availability_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_availability_slots_SpecialistUserId_StartsAtUtc",
                table: "availability_slots",
                columns: new[] { "SpecialistUserId", "StartsAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AvailabilitySlotId",
                table: "bookings",
                column: "AvailabilitySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ParentUserId_StartsAtUtc",
                table: "bookings",
                columns: new[] { "ParentUserId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_SpecialistUserId_StartsAtUtc",
                table: "bookings",
                columns: new[] { "SpecialistUserId", "StartsAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "availability_slots");
        }
    }
}
