using FluentValidation;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

public class WarrantyClaimDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string? ClaimNumber { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string? DealerCode { get; set; } = default!;
    public string? DealerClaimNo { get; set; } = default!;
    public DateTime? DateOfReceipt { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProcessFlags ProcessFlg { get; set; } = ProcessFlags.FirstSubmissionFromDealer;
    public string WarrantyType { get; set; } = default!;
    public string Franchise { get; set; } = default!;

    public string? AP1 { get; set; }
    public string? AP2 { get; set; }
    public string? AP3 { get; set; }
    public string? AP4 { get; set; }
    public string? AP5 { get; set; }
    public bool NV { get; set; }
    public int? FV { get; set; }
    public string VIN_WMI { get; set; } = default!;
    public string VIN_VDS { get; set; } = default!;
    public string VIN_CD { get; set; } = default!;
    public string VIN_VIS { get; set; } = default!;
    public DateTime? DeliveryDate { get; set; }
    public bool PreDelivery { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KMFlags KMFlg { get; set; } = KMFlags.K;
    public string RepairOrderNo { get; set; } = default!;
    public string DataID { get; set; } = DataIDs.W.Key;
    public List<WarrantyClaimLaborLineDTO> WarrantyClaimLaborLines { get; set; } = new();
    public List<WarrantyClaimSubletLineDTO> WarrantyClaimSubletLines { get; set; } = new();
    public List<WarrantyClaimPartLineDTO> WarrantyClaimPartLines { get; set; } = new();
    public string? LaborOperationNoMain { get; set; }
    public decimal LaborRate { get => Franchise == Franchises.Toyota.Key ? 25 : 25; }
    public decimal LaborRateJPY { get => LaborRate * this.LaborExchangeRate; }
    public decimal HourTotal { get => WarrantyClaimLaborLines.Sum(x => x.Hour ?? 0m); }
    public decimal HourTotalDistributor { get => WarrantyClaimLaborLines.Sum(x => x.DistributorHour ?? 0m); }
    public decimal? LaborTotalAmount { get => HourTotal * LaborRate; }
    public decimal? LaborTotalAmountDistributor { get => HourTotalDistributor * LaborRate; }

    [JsonIgnore]
    public decimal? LaborTotalAmountDistributorJPY { get => LaborTotalAmountDistributor * this.LaborExchangeRate; }

    public decimal? SubletTotalAmount { get => WarrantyClaimSubletLines.Sum(x => x.Amount); }
    public decimal? SubletTotalAmountJPY { get => SubletTotalAmount * this.SubletExchangeRate; }

    public decimal? SubletTotalAmountDistributor { get => WarrantyClaimSubletLines.Sum(x => x.DistributorAmount); }
    public decimal? SubletTotalAmountDistributorJPY { get => SubletTotalAmountDistributor * this.SubletExchangeRate; }


    public string? SubletDescription { get; set; }
    public string? T1 { get; set; }
    public string? T2 { get; set; }
    public string? T3_1 { get; set; }
    public string? T3_2 { get; set; }
    public string? T3_3 { get; set; }
    public string? T3_4 { get; set; }
    public string? T3_5 { get; set; }
    public string? T3_6 { get; set; }
    public string? T3_7 { get; set; }
    public string Condition { get; set; } = default!;
    public string Cause { get; set; } = default!;
    public string Remedy { get; set; } = default!;
    public string? OFPLocalFlag { get; set; }
    public string? OFP { get; set; }
    public decimal? PartsTotalAmount { get => WarrantyClaimPartLines.Sum(x => (x.Price ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsSubTotalAmountDistributor { get => WarrantyClaimPartLines.Sum(x => (x.DistributorPrice ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsTotalAmountDistributor { get => WarrantyClaimPartLines.Sum(x => (x.DistributorPrice ?? 0m) * (x.Qty ?? 0) * PRR1); }
    public decimal? PartsTotalAmountDistributorJPY { get => PartsTotalAmountDistributor * this.PartExchangeRate; }
    public decimal? TotalClaimAmount { get => LaborTotalAmount + SubletTotalAmount + PartsTotalAmount; }
    public decimal? TotalClaimAmountDistributor { get => LaborTotalAmountDistributor + SubletTotalAmountDistributor + PartsTotalAmountDistributor; }
    public decimal? TotalClaimAmountDistributorJPY { get => LaborTotalAmountDistributorJPY + SubletTotalAmountDistributorJPY + PartsTotalAmountDistributorJPY; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? DistributorProcessDate { get; set; }
    public int LaborAdjustment { get; set; } = 100;
    public int SubletAdjustment { get; set; } = 100;
    public int PartsAdjustment { get; set; } = 100;
    public string? DistComment1 { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus? ClaimStatus { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }
    public DateTime? AcInstallDate { get; set; }
    public int? AcInstallKm { get; set; }
    public string? ACPreviousRepairOrderNo { get; set; }
    public DateTime? AcPreviousRepairDate { get; set; }
    public int? AcPreviousRepairKm { get; set; }
    public string? AcPreviousInvoiceNo { get; set; }
    public string? AcCurrentInvoiceNo { get; set; }
    public bool? SpecialServiceCampaign { get; set; }
    public string? SSCCampaignCode { get; set; }
    public long? FreeServiceRegisteredVehicleId { get; set; }
    public int? FreeServiceBreakPart { get; set; }
    public string? DealerComments { get; set; }
    public string VIN { get => $"{VIN_WMI}{VIN_VDS}{VIN_CD}{VIN_VIS}"; }
    public string? ModelCode { get; set; }
    public int? YearModel { get; set; }
    public string? Katashiki { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationTypes OperationType { get; set; }

    public string? BatteryTestCode11 { get; set; }
    public string? BatteryTestCode12 { get; set; }
    public string? BatteryTestCode21 { get; set; }
    public string? BatteryTestCode22 { get; set; }
    public string? TSB { get; set; }

    public int? InvoiceCurrency { get; set; } = 1;
    public decimal PRR1 { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }

    public string? ManufacturerErrorMessage { get; set; }

    public ShiftEntitySelectDTO? ReferenceWarrantyClaim { get; set; }

    public List<ShiftFileDTO>? Attachments { get; set; }
}

public class WarrantyClaimValidator : AbstractValidator<WarrantyClaimDTO>
{
    // Shared by the line-item validators (sublet/part/labor) so every CSV-bound field reports the
    // same reason.
    public const string CyrillicNotAllowedMessage =
        "Cyrillic characters are not allowed (this field is exported to the manufacturer CSV).";

    public WarrantyClaimValidator()
    {
        RuleFor(x => x.InvoiceCurrency)
            .NotNull();

        RuleFor(x => x.PRR1)
            .NotEmpty();

        RuleFor(x => x.LaborExchangeRate)
            .NotEmpty();

        RuleFor(x => x.PartExchangeRate)
            .NotEmpty();

        RuleFor(x => x.SubletExchangeRate)
            .NotEmpty();

        //RuleFor(x => x.ClaimNumber)
        //    .NotEmpty()
        //    .Length(7);

        RuleFor(x => x.DealerClaimNo)
            .MaximumLength(10);

        RuleFor(x => x.WarrantyType)
            .NotEmpty();

        RuleFor(x => x.OperationType)
            .NotEqual(x => OperationTypes.NotSpecified);

        RuleFor(x => x.Franchise)
            .NotEmpty();

        RuleFor(x => x.VIN_WMI)
            .NotEmpty()
            .Length(3)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.VIN_VDS)
            .NotEmpty()
            .Length(x => x.VIN_CD?.Length == 0 ? 6 : 5)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV))
            .WithMessage("Should be 6 characters when CD is not provided or 5 when CD is provided.");

        RuleFor(x => x.VIN_CD)
            .Length(x => x.VIN_VDS?.Length == 5 ? 1 : 0)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV))
            .WithMessage("Should be empty if VDS is 6 characters. or 1 character if VDS is 5 characters");

        RuleFor(x => x.VIN_VIS)
            .NotEmpty()
            .Length(8)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.DeliveryDate)
            .NotNull()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV || x.PreDelivery));

        RuleFor(x => x.RepairDate)
            .NotNull();
        //.When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.Odometer)
            .NotNull()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.RepairOrderNo)
            .NotEmpty()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        // Condition / Cause / Remedy are non-nullable strings on the DTO (and NOT NULL in the DB), so the
        // server's [ApiController] model binding already rejects them when null. Validate them client-side
        // too, otherwise the form submits and the requirement only surfaces as a server error afterwards.
        RuleFor(x => x.Condition)
            .NotEmpty();

        RuleFor(x => x.Cause)
            .NotEmpty();

        RuleFor(x => x.Remedy)
            .NotEmpty();

        RuleFor(x => x.AcPreviousRepairDate)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key || x.WarrantyType == WarrantyTypes.P2.Key);

        RuleFor(x => x.ACPreviousRepairOrderNo)
            .NotEmpty()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key);

        RuleFor(x => x.AcPreviousRepairKm)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key);

        RuleFor(x => x.AcPreviousInvoiceNo)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P2.Key);

        RuleFor(x => x.AcCurrentInvoiceNo)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P2.Key);

        // These fields are exported to the manufacturer CSV (see WarrantyClaimManufacturerCSV /
        // WarrantyClaimService.GenerateCSV), which is then imported into the manufacturer system
        // that rejects Cyrillic. This is the inverse of ServiceActivationValidator, which *enforces*
        // Cyrillic on customer names: here we reject any value that lands in the CSV and contains it.
        // NOTE: line-item collections (parts/sublet/labor) also land in the CSV but are not covered
        // yet — the project has no child-validator wiring (RuleForEach/SetValidator).
        void NoCyrillic(Expression<Func<WarrantyClaimDTO, string?>> field) =>
            RuleFor(field)
                .Must(value => !ContainsCyrillic(value))
                .WithMessage(CyrillicNotAllowedMessage);

        NoCyrillic(x => x.InvoiceNo);
        NoCyrillic(x => x.DealerCode);
        NoCyrillic(x => x.DealerClaimNo);
        NoCyrillic(x => x.VIN_WMI);
        NoCyrillic(x => x.VIN_VDS);
        NoCyrillic(x => x.VIN_CD);
        NoCyrillic(x => x.VIN_VIS);
        NoCyrillic(x => x.RepairOrderNo);
        NoCyrillic(x => x.ACPreviousRepairOrderNo);
        NoCyrillic(x => x.AcPreviousInvoiceNo);
        NoCyrillic(x => x.AcCurrentInvoiceNo);
        NoCyrillic(x => x.T1);
        NoCyrillic(x => x.T2);
        NoCyrillic(x => x.T3_1);
        NoCyrillic(x => x.T3_2);
        NoCyrillic(x => x.T3_3);
        NoCyrillic(x => x.T3_4);
        NoCyrillic(x => x.T3_5);
        NoCyrillic(x => x.T3_6);
        NoCyrillic(x => x.T3_7);
        NoCyrillic(x => x.Condition);
        NoCyrillic(x => x.Cause);
        NoCyrillic(x => x.Remedy);
        NoCyrillic(x => x.DistComment1);
        NoCyrillic(x => x.BatteryTestCode11);
        NoCyrillic(x => x.BatteryTestCode12);
        NoCyrillic(x => x.BatteryTestCode21);
        NoCyrillic(x => x.BatteryTestCode22);
        NoCyrillic(x => x.TSB);

        // Line-item collections also flow into the manufacturer CSV (see WarrantyClaimService.GenerateCSV):
        // sublet Description/InvoiceNo, part PartNumber, labor OperationNumber. ShiftBlazor maps a changed
        // row field to a path like "WarrantyClaimSubletLines[i].Description" and validates it against these
        // child validators, so per-row errors surface inline; full-model validation on save covers all rows.
        RuleForEach(x => x.WarrantyClaimSubletLines).SetValidator(new WarrantyClaimSubletLineValidator());
        RuleForEach(x => x.WarrantyClaimPartLines).SetValidator(new WarrantyClaimPartLineValidator());
        RuleForEach(x => x.WarrantyClaimLaborLines).SetValidator(new WarrantyClaimLaborLineValidator());
    }

    // Inverse of ServiceActivationValidator.IsCyrillicText: returns true if the value contains at
    // least one Cyrillic character. Same Unicode blocks; empty/whitespace is treated as clean so
    // optional fields don't error.
    public static bool ContainsCyrillic(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        foreach (var c in input)
        {
            if ((c >= 0x0400 && c <= 0x04FF) || // Cyrillic
                (c >= 0x0500 && c <= 0x052F) || // Cyrillic Supplement
                (c >= 0x2DE0 && c <= 0x2DFF) || // Cyrillic Extended-A
                (c >= 0xA640 && c <= 0xA69F) || // Cyrillic Extended-B
                (c >= 0x1C80 && c <= 0x1C8F))   // Cyrillic Extended-C
                return true;
        }

        return false;
    }
}
