using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INCBack.Migrations
{
    /// <inheritdoc />
    public partial class TaxonomiesAndAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "specialist_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    About = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: true),
                    PricePerHour = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Telegram = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsEmailPublic = table.Column<bool>(type: "boolean", nullable: false),
                    AvatarBase64 = table.Column<string>(type: "text", nullable: true),
                    AvatarMimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ModerationComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModeratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialist_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "specializations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "specialist_profile_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialist_profile_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_specialist_profile_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_specialist_profile_skills_specialist_profiles_SpecialistPro~",
                        column: x => x.SpecialistProfileId,
                        principalTable: "specialist_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpecialistDiplomas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SpecialistProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Base64Data = table.Column<string>(type: "text", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialistDiplomas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialistDiplomas_specialist_profiles_SpecialistProfileId",
                        column: x => x.SpecialistProfileId,
                        principalTable: "specialist_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "specialist_profile_specializations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialist_profile_specializations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_specialist_profile_specializations_specialist_profiles_Spec~",
                        column: x => x.SpecialistProfileId,
                        principalTable: "specialist_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_specialist_profile_specializations_specializations_Speciali~",
                        column: x => x.SpecializationId,
                        principalTable: "specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_skills_Name",
                table: "skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_specialist_profile_skills_SkillId",
                table: "specialist_profile_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_specialist_profile_skills_SpecialistProfileId_SkillId",
                table: "specialist_profile_skills",
                columns: new[] { "SpecialistProfileId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_specialist_profile_specializations_SpecialistProfileId_Spec~",
                table: "specialist_profile_specializations",
                columns: new[] { "SpecialistProfileId", "SpecializationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_specialist_profile_specializations_SpecializationId",
                table: "specialist_profile_specializations",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_specialist_profiles_UserId",
                table: "specialist_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecialistDiplomas_SpecialistProfileId",
                table: "SpecialistDiplomas",
                column: "SpecialistProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_specializations_Name",
                table: "specializations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "specialist_profile_skills");

            migrationBuilder.DropTable(
                name: "specialist_profile_specializations");

            migrationBuilder.DropTable(
                name: "SpecialistDiplomas");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "specializations");

            migrationBuilder.DropTable(
                name: "specialist_profiles");
        }
    }
}
