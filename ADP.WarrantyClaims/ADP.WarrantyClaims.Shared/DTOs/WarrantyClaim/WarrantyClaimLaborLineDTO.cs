using FluentValidation;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim
{
    public class WarrantyClaimLaborLineDTO
    {
        public long ID { get; set; }
        public string? PayCode { get; set; }
        public bool MainOperation { get; set; }
        public string? OperationNumber { get; set; }
        public decimal? Hour { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? DistributorHour { get; set; }
    }

    // OperationNumber -> CSV LABOPENO. It is selected from the flat-rate lookup, but it is still
    // exported, so reject Cyrillic for consistency with the other CSV-bound fields. Wired in via
    // WarrantyClaimValidator.RuleForEach(x => x.WarrantyClaimLaborLines).
    public class WarrantyClaimLaborLineValidator : AbstractValidator<WarrantyClaimLaborLineDTO>
    {
        public WarrantyClaimLaborLineValidator()
        {
            RuleFor(x => x.OperationNumber)
                .Must(value => !WarrantyClaimValidator.ContainsCyrillic(value))
                .WithMessage(WarrantyClaimValidator.CyrillicNotAllowedMessage);
        }
    }
}
