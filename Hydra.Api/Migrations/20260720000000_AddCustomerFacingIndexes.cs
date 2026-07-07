using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hydra.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFacingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Venues_Location",
                table: "Venues",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_VenueEvents_StartsAtUtc",
                table: "VenueEvents",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedAtUtc",
                table: "Bookings",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_Location",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_VenueEvents_StartsAtUtc",
                table: "VenueEvents");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedAtUtc",
                table: "Bookings");
        }
    }
}
