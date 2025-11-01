using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyTemplateInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weekly_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weekly_template_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeeklyTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_template_slots", x => x.Id);
                    table.CheckConstraint("CK_WeeklyTemplateSlot_Time", "\"EndLocalTime\" > \"StartLocalTime\"");
                    table.ForeignKey(
                        name: "FK_weekly_template_slots_weekly_templates_WeeklyTemplateId",
                        column: x => x.WeeklyTemplateId,
                        principalTable: "weekly_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_template_slots_WeeklyTemplateId_DayOfWeek_StartLocal~",
                table: "weekly_template_slots",
                columns: new[] { "WeeklyTemplateId", "DayOfWeek", "StartLocalTime", "EndLocalTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_templates_SpecialistUserId",
                table: "weekly_templates",
                column: "SpecialistUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_template_slots");

            migrationBuilder.DropTable(
                name: "weekly_templates");
        }
    }
}
