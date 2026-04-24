using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyIntegrationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationId",
                schema: "Surveys",
                table: "Survey",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "UniqueHash",
                schema: "Surveys",
                table: "Survey",
                type: "BINARY(32)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Survey_UniqueHash",
                schema: "Surveys",
                table: "Survey",
                column: "UniqueHash",
                unique: true,
                filter: "UniqueHash IS NOT NULL and IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Survey_UniqueHash",
                schema: "Surveys",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                schema: "Surveys",
                table: "Survey");

            migrationBuilder.DropColumn(
                name: "UniqueHash",
                schema: "Surveys",
                table: "Survey");
        }
    }
}
