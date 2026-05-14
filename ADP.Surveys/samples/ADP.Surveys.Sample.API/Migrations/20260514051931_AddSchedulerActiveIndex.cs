using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerActiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SurveyInstance_Status_NextSendAt_Active",
                schema: "Surveys",
                table: "SurveyInstance",
                columns: new[] { "Status", "NextSendAt" },
                filter: "Status IN (0, 1, 2) AND IsDeleted = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SurveyInstance_Status_NextSendAt_Active",
                schema: "Surveys",
                table: "SurveyInstance");
        }
    }
}
