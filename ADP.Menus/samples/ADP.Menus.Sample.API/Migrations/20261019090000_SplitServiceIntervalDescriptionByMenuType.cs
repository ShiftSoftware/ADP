using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <summary>
    /// Splits the service interval's single Description into one per menu type (Paid, Free, Contract).
    /// The three are added, each filled with the current Description, and only then is Description
    /// dropped — so every existing interval reads exactly as it did, whatever the variant's type.
    /// </summary>
    public partial class SplitServiceIntervalDescriptionByMenuType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in new[] { "PaidDescription", "FreeDescription", "ContractDescription" })
            {
                migrationBuilder.AddColumn<string>(
                    name: column,
                    schema: "Menu",
                    table: "ServiceInterval",
                    type: "nvarchar(max)",
                    nullable: false,
                    defaultValue: "");
            }

            migrationBuilder.Sql(
                "UPDATE [Menu].[ServiceInterval] SET [PaidDescription] = [Description], [FreeDescription] = [Description], [ContractDescription] = [Description];");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Menu",
                table: "ServiceInterval");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Menu",
                table: "ServiceInterval",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Lossy by necessity: one column cannot hold three texts, so the paid one is kept.
            migrationBuilder.Sql("UPDATE [Menu].[ServiceInterval] SET [Description] = [PaidDescription];");

            foreach (var column in new[] { "PaidDescription", "FreeDescription", "ContractDescription" })
            {
                migrationBuilder.DropColumn(
                    name: column,
                    schema: "Menu",
                    table: "ServiceInterval");
            }
        }
    }
}
