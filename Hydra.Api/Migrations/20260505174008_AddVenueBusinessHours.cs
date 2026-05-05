using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hydra.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueBusinessHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloseHour",
                table: "BookingRules",
                type: "integer",
                nullable: false,
                defaultValue: 22);

            migrationBuilder.AddColumn<int>(
                name: "OpenHour",
                table: "BookingRules",
                type: "integer",
                nullable: false,
                defaultValue: 9);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseHour",
                table: "BookingRules");

            migrationBuilder.DropColumn(
                name: "OpenHour",
                table: "BookingRules");
        }
    }
}
