using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.Menus.Sample.API.Migrations
{
    /// <inheritdoc />
    public partial class RefactorIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IsSuperAdmin",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "IsProtected");

            migrationBuilder.RenameColumn(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "IsProtected");

            migrationBuilder.AddColumn<byte[]>(
                name: "TotpSecret",
                schema: "ShiftIdentity",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotpSecret",
                schema: "ShiftIdentity",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Users",
                newName: "IsSuperAdmin");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Regions",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Countries",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "CompanyBranches",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Companies",
                newName: "BuiltIn");

            migrationBuilder.RenameColumn(
                name: "IsProtected",
                schema: "ShiftIdentity",
                table: "Cities",
                newName: "BuiltIn");

            migrationBuilder.AddColumn<bool>(
                name: "BuiltIn",
                schema: "ShiftIdentity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
