using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class SOLaborCosmosModel
{
    public string id { get; set; } = default!;

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string VIN { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string RTSCode { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string InvoiceState { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string LoadStatus { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FileHelpers.FieldConverter(FileHelpers.ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime DateEdited { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public int SOLabordata_DealerId { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public int Comp { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public int WIP { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FileHelpers.FieldNullValue(0)]
    public int InvoiceNumber { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string SalesType { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Account { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string MenuCode { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public decimal? ExtendedPrice { get; set; }

    public int LineNumber { get; set; }
    [FileHelpers.FieldNullValue(0)]
    public int OriginalInvoiceNumber { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Customer_Magic { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Department { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Description { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string ServiceCode { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
    public DateTime? DateCreated { get; set; }


    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
    public DateTime? DateInserted { get; set; }

    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}
