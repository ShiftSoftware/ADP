using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class empty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TriggerPullCursor",
                schema: "Surveys",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSourceRowID = table.Column<long>(type: "bigint", nullable: false),
                    LastAdvancedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RetryRowID = table.Column<long>(type: "bigint", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriggerPullCursor", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TriggerPullCursor",
                schema: "Surveys");
        }
    }
}
