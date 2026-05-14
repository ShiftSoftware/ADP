using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyOutboxEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurveyOutboxEvent",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyResponseID = table.Column<long>(type: "bigint", nullable: false),
                    SurveyInstanceID = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DispatchLogJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSaveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReplicationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedByUserID = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyOutboxEvent", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SurveyOutboxEvent_SurveyInstance_SurveyInstanceID",
                        column: x => x.SurveyInstanceID,
                        principalSchema: "Surveys",
                        principalTable: "SurveyInstance",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SurveyOutboxEvent_SurveyResponse_SurveyResponseID",
                        column: x => x.SurveyResponseID,
                        principalSchema: "Surveys",
                        principalTable: "SurveyResponse",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOutboxEvent_Status_CreateDate_Pending",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                columns: new[] { "Status", "CreateDate" },
                filter: "Status = 0 AND IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOutboxEvent_SurveyInstanceID",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                column: "SurveyInstanceID");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyOutboxEvent_SurveyResponseID",
                schema: "Surveys",
                table: "SurveyOutboxEvent",
                column: "SurveyResponseID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveyOutboxEvent",
                schema: "Surveys");
        }
    }
}
