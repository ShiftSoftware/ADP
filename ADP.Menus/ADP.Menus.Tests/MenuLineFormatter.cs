using System.Globalization;
using System.Text;

// MenuLineMargins lives in the report layer and supplies the derived margin/profit figures below as
// extension members. Importing it here (rather than reimplementing the arithmetic in the formatter) is
// what makes the golden snapshots prove the Phase 2 move was output-preserving.
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Renders generated menu lines to a canonical, diffable string — the golden snapshot format.
///
/// Every value is written with <see cref="CultureInfo.InvariantCulture"/> so the snapshot text itself
/// is culture-stable, and decimals are written unformatted so their SCALE is pinned too (12.50 and
/// 12.5 render differently and must not be treated as equal — scale reaches the labour code through
/// <c>GetAllowedTimeText</c>).
///
/// Line order is preserved, never sorted: the order the generator emits lines in is part of the
/// behaviour under contract.
/// </summary>
internal static class MenuLineFormatter
{
    internal static string Format(IEnumerable<MenuLineDTO> lines)
    {
        var builder = new StringBuilder();
        var index = 0;

        foreach (var line in lines)
        {
            index++;
            builder.Append("#").Append(index.ToString(CultureInfo.InvariantCulture))
                   .Append(line.IsStandalone ? " STANDALONE" : " PERIODIC").AppendLine();

            builder.Append("   Code=").Append(Quote(line.Code)).AppendLine();
            builder.Append("   LabourCode=").Append(Quote(line.LabourCode)).AppendLine();
            builder.Append("   Description=").Append(Quote(line.Description)).AppendLine();
            builder.Append("   BasicModelCode=").Append(Quote(line.BasicModelCode))
                   .Append(" BrandID=").Append(FormatNullable(line.BrandID))
                   .Append(" Model=").Append(Quote(line.Model)).AppendLine();

            builder.Append("   LabourRate=").Append(FormatDecimal(line.LabourRate))
                   .Append(" AllowedTime=").Append(FormatDecimal(line.AllowedTime))
                   .Append(" Consumable=").Append(FormatDecimal(line.Consumable))
                   .Append(" Discount=").Append(FormatNullable(line.DiscountPercentage)).AppendLine();

            builder.Append("   LabourCost=").Append(FormatDecimal(line.LabourCost))
                   .Append(" LabourPrice=").Append(FormatDecimal(line.LabourPrice))
                   .Append(" LabourTotalPrice=").Append(FormatDecimal(line.LabourTotalPrice))
                   .Append(" LabourProfit=").Append(FormatDecimal(line.LabourProfit)).AppendLine();

            builder.Append("   PartsCost=").Append(FormatDecimal(line.PartsCost))
                   .Append(" PartsPrice=").Append(FormatDecimal(line.PartsPrice))
                   .Append(" PartsProfit=").Append(FormatDecimal(line.PartsProfit))
                   .Append(" PartsProfitPercentage=").Append(FormatDecimal(line.PartsProfitPercentage)).AppendLine();

            builder.Append("   GrossProfit=").Append(FormatDecimal(line.GrossProfit))
                   .Append(" GrossProfitPercentage=").Append(FormatDecimal(line.GrossProfitPercentage))
                   .Append(" MenuProfit=").Append(FormatDecimal(line.MenuProfit))
                   .Append(" MenuTotalPrice=").Append(FormatDecimal(line.MenuTotalPrice)).AppendLine();

            foreach (var part in line.Parts ?? [])
            {
                builder.Append("   part ").Append(Quote(part.PartNumber))
                       .Append(" Quantity=").Append(FormatDecimal(part.Quantity))
                       .Append(" Cost=").Append(FormatDecimal(part.Cost))
                       .Append(" Price=").Append(FormatDecimal(part.Price))
                       .Append(" TotalCost=").Append(FormatDecimal(part.TotalCost))
                       .Append(" TotalPrice=").Append(FormatDecimal(part.TotalPrice)).AppendLine();
            }
        }

        if (index == 0)
            builder.AppendLine("(no lines)");

        return builder.ToString().ReplaceLineEndings("\n").TrimEnd('\n');
    }

    /// <summary>
    /// Renders only the fields the legacy <see cref="MenuLineDTO"/> and the new
    /// <see cref="GeneratedMenuLine"/> have in common — the differential-test projection.
    ///
    /// The derived margin/profit figures are excluded because the shared generator deliberately does
    /// not carry them (they are pure functions of these fields and belong to the DMS report layer).
    /// Everything the generator IS responsible for appears here.
    /// </summary>
    internal static string FormatCore(IEnumerable<MenuLineDTO> lines) =>
        FormatCore(lines.Select(line => new CoreLine(
            line.IsStandalone, line.Code, line.LabourCode, line.Description,
            line.BasicModelCode, line.BrandID, line.Model,
            line.LabourRate, line.AllowedTime, line.Consumable, line.DiscountPercentage,
            (line.Parts ?? []).Select(part => new CorePart(part.PartNumber, part.Quantity, part.Cost, part.Price)))));

    internal static string FormatCore(IEnumerable<GeneratedMenuLine> lines) =>
        FormatCore(lines.Select(line => new CoreLine(
            line.IsStandalone, line.Code, line.LabourCode, line.Description,
            line.BasicModelCode, line.BrandID, line.Model,
            line.LabourRate, line.AllowedTime, line.Consumable, line.DiscountPercentage,
            (line.Parts ?? []).Select(part => new CorePart(part.PartNumber, part.Quantity, part.Cost, part.Price)))));

    private sealed record CorePart(string PartNumber, decimal Quantity, decimal? Cost, decimal Price);

    private sealed record CoreLine(
        bool IsStandalone, string? Code, string? LabourCode, string? Description,
        string? BasicModelCode, long? BrandID, string? Model,
        decimal LabourRate, decimal AllowedTime, decimal Consumable, decimal? DiscountPercentage,
        IEnumerable<CorePart> Parts);

    private static string FormatCore(IEnumerable<CoreLine> lines)
    {
        var builder = new StringBuilder();
        var index = 0;

        foreach (var line in lines)
        {
            index++;
            builder.Append("#").Append(index.ToString(CultureInfo.InvariantCulture))
                   .Append(line.IsStandalone ? " STANDALONE" : " PERIODIC").AppendLine();
            builder.Append("   Code=").Append(Quote(line.Code)).AppendLine();
            builder.Append("   LabourCode=").Append(Quote(line.LabourCode)).AppendLine();
            builder.Append("   Description=").Append(Quote(line.Description)).AppendLine();
            builder.Append("   BasicModelCode=").Append(Quote(line.BasicModelCode))
                   .Append(" BrandID=").Append(FormatNullable(line.BrandID))
                   .Append(" Model=").Append(Quote(line.Model)).AppendLine();
            builder.Append("   LabourRate=").Append(FormatDecimal(line.LabourRate))
                   .Append(" AllowedTime=").Append(FormatDecimal(line.AllowedTime))
                   .Append(" Consumable=").Append(FormatDecimal(line.Consumable))
                   .Append(" Discount=").Append(FormatNullable(line.DiscountPercentage)).AppendLine();

            foreach (var part in line.Parts)
            {
                builder.Append("   part ").Append(Quote(part.PartNumber))
                       .Append(" Quantity=").Append(FormatDecimal(part.Quantity))
                       .Append(" Cost=").Append(FormatNullable(part.Cost))
                       .Append(" Price=").Append(FormatDecimal(part.Price)).AppendLine();
            }
        }

        if (index == 0)
            builder.AppendLine("(no lines)");

        return builder.ToString().ReplaceLineEndings("\n").TrimEnd('\n');
    }

    private static string FormatDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatNullable(decimal? value) => value.HasValue ? FormatDecimal(value.Value) : "null";

    private static string FormatNullable(long? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";

    private static string Quote(string? value) => value is null ? "null" : "\"" + value + "\"";
}
