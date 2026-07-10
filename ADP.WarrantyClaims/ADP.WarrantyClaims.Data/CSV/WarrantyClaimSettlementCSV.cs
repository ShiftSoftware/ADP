using FileHelpers;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.CSV;

public class ExcelFormulaStringConverter : ConverterBase
{
    public override object StringToField(string from)
    {
        if (string.IsNullOrWhiteSpace(from))
            return null!;

        // Matches = "anything"
        if (from.StartsWith("=\"") && from.EndsWith("\""))
            return from.Substring(2, from.Length - 3);

        return from;
    }

    public override string FieldToString(object fieldValue)
    {
        if (fieldValue is null)
            return $"=\"\"";

        return $"=\"{fieldValue}\"";
    }
}

[DelimitedRecord(",")]
[IgnoreFirst()]
public class WarrantyClaimSettlementCSV
{
    [FieldCaption("Invoice No.")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? InvoiceNo { get; set; }

    [FieldCaption("TWC No.")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? ClaimNumber { get; set; }

    [FieldCaption("Franchise")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? Franchise { get; set; }

    [FieldCaption("Warranty Type")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? WarrantyType { get; set; }


    [FieldCaption("Receive Date")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    public string? ReceiveDate { get; set; }


    [FieldCaption("Amount Grand Total")]
    public decimal? AmountGrandTotal { get; set; } = 0m;


    [FieldCaption("SW104 No.")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    public string? SW104No { get; set; }


    [FieldCaption("Amount Settled")]
    public decimal? AmountSettled { get; set; } = 0m;


    [FieldCaption("Settled Amount Parts")]
    public decimal? SettledAmountParts { get; set; } = 0m;


    [FieldCaption("Settled Amount Labor")]
    public decimal? SettledAmountLabor { get; set; } = 0m;


    [FieldCaption("Settled Amount Sublet")]
    public decimal? SettledAmountSublet { get; set; } = 0m;



    [FieldCaption("Settled Tax Total")]
    public decimal? SettledTaxTotal { get; set; } = 0m;


    [FieldCaption("Main Reason Code")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? MainReasonCode { get; set; }




    [FieldCaption("Sub Reason Code1")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode1 { get; set; }




    [FieldCaption("Sub Reason Code2")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode2 { get; set; }



    [FieldCaption("Sub Reason Code3")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode3 { get; set; }




    [FieldCaption("Sub Reason Code4")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode4 { get; set; }




    [FieldCaption("Sub Reason Code5")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode5 { get; set; }



    [FieldCaption("Sub Reason Code6")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? SubReasonCode6 { get; set; }




    [FieldCaption("Status Name")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? StatusName { get; set; }




    [FieldCaption("F.Dist")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DistCode { get; set; }




    [FieldCaption("Data ID")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DataId { get; set; }




    [FieldCaption("WAH Comment")]
    [FieldConverter(typeof(ExcelFormulaStringConverter))]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? WahComment { get; set; }
}
