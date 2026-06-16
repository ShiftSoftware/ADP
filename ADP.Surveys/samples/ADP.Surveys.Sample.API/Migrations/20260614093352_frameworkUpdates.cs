using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class frameworkUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedRowLogs");

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
                schema: "ShiftIdentity",
                table: "TeamUsers");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "TeamBranches");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyVersion");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyResponse");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyOutboxEvent");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyAnswer");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "ScreenTemplate");

            migrationBuilder.DropColumn(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "BankQuestion");

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
                schema: "Surveys",
                table: "SurveyVersion",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyResponse",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "SurveyAnswer",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "Survey",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "ScreenTemplate",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "Surveys",
                table: "BankQuestion",
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
