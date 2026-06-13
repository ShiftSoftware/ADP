using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedRowLogs");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModelLabourRate");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModelLabourDetails");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModel");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserLogs");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceIntervalGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceInterval");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemVehicleModelPart");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemVehicleModel");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemServiceIntervalGroup");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVersion");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariantLabourRate");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuPeriodicAvailability");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuLabourDetails");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItemPartCountryPrice");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItemPart");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItem");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "Menu");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "LabourRateMapping");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "BrandMapping");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees");

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Regions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyCalendars");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchServices");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchDepartments");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "CompanyBranchBrands");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "LastReplicationStamp",
                schema: "ShiftIdentity",
                table: "Brands");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModelLabourRate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModelLabourDetails",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "VehicleModel",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "UserAccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamBranches",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "StandaloneReplacementItemGroup",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceIntervalGroup",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ServiceInterval",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemVehicleModelPart",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemVehicleModel",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItemServiceIntervalGroup",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "ReplacementItem",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVersion",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariantLabourRate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuVariant",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuLabourDetails",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItemPartCountryPrice",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItemPart",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "MenuItem",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "Menu",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "LabourRateMapping",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Menu",
                table: "BrandMapping",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "Apps",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "AccessTrees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeletedRowLogs",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContainerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReplicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartitionKeyLevelOneType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelOneValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelThreeType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelThreeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartitionKeyLevelTwoType = table.Column<int>(type: "int", nullable: false),
                    PartitionKeyLevelTwoValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedRowLogs", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedRowLogs_ContainerName_LastReplicationDate",
                table: "DeletedRowLogs",
                columns: new[] { "ContainerName", "LastReplicationDate" });
        }
    }
}
