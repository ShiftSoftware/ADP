using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Surveys.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class identityUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                schema: "ShiftIdentity",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                schema: "ShiftIdentity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
