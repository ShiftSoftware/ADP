using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.API.Migrations
{
    /// <summary>
    /// A variant's single MenuType (0 = Paid, 1 = Free, 2 = Contract) becomes MenuTypes, a JSON array of those
    /// values such as [0,1], and each period and item gets a list of the variant's types it is visible
    /// under. The new columns are filled from the old one BEFORE it is dropped, and every existing period and
    /// item takes its variant's one type, so nothing reads differently afterwards.
    /// </summary>
    public partial class StoreMenuTypesAsJsonLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MenuTypes",
                schema: "Menu",
                table: "MenuVariant",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[0]");

            migrationBuilder.Sql("UPDATE [Menu].[MenuVariant] SET [MenuTypes] = N'[' + CAST([MenuType] AS nvarchar(10)) + N']';");

            migrationBuilder.DropColumn(
                name: "MenuType",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.AddColumn<string>(
                name: "MenuTypes",
                schema: "Menu",
                table: "MenuPeriodicAvailability",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(@"UPDATE p SET p.[MenuTypes] = v.[MenuTypes]
FROM [Menu].[MenuPeriodicAvailability] p INNER JOIN [Menu].[MenuVariant] v ON v.[ID] = p.[MenuVariantID];");

            migrationBuilder.AddColumn<string>(
                name: "StandaloneMenuTypes",
                schema: "Menu",
                table: "MenuItem",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(@"UPDATE i SET i.[StandaloneMenuTypes] = v.[MenuTypes]
FROM [Menu].[MenuItem] i INNER JOIN [Menu].[MenuVariant] v ON v.[ID] = i.[MenuVariantID];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuType",
                schema: "Menu",
                table: "MenuVariant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Lossy by necessity: one value cannot hold a list, so a variant keeps the first of its types.
            migrationBuilder.Sql("UPDATE [Menu].[MenuVariant] SET [MenuType] = CAST(ISNULL(JSON_VALUE([MenuTypes], '$[0]'), '0') AS int);");

            migrationBuilder.DropColumn(
                name: "MenuTypes",
                schema: "Menu",
                table: "MenuVariant");

            migrationBuilder.DropColumn(
                name: "MenuTypes",
                schema: "Menu",
                table: "MenuPeriodicAvailability");

            migrationBuilder.DropColumn(
                name: "StandaloneMenuTypes",
                schema: "Menu",
                table: "MenuItem");
        }
    }
}
