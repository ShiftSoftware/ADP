using ShiftSoftware.ADP.Cases.Shared;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Printing;

public static class WarrantyClaimsCurrencyPrinting
{
    /// <summary>
    /// Makes a claim, certificate or invoice printout show the currency of its figures. The templates are
    /// written for USD and a USD printout is left exactly as it is. For another currency the cells formatted
    /// with a dollar sign take the currency's decimals and ISO code, the other money cells get thousand
    /// separators, and the money labels get the code appended. Local figures are much longer than dollar
    /// ones, so every cell touched here stays on one line and shrinks to fit its column.
    /// </summary>
    public static void UseClaimCurrency(this FastReport.Report report, string? currency)
    {
        if (DocumentCurrency.IsUSD(currency))
            return;

        var code = currency!.Trim().ToUpperInvariant();
        var decimals = DocumentCurrency.Decimals(code);
        var number = decimals == 0 ? "#,0" : "#,0." + new string('0', decimals);
        // The unformatted figures sit in narrow columns, so they drop zero minor units (12,643,100 rather than 12,643,100.00).
        var lineNumber = decimals == 0 ? "#,0" : "#,0." + new string('#', decimals);

        foreach (var text in report.AllObjects.OfType<FastReport.TextObjectBase>())
        {
            var isFigure = true;

            if (text.Format is FastReport.Format.CustomFormat custom && custom.Format.StartsWith('$'))
                custom.Format = $"{number} '{code}'";
            else if (IsMoneyField(text.Text))
                text.Format = new FastReport.Format.CustomFormat { Format = lineNumber };
            else if (IsMoneyLabel(text.Text))
            {
                text.Text = $"{text.Text} ({code})";
                isFigure = false;
            }
            else
                continue;

            // A figure never breaks across lines; a label keeps the template's wrapping.
            if (text is FastReport.TextObject cell)
            {
                cell.WordWrap = cell.WordWrap && !isFigure;
                cell.AutoShrink = FastReport.AutoShrinkMode.FontSize;
                cell.AutoShrinkMinSize = 7;
            }
        }
    }

    // A single data field holding money: [Labor.Amount], [Part.Price], [C2.LaborRate], [Table.Parts], ...
    private static readonly Regex MoneyField =
        new(@"^\[\w+\.(\w*Amount|\w*Price|LaborRate|Labor|Sublet|Parts|Total)\]$", RegexOptions.CultureInvariant);

    private static bool IsMoneyField(string? text) => text is not null && MoneyField.IsMatch(text);

    private static bool IsMoneyLabel(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && !text.Contains('[')
        && (text.Contains("Amount") || text.Contains("Price") || text.Contains("Labor Rate"));
}
