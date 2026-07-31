using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddReplicationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceIntervalGroup",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ServiceIntervalGroup",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceInterval",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ServiceInterval",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItem",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ReplacementItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariant",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuVariant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuLabourDetails",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuLabourDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItem",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuItem",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "LabourRateMapping",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "LabourRateMapping",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "BrandMapping",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "BrandMapping",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceIntervalGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ServiceIntervalGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceInterval");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ServiceInterval");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "ReplacementItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuPeriodicAvailability");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuPeriodicAvailability");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuLabourDetails");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuLabourDetails");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "LabourRateMapping");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "LabourRateMapping");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "BrandMapping");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "Menu",
                table: "BrandMapping");
        }
    }
}
