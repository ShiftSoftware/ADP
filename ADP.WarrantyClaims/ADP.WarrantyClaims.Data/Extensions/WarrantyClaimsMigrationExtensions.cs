using Microsoft.EntityFrameworkCore.Migrations;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;

public static class WarrantyClaimsMigrationExtensions
{
    /// <summary>
    /// Data migration for the claims that existed before claims had a currency. Call it in the host's
    /// migration, after the columns are added: the column defaults have already made every existing claim
    /// a USD claim at rate 1, and this copies its amounts into the USD equivalents. It only touches USD
    /// claims, where the two are equal by definition, so running it again changes nothing.
    /// Pass the table names when the host's DbSets rename them.
    /// </summary>
    public static MigrationBuilder MigrateWarrantyClaimsToUSD(
        this MigrationBuilder migrationBuilder,
        string? schema = null,
        string claimTable = nameof(WarrantyClaim),
        string partLineTable = nameof(WarrantyClaimPartLine),
        string subletLineTable = nameof(WarrantyClaimSubletLine))
    {
        foreach (var sql in MigrateToUSDSql(schema, claimTable, partLineTable, subletLineTable))
            migrationBuilder.Sql(sql);

        return migrationBuilder;
    }

    public static IReadOnlyList<string> MigrateToUSDSql(
        string? schema = null,
        string claimTable = nameof(WarrantyClaim),
        string partLineTable = nameof(WarrantyClaimPartLine),
        string subletLineTable = nameof(WarrantyClaimSubletLine))
    {
        string table(string name) => string.IsNullOrWhiteSpace(schema) ? $"[{name}]" : $"[{schema}].[{name}]";

        var usdClaims = $"SELECT [ID] FROM {table(claimTable)} WHERE [{nameof(WarrantyClaim.Currency)}] = '{DocumentCurrency.USD}'";

        return
        [
            $"""
            UPDATE {table(claimTable)} SET
                [{nameof(WarrantyClaim.CurrencyRate)}] = 1,
                [{nameof(WarrantyClaim.LaborRateUSD)}] = [{nameof(WarrantyClaim.LaborRate)}],
                [{nameof(WarrantyClaim.LaborTotalAmountUSD)}] = [{nameof(WarrantyClaim.LaborTotalAmount)}],
                [{nameof(WarrantyClaim.SubletTotalAmountUSD)}] = [{nameof(WarrantyClaim.SubletTotalAmount)}],
                [{nameof(WarrantyClaim.PartsTotalAmountUSD)}] = [{nameof(WarrantyClaim.PartsTotalAmount)}],
                [{nameof(WarrantyClaim.TotalClaimAmountUSD)}] = [{nameof(WarrantyClaim.TotalClaimAmount)}]
            WHERE [{nameof(WarrantyClaim.Currency)}] = '{DocumentCurrency.USD}'
            """,
            $"""
            UPDATE {table(partLineTable)} SET [{nameof(WarrantyClaimPartLine.PriceUSD)}] = [{nameof(WarrantyClaimPartLine.Price)}]
            WHERE [{nameof(WarrantyClaimPartLine.WarrantyClaimID)}] IN ({usdClaims})
            """,
            $"""
            UPDATE {table(subletLineTable)} SET [{nameof(WarrantyClaimSubletLine.AmountUSD)}] = [{nameof(WarrantyClaimSubletLine.Amount)}]
            WHERE [{nameof(WarrantyClaim)}ID] IN ({usdClaims})
            """,
        ];
    }
}
