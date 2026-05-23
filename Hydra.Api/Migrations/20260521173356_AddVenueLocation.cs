using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hydra.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Venues",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Venues");
        }
    }
}
