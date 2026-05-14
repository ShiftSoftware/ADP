using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTriggerInstanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryLogJson",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSentAt",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextSendAt",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientAddress",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientLocale",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemindersRemaining",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TriggerId",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "UniqueHash",
                schema: "Surveys",
                table: "SurveyInstance",
                type: "BINARY(32)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_UniqueHash",
                schema: "Surveys",
                table: "SurveyInstance",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyInstance_UniqueHash",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "Channel",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "DeliveryLogJson",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "LastSentAt",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "NextSendAt",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "RecipientAddress",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "RecipientLocale",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "RemindersRemaining",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "TriggerId",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.DropColumn(
                name: "UniqueHash",
                schema: "Surveys",
                table: "SurveyInstance");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastReplicationDate",
                schema: "ShiftIdentity",
                table: "CompanyCalendars",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
