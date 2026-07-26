using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryAndFrameworkUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyOutboxEvent_Status_CreateDate_Pending",
                schema: "Surveys",
                table: "SurveyOutboxEvent");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "IsProtected");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOutboxEvent_Status_CreateDate_Pending",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                columns: new[] { "Status", "CreateDate" },
                filter: "Status IN (0, 2) AND IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyOutboxEvent_Status_CreateDate_Pending",
                schema: "Surveys",
                table: "SurveyOutboxEvent");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "Surveys",
                table: "SurveyOutboxEvent");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "BuiltIn");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOutboxEvent_Status_CreateDate_Pending",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                columns: new[] { "Status", "CreateDate" },
                filter: "Status = 0 AND IsDeleted = 0");
        }
    }
}
