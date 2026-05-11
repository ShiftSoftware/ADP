using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingPropagationToReplacementItemVehicleModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPendingPropagation",
                schema: "Menu",
                table: "ReplacementItemVehicleModel",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingSince",
                schema: "Menu",
                table: "ReplacementItemVehicleModel",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPendingPropagation",
                schema: "Menu",
                table: "ReplacementItemVehicleModel");

            migrationBuilder.DropColumn(
                name: "PendingSince",
                schema: "Menu",
                table: "ReplacementItemVehicleModel");
        }
    }
}
