using FluentValidation;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

public class WarrantyClaimPartLineDTO
{
    public long ID { get; set; }
    public string? PayCode { get; set; }
    public bool OFP { get; set; }
    public string? LocalF { get; set; }
    public string? PartNumber { get; set; }
    public string? PartDescription { get; set; }
    public int? Qty { get; set; }
    public decimal? Price { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DistributorPrice { get; set; }


    [JsonIgnore]
    public bool Loading { get; set; }

    public bool FoundInLookup { get; set; }
}

// PartNumber -> CSV HBAN, which is exported, so it must not contain Cyrillic. PartDescription is NOT
// exported (no CSV column) and is intentionally left unrestricted. Wired in via
// WarrantyClaimValidator.RuleForEach(x => x.WarrantyClaimPartLines).
public class WarrantyClaimPartLineValidator : AbstractValidator<WarrantyClaimPartLineDTO>
{
    public WarrantyClaimPartLineValidator()
    {
        RuleFor(x => x.PartNumber)
            .Must(value => !WarrantyClaimValidator.ContainsCyrillic(value))
            .WithMessage(WarrantyClaimValidator.CyrillicNotAllowedMessage);
    }
}
