using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentChildProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parent_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parent_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "caregiver_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Relation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_caregiver_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_caregiver_members_parent_profiles_ParentProfileId",
                        column: x => x.ParentProfileId,
                        principalTable: "parent_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_caregiver_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "children",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Sex = table.Column<int>(type: "integer", nullable: false),
                    SupportLevel = table.Column<int>(type: "integer", nullable: false),
                    PrimaryDiagnosis = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NonVerbal = table.Column<bool>(type: "boolean", nullable: false),
                    CommunicationMethod = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Allergies = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Medications = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Triggers = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    CalmingStrategies = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    SchoolOrCenter = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CurrentGoals = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_children", x => x.Id);
                    table.CheckConstraint("CK_Child_BirthDate", "\"BirthDate\" IS NULL OR \"BirthDate\" < CURRENT_TIMESTAMP");
                    table.ForeignKey(
                        name: "FK_children_parent_profiles_ParentProfileId",
                        column: x => x.ParentProfileId,
                        principalTable: "parent_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "child_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentBase64 = table.Column<string>(type: "text", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_child_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_child_documents_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_child_documents_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "child_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_child_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_child_notes_children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_caregiver_members_ParentProfileId_Email",
                table: "caregiver_members",
                columns: new[] { "ParentProfileId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_caregiver_members_ParentProfileId_UserId",
                table: "caregiver_members",
                columns: new[] { "ParentProfileId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_caregiver_members_UserId",
                table: "caregiver_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_child_documents_ChildId",
                table: "child_documents",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_child_documents_UploadedByUserId",
                table: "child_documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_child_notes_ChildId",
                table: "child_notes",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_children_ParentProfileId_FirstName_BirthDate",
                table: "children",
                columns: new[] { "ParentProfileId", "FirstName", "BirthDate" });

            migrationBuilder.CreateIndex(
                name: "IX_parent_profiles_UserId",
                table: "parent_profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "caregiver_members");

            migrationBuilder.DropTable(
                name: "child_documents");

            migrationBuilder.DropTable(
                name: "child_notes");

            migrationBuilder.DropTable(
                name: "children");

            migrationBuilder.DropTable(
                name: "parent_profiles");
        }
    }
}
