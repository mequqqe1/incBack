using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChatFavoritesCalendarChronology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecordedByUserId",
                table: "daily_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordedByUserId",
                table: "daily_med_intakes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedCaregiverMemberId",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "parent_favorite_specialists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_favorite_specialists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parent_specialist_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_specialist_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parent_specialist_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_specialist_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parent_specialist_messages_parent_specialist_conversations_~",
                        column: x => x.ConversationId,
                        principalTable: "parent_specialist_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AssignedCaregiverMemberId",
                table: "bookings",
                column: "AssignedCaregiverMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_parent_favorite_specialists_ParentUserId_SpecialistUserId",
                table: "parent_favorite_specialists",
                columns: new[] { "ParentUserId", "SpecialistUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parent_specialist_conversations_ParentUserId_SpecialistUser~",
                table: "parent_specialist_conversations",
                columns: new[] { "ParentUserId", "SpecialistUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parent_specialist_messages_ConversationId_CreatedAtUtc",
                table: "parent_specialist_messages",
                columns: new[] { "ConversationId", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_caregiver_members_AssignedCaregiverMemberId",
                table: "bookings",
                column: "AssignedCaregiverMemberId",
                principalTable: "caregiver_members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_caregiver_members_AssignedCaregiverMemberId",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "parent_favorite_specialists");

            migrationBuilder.DropTable(
                name: "parent_specialist_messages");

            migrationBuilder.DropTable(
                name: "parent_specialist_conversations");

            migrationBuilder.DropIndex(
                name: "IX_bookings_AssignedCaregiverMemberId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "RecordedByUserId",
                table: "daily_sessions");

            migrationBuilder.DropColumn(
                name: "RecordedByUserId",
                table: "daily_med_intakes");

            migrationBuilder.DropColumn(
                name: "AssignedCaregiverMemberId",
                table: "bookings");
        }
    }
}
