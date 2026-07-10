using FileHelpers;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.CSV;

class WarrantyClaimManufacturerCSV_PadRightConverter : ConverterBase
{
    private int _size;
    public WarrantyClaimManufacturerCSV_PadRightConverter(int size)
    {
        _size = size;
    }

    public override object StringToField(string from)
    {
        throw new NotImplementedException();
    }

    public override string FieldToString(object from)
    {
        if (from is null)
            return "";

        return from.ToString()!;

        return (from ?? "").ToString()!.PadRight(_size, ' ');
    }
}

class WarrantyClaimManufacturerCSV_FranchiseConverter : ConverterBase
{
    public override object StringToField(string from)
    {
        throw new NotImplementedException();
    }

    public override string FieldToString(object from)
    {
        switch (from)
        {
            case "TYT":
                return "1";
            case "LEX": return "4";
            default:
                return " ";
        }
    }
}

class WarrantyClaimManufacturerCSV_PadLeftConverter : ConverterBase
{
    private int _size;
    private string _format;

    public WarrantyClaimManufacturerCSV_PadLeftConverter(int size, string format)
    {
        _size = size;
        _format = format;
    }

    public override object StringToField(string from)
    {
        throw new NotImplementedException();
    }

    public override string FieldToString(object from)
    {
        return string.Format(_format, from)!;

        //if (from is null)
        //    return "";

        //return from!.ToString()!;// string.Format(_format, from)!.PadLeft(_size, '0');
    }
}

public class WarrantyClaimManufacturerCSV_DateConverter : ConverterBase
{
    // Sentinel used by GenerateCSV to mark a pre-delivery date; emitted as 8 zeros per manufacturer spec.
    public static readonly DateTime PreDeliverySentinel = new(1753, 1, 1);

    public override object StringToField(string from)
    {
        throw new NotImplementedException();
    }

    public override string FieldToString(object from)
    {
        if (from is null)
            return "";

        var date = (DateTime)from;

        if (date == PreDeliverySentinel)
            return "00000000";

        return date.ToString("yyyyMMdd");
    }
}

class WarrantyClaimManufacturerCSV_BoolConverter : ConverterBase
{
    public override object StringToField(string from)
    {
        throw new NotImplementedException();
    }

    public override string FieldToString(object from)
    {
        if (from is false)
            return "";

        return "1";
    }
}

[DelimitedRecord(",")]
public class WarrantyClaimManufacturerCSV
{
    [FieldCaption("RECEIVE")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string RECEIVE { get; set; } = "10";


    [FieldCaption("FDIST")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? DistCode { get; set; } = "96157";


    [FieldCaption("TWCNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 7)]
    public string? ClaimNumber { get; set; }


    [FieldCaption("INITPRODT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? InitialProcessDate { get; set; }


    [FieldCaption("PAGE")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 2, "{0:0}")]
    public int? Page { get; set; }


    [FieldCaption("CLMNT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? ClaimantCode { get; set; } = "96157";

    [FieldCaption("FRAN")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_FranchiseConverter))]
    public string? Franchise { get; set; }

    [FieldCaption("WARRTP")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? WarrantyType { get; set; }


    [FieldCaption("DATAID")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? DataID { get; set; }


    [FieldCaption("NONVHCLFLG")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_BoolConverter))]
    public bool NV { get; set; }

    [FieldCaption("OPETYPE")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public int? OperationType { get; set; }


    [FieldCaption("PRODT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? ProcessDate { get; set; }


    [FieldCaption("PROFLG")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public int? ProcessFlg { get; set; }


    [FieldCaption("INVNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 7)]
    public string? InvoiceNo { get; set; }


    [FieldCaption("DLR")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DealerCode { get; set; }


    [FieldCaption("DLRCLMNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DealerClaimNo { get; set; }


    [FieldCaption("CUR")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public int? InvoiceCurrency { get; set; }


    [FieldCaption("KWSRATELOTINV")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 7, "{0:0.0000}")]
    public decimal? ExchangeRate { get; set; }



    [FieldCaption("PRR1")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 5, "{0:0.000}")]
    public decimal? PRR1 { get; set; }


    [FieldCaption("LABRATE")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 7, "{0:0.00}")]
    public decimal? LaborRate { get; set; }



    [FieldCaption("T1CD")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 2)]
    public string? T1 { get; set; }



    [FieldCaption("T2CD")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 2)]
    public string? T2 { get; set; }



    [FieldCaption("WMI")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 3)]
    public string? VIN_WMI { get; set; }


    [FieldCaption("VDS")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 6)]
    public string? VIN_VDS { get; set; }


    [FieldCaption("CD")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? VIN_CD { get; set; }


    [FieldCaption("VIS")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 8)]
    public string? VIN_VIS { get; set; }



    [FieldCaption("PST1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_1 { get; set; }

    [FieldCaption("PST2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_2 { get; set; }

    [FieldCaption("PST3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_3 { get; set; }

    [FieldCaption("PST4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_4 { get; set; }

    [FieldCaption("PST5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_5 { get; set; }

    [FieldCaption("PST6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_6 { get; set; }

    [FieldCaption("PST7")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string? T3_7 { get; set; }


    [FieldCaption("REPROPENDT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? RepairOpenDate { get; set; }


    [FieldCaption("REPCOMPDT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? RepairCompletionDate { get; set; }


    [FieldCaption("DELIDT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? DeliveryDate { get; set; }


    [FieldCaption("MILE")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 6, "{0:0}")]
    public int? Odometer { get; set; }


    [FieldCaption("KLMFLG")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public int? KMFlg { get; set; }


    [FieldCaption("REPRODRNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? RepairOrderNo { get; set; }


    [FieldCaption("PRER_ONO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? ACPreviousRepairOrderNo { get; set; }



    [FieldCaption("PREREPCMPDT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? AcPreviousRepairDate { get; set; }


    [FieldCaption("ACCFSTINSDT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_DateConverter))]
    public DateTime? AcInstallDate { get; set; }


    [FieldCaption("ACCFSTINSMILE")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 6, "{0:0}")]
    public decimal AcInstallKm { get; set; }



    [FieldCaption("PREREPMILE")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 6, "{0:0}")]
    public decimal AcPreviousRepairKm { get; set; }



    [FieldCaption("PREINVNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 7, "{0:0}")]
    public string? AcPreviousInvoiceNo { get; set; }


    [FieldCaption("CURINVNO")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 7, "{0:0}")]
    public string? AcCurrentInvoiceNo { get; set; }



    [FieldCaption("AMTP_NITX")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal? PartsTotalAmount { get; set; }


    [FieldCaption("HRTTL")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 4, "{0:0.0}")]
    public decimal? HourTotal { get; set; }



    [FieldCaption("AMTLAB_NITX")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal? LaborTotalAmount { get; set; }


    [FieldCaption("AMTSBLT_NITX")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal? SubletTotalAmount { get; set; }


    [FieldCaption("AMTGTTL")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal? TotalClaimAmount { get; set; }


    [FieldCaption("ADJFCLMNT_PP")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public int? PartsAdjustment { get; set; }


    [FieldCaption("ADJFCLMNT_PLAB")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public int? LaborAdjustment { get; set; }


    [FieldCaption("ADJFCLMNT_PSBLT")]
    [FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public int? SubletAdjustment { get; set; }


    [FieldCaption("OFPLOCFLG")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? OFPLocalFlag { get; set; }



    [FieldCaption("OFP")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? OFP { get; set; }



    [FieldCaption("PPAYCD1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode1 { get; set; }


    [FieldCaption("BHNLOCFLG1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF1 { get; set; }


    [FieldCaption("HBAN1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo1 { get; set; }


    [FieldCaption("BHNKOSU1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty1 { get; set; }


    [FieldCaption("PAMT1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount1 { get; set; }


    [FieldCaption("MLTPRR1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR1 { get; set; } = "0";




    [FieldCaption("PPAYCD2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode2 { get; set; }


    [FieldCaption("BHNLOCFLG2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF2 { get; set; }


    [FieldCaption("HBAN2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo2 { get; set; }


    [FieldCaption("BHNKOSU2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty2 { get; set; }


    [FieldCaption("PAMT2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount2 { get; set; }


    [FieldCaption("MLTPRR2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR2 { get; set; } = "0";














    [FieldCaption("PPAYCD3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode3 { get; set; }


    [FieldCaption("BHNLOCFLG3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF3 { get; set; }


    [FieldCaption("HBAN3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo3 { get; set; }


    [FieldCaption("BHNKOSU3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty3 { get; set; }


    [FieldCaption("PAMT3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount3 { get; set; }


    [FieldCaption("MLTPRR3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR3 { get; set; } = "0";






    [FieldCaption("PPAYCD4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode4 { get; set; }


    [FieldCaption("BHNLOCFLG4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF4 { get; set; }


    [FieldCaption("HBAN4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo4 { get; set; }


    [FieldCaption("BHNKOSU4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty4 { get; set; }


    [FieldCaption("PAMT4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount4 { get; set; }


    [FieldCaption("MLTPRR4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR4 { get; set; } = "0";









    [FieldCaption("PPAYCD5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode5 { get; set; }


    [FieldCaption("BHNLOCFLG5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF5 { get; set; }


    [FieldCaption("HBAN5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo5 { get; set; }


    [FieldCaption("BHNKOSU5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty5 { get; set; }


    [FieldCaption("PAMT5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount5 { get; set; }


    [FieldCaption("MLTPRR5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR5 { get; set; } = "0";





    [FieldCaption("PPAYCD6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsPayCode6 { get; set; }


    [FieldCaption("BHNLOCFLG6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? PartsLF6 { get; set; }


    [FieldCaption("HBAN6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 15)]
    public string? PartsPartsNo6 { get; set; }


    [FieldCaption("BHNKOSU6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 3, "{0:0}")]
    public decimal PartsQty6 { get; set; }


    [FieldCaption("PAMT6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal PartsAmount6 { get; set; }


    [FieldCaption("MLTPRR6")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 5)]
    public string MultiPRR6 { get; set; } = "0";




    [FieldCaption("LABOPENO_MAIN")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? LaborOperationNoMain { get; set; }




    [FieldCaption("LABPAYCD1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? LaborPayCode1 { get; set; }


    [FieldCaption("LABOPENO1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? LaborOperationNo1 { get; set; }


    [FieldCaption("LABHR1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 4, "{0:0.0}")]
    public decimal LaborHour1 { get; set; }



    [FieldCaption("LABPAYCD2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? LaborPayCode2 { get; set; }


    [FieldCaption("LABOPENO2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? LaborOperationNo2 { get; set; }


    [FieldCaption("LABHR2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 4, "{0:0.0}")]
    public decimal LaborHour2 { get; set; }



    [FieldCaption("LABPAYCD3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? LaborPayCode3 { get; set; }


    [FieldCaption("LABOPENO3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? LaborOperationNo3 { get; set; }


    [FieldCaption("LABHR3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 4, "{0:0.0}")]
    public decimal LaborHour3 { get; set; }

















    [FieldCaption("SBPAYCD1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? SubletPayCode1 { get; set; }


    [FieldCaption("SBLTTP1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 2)]
    public string? SubletType1 { get; set; }


    [FieldCaption("SBLTINVNO1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 7)]
    public string? SubletInvoiceNo1 { get; set; }


    [FieldCaption("SBLTAMT1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal SubletAmount1 { get; set; }


    [FieldCaption("SBLTDESCRPTN1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 50)]
    public string? SubletDescription { get; set; }




    [FieldCaption("SBLTPAYCD2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? SubletPayCode2 { get; set; }


    [FieldCaption("SBLTTP2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 2)]
    public string? SubletType2 { get; set; }


    [FieldCaption("SBLTINVNO2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 7)]
    public string? SubletInvoiceNo2 { get; set; }


    [FieldCaption("SBLTAMT2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadLeftConverter), 9, "{0:0.00}")]
    public decimal SubletAmount2 { get; set; }


    [FieldCaption("SBLTDESCRPTN2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 50)]
    public string? SubletDescription2 { get; set; }


    [FieldCaption("CNDTIONDSC")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 75)]
    public string? Condition { get; set; }


    [FieldCaption("CSEDSC")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 75)]
    public string? Cause { get; set; }


    [FieldCaption("RMDDSC")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 75)]
    public string? Remedy { get; set; }


    [FieldCaption("FDSCMT")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 75)]
    public string? DistComment1 { get; set; }



    [FieldCaption("EVIFLG")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? EvidenceFlag { get; set; } = "0";




    [FieldCaption("WARRAPPFLG1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? AP1 { get; set; }

    [FieldCaption("WARRAPPFLG2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? AP2 { get; set; }


    [FieldCaption("WARRAPPFLG3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? AP3 { get; set; }


    [FieldCaption("WARRAPPFLG4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? AP4 { get; set; }


    [FieldCaption("WARRAPPFLG5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 1)]
    public string? AP5 { get; set; }



    [FieldCaption("BTRYTESTCD1_1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 20)]
    public string? BatteryTestCode11 { get; set; }


    [FieldCaption("BTRYTESTCD1_2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 20)]
    public string? BatteryTestCode12 { get; set; }


    [FieldCaption("BTRYTESTCD2_1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 20)]
    public string? BatteryTestCode21 { get; set; }


    [FieldCaption("BTRYTESTCD2_2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 20)]
    public string? BatteryTestCode22 { get; set; }



    [FieldCaption("DIAGCD1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DiagCode1 { get; set; } = "";


    [FieldCaption("DIAGCD2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DiagCode2 { get; set; } = "";

    [FieldCaption("DIAGCD3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DiagCode3 { get; set; } = "";


    [FieldCaption("DIAGCD4")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DiagCode4 { get; set; } = "";


    [FieldCaption("DIAGCD5")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 10)]
    public string? DiagCode5 { get; set; } = "";


    [FieldCaption("TSBNO_D_")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 20)]
    public string? TSB { get; set; } = "";



    [FieldCaption("SNO1")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 30)]
    public string? Serial1 { get; set; } = "";


    [FieldCaption("SNO2")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 30)]
    public string? Serial2 { get; set; } = "";


    [FieldCaption("SNO3")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(typeof(WarrantyClaimManufacturerCSV_PadRightConverter), 30)]
    public string? Serial3 { get; set; } = "";
}
