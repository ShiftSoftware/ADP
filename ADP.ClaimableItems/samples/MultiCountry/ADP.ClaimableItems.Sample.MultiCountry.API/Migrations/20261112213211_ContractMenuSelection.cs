using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Migrations
{
    /// <inheritdoc />
    public partial class ContractMenuSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MenuID",
                schema: "ClaimableItems",
                table: "ClaimableItemContract",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MenuVehicleModelID",
                schema: "ClaimableItems",
                table: "ClaimableItemContract",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MenuID",
                schema: "ClaimableItems",
                table: "ClaimableItemContract");

            migrationBuilder.DropColumn(
                name: "MenuVehicleModelID",
                schema: "ClaimableItems",
                table: "ClaimableItemContract");
        }
    }
}
