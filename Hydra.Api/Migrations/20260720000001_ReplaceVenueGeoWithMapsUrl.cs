using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hydra.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceVenueGeoWithMapsUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Venues");

            migrationBuilder.AddColumn<string>(
                name: "MapsUrl",
                table: "Venues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapsUrl",
                table: "Venues");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Venues",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Venues",
                type: "double precision",
                nullable: true);
        }
    }
}
