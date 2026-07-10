using FluentValidation;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim
{
    public class WarrantyClaimSubletLineDTO
    {
        public long ID { get; set; }
        public string? PayCode { get; set; }
        public string? SubletType { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal? Amount { get; set; }

        // Distributor amount. Only populated for distributors; nulled out for dealers (see
        // WarrantyClaimRepository.ViewAsync) and defaulted to Amount on dealer save. Mirrors part DistributorPrice / labor DistributorHour.
        public decimal? DistributorAmount { get; set; }
        public string? Description { get; set; }
    }

    // Description -> CSV SBLTDESCRPTN, InvoiceNo -> CSV SBLTINVNO. Both are exported, so neither may
    // contain Cyrillic. Wired in via WarrantyClaimValidator.RuleForEach(x => x.WarrantyClaimSubletLines).
    public class WarrantyClaimSubletLineValidator : AbstractValidator<WarrantyClaimSubletLineDTO>
    {
        public WarrantyClaimSubletLineValidator()
        {
            RuleFor(x => x.Description)
                .Must(value => !WarrantyClaimValidator.ContainsCyrillic(value))
                .WithMessage(WarrantyClaimValidator.CyrillicNotAllowedMessage);

            RuleFor(x => x.InvoiceNo)
                .Must(value => !WarrantyClaimValidator.ContainsCyrillic(value))
                .WithMessage(WarrantyClaimValidator.CyrillicNotAllowedMessage);
        }
    }
}
