using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hydra.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GooglePlaceId",
                table: "Venues");

            migrationBuilder.CreateTable(
                name: "VenuePhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    GooglePlaceId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenuePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenuePhotos_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenuePhotos_VenueId_DisplayOrder",
                table: "VenuePhotos",
                columns: new[] { "VenueId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenuePhotos");

            migrationBuilder.AddColumn<string>(
                name: "GooglePlaceId",
                table: "Venues",
                type: "text",
                nullable: true);
        }
    }
}
