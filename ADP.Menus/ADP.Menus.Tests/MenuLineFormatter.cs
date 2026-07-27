using System.Globalization;
using System.Text;

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
                   .Append(" BrandID=").Append(N(line.BrandID))
                   .Append(" Model=").Append(Quote(line.Model)).AppendLine();

            builder.Append("   LabourRate=").Append(D(line.LabourRate))
                   .Append(" AllowedTime=").Append(D(line.AllowedTime))
                   .Append(" Consumable=").Append(D(line.Consumable))
                   .Append(" Discount=").Append(N(line.DiscountPercentage)).AppendLine();

            builder.Append("   LabourCost=").Append(D(line.LabourCost))
                   .Append(" LabourPrice=").Append(D(line.LabourPrice))
                   .Append(" LabourTotalPrice=").Append(D(line.LabourTotalPrice))
                   .Append(" LabourProfit=").Append(D(line.LabourProfit)).AppendLine();

            builder.Append("   PartsCost=").Append(D(line.PartsCost))
                   .Append(" PartsPrice=").Append(D(line.PartsPrice))
                   .Append(" PartsProfit=").Append(D(line.PartsProfit))
                   .Append(" PartsProfitPercentage=").Append(D(line.PartsProfitPercentage)).AppendLine();

            builder.Append("   GrossProfit=").Append(D(line.GrossProfit))
                   .Append(" GrossProfitPercentage=").Append(D(line.GrossProfitPercentage))
                   .Append(" MenuProfit=").Append(D(line.MenuProfit))
                   .Append(" MenuTotalPrice=").Append(D(line.MenuTotalPrice)).AppendLine();

            foreach (var part in line.Parts ?? [])
            {
                builder.Append("   part ").Append(Quote(part.PartNumber))
                       .Append(" Qty=").Append(D(part.Quantity))
                       .Append(" Cost=").Append(D(part.Cost))
                       .Append(" Price=").Append(D(part.Price))
                       .Append(" TotalCost=").Append(D(part.TotalCost))
                       .Append(" TotalPrice=").Append(D(part.TotalPrice)).AppendLine();
            }
        }

        if (index == 0)
            builder.AppendLine("(no lines)");

        return builder.ToString().ReplaceLineEndings("\n").TrimEnd('\n');
    }

    private static string D(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string N(decimal? value) => value.HasValue ? D(value.Value) : "null";

    private static string N(long? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";

    private static string Quote(string? value) => value is null ? "null" : "\"" + value + "\"";
}
