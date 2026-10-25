using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <summary>
    /// Replaces the variant's IsFree flag with a MenuType (0 = Paid, 1 = Free, 2 = Contract). The new
    /// column is added and filled from the old one BEFORE the old one is dropped, so a free variant stays
    /// free; the scaffolded drop-then-add would have silently turned every variant paid.
    /// </summary>
    public partial class ReplaceIsFreeWithMenuType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuType",
                schema: "Menu",
                table: "MenuVariant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [Menu].[MenuVariant] SET [MenuType] = 1 WHERE [IsFree] = 1;");

            migrationBuilder.DropColumn(
                name: "IsFree",
                schema: "Menu",
                table: "MenuVariant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                schema: "Menu",
                table: "MenuVariant",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Lossy by necessity: a bool cannot hold Contract, which reads back as not-free.
            migrationBuilder.Sql("UPDATE [Menu].[MenuVariant] SET [IsFree] = 1 WHERE [MenuType] = 1;");

            migrationBuilder.DropColumn(
                name: "MenuType",
                schema: "Menu",
                table: "MenuVariant");
        }
    }
}
