using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Models.DealerData;

[FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
public class AccessoryCSV : CacheableCSV
{
    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string VIN { get; set; } = default!;

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string PartNumber { get; set; } = default!;

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string PartDescription { get; set; } = default!;

    public int WIP { get; set; } = default!;

    public int InvoiceNumber { get; set; }

    //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
    public DateTime DateEdited { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string City { get; set; } = default!;
}
