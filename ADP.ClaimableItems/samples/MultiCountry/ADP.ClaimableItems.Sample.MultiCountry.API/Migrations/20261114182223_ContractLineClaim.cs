using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftSoftware.ADP.ClaimableItems.Sample.MultiCountry.API.Migrations
{
    /// <inheritdoc />
    public partial class ContractLineClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ClaimCompanyBranchID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClaimCompanyID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClaimCost",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimDate",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimInvoiceNumber",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimJobNumber",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimQRCode",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ClaimedByUserID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClaimed",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimCompanyBranchID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimCompanyID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimCost",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimDate",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimInvoiceNumber",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimJobNumber",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimQRCode",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "ClaimedByUserID",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "IsClaimed",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                schema: "ClaimableItems",
                table: "ClaimableItemContractLine");
        }
    }
}
