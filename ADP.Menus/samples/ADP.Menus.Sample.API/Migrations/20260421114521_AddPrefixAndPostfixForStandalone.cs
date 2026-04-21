using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPrefixAndPostfixForStandalone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StandaloneMenuPostfix",
                schema: "Menu",
                table: "MenuVariant",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StandaloneMenuPrefix",
                schema: "Menu",
                table: "MenuVariant",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandaloneMenuPostfix",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.DropColumn(
                name: "StandaloneMenuPrefix",
                schema: "Menu",
                table: "MenuVariant");
        }
    }
}
