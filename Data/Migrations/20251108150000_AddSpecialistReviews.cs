using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INCBack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialistReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "specialist_profiles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ReviewsCount",
                table: "specialist_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "specialist_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialistUserId = table.Column<string>(type: "text", nullable: false),
                    ParentUserId = table.Column<string>(type: "text", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specialist_reviews", x => x.Id);
                    table.CheckConstraint("CK_Review_Rating", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_specialist_reviews_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_specialist_reviews_BookingId",
                table: "specialist_reviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_specialist_reviews_SpecialistUserId_CreatedAtUtc",
                table: "specialist_reviews",
                columns: new[] { "SpecialistUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "specialist_reviews");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "specialist_profiles");

            migrationBuilder.DropColumn(
                name: "ReviewsCount",
                table: "specialist_profiles");
        }
    }
}
