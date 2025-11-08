using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrackerMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    SleepTotalHours = table.Column<double>(type: "double precision", nullable: true),
                    SleepLatencyMin = table.Column<int>(type: "integer", nullable: true),
                    NightWakings = table.Column<int>(type: "integer", nullable: true),
                    SleepQuality = table.Column<int>(type: "integer", nullable: true),
                    Mood = table.Column<int>(type: "integer", nullable: false),
                    Anxiety = table.Column<int>(type: "integer", nullable: true),
                    SensoryOverload = table.Column<bool>(type: "boolean", nullable: false),
                    MealsCount = table.Column<int>(type: "integer", nullable: true),
                    Appetite = table.Column<int>(type: "integer", nullable: false),
                    DietNotes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CommunicationLevel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NewSkillObserved = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ToiletingStatus = table.Column<int>(type: "integer", nullable: false),
                    SelfCareDressing = table.Column<bool>(type: "boolean", nullable: true),
                    SelfCareHygiene = table.Column<bool>(type: "boolean", nullable: true),
                    HomeTasksDone = table.Column<bool>(type: "boolean", nullable: true),
                    RewardUsed = table.Column<bool>(type: "boolean", nullable: true),
                    TriggersJson = table.Column<string>(type: "text", nullable: true),
                    EnvironmentChangesJson = table.Column<string>(type: "text", nullable: true),
                    ParentNote = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    IncidentsCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_entries_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    DurationSec = table.Column<int>(type: "integer", nullable: true),
                    Injury = table.Column<bool>(type: "boolean", nullable: false),
                    AntecedentJson = table.Column<string>(type: "text", nullable: true),
                    BehaviorJson = table.Column<string>(type: "text", nullable: true),
                    ConsequenceJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_incidents_daily_entries_DailyEntryId",
                        column: x => x.DailyEntryId,
                        principalTable: "daily_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_med_intakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Drug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Taken = table.Column<bool>(type: "boolean", nullable: false),
                    SideEffectsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_med_intakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_med_intakes_daily_entries_DailyEntryId",
                        column: x => x.DailyEntryId,
                        principalTable: "daily_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DurationMin = table.Column<int>(type: "integer", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: true),
                    GoalTagsJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_sessions_daily_entries_DailyEntryId",
                        column: x => x.DailyEntryId,
                        principalTable: "daily_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_entries_ChildId_Date",
                table: "daily_entries",
                columns: new[] { "ChildId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_incidents_DailyEntryId",
                table: "daily_incidents",
                column: "DailyEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_med_intakes_DailyEntryId",
                table: "daily_med_intakes",
                column: "DailyEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_sessions_DailyEntryId",
                table: "daily_sessions",
                column: "DailyEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_incidents");

            migrationBuilder.DropTable(
                name: "daily_med_intakes");

            migrationBuilder.DropTable(
                name: "daily_sessions");

            migrationBuilder.DropTable(
                name: "daily_entries");
        }
    }
}
