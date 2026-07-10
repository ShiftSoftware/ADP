using AutoMapper;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.AutoMapperProfiles;

/// <summary>
/// The Financial list maps (entity -> Distributor/DealerFinancialListDTO). Moved from the host
/// consumer's own AutoMapper profile (Phase 3 Slice 3.5, D23): the Financial DTOs genericized
/// lock-step with the entity, so the org-name ForMember bridges the host carried after the Phase 3
/// rename dissolved back into convention matching. Only three kinds of explicit config remain:
/// the DateTime -> DateTimeOffset conversions (Process/DistributorProcessDate), the
/// reference-claim-number flattening pin (the member no longer decomposes to a valid
/// nav + property path by convention), and the Dealer map's Ignore list.
/// Gated by the host consumer's FinancialMapping tests.
/// </summary>
public class Financial : Profile
{
    public Financial()
    {
        CreateMap<Entities.WarrantyClaim, DistributorFinancialListDTO>()
            .ForMember(
                x => x.ProcessDate,
                x => x.MapFrom(y => y!.ProcessDate.HasValue ? new DateTimeOffset(y.ProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(
                x => x.DistributorProcessDate,
                x => x.MapFrom(y => y!.DistributorProcessDate.HasValue ? new DateTimeOffset(y.DistributorProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(x => x.ReferenceWarrantyClaimNumber, x => x.MapFrom(y => y.ReferenceWarrantyClaim!.ClaimNumber));

        // The Dealer map keeps its Ignore list EXACTLY as before (dealers never see the
        // distributor-side figures); the ignored members stay null even though the entity
        // carries values.
        CreateMap<Entities.WarrantyClaim, DealerFinancialListDTO>()
            .ForMember(
                x => x.ProcessDate,
                x => x.MapFrom(y => y!.ProcessDate.HasValue ? new DateTimeOffset(y.ProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(
                x => x.DistributorProcessDate,
                x => x.MapFrom(y => y!.DistributorProcessDate.HasValue ? new DateTimeOffset(y.DistributorProcessDate.Value, TimeSpan.Zero) : (DateTimeOffset?)null)
            )
            .ForMember(x => x.ReferenceWarrantyClaimNumber, x => x.MapFrom(y => y.ReferenceWarrantyClaim!.ClaimNumber))
            .ForMember(x => x.DistComment1, x => x.Ignore())
            .ForMember(x => x.HourTotalDistributor, x => x.Ignore())
            .ForMember(x => x.LaborTotalAmountDistributor, x => x.Ignore())
            .ForMember(x => x.SubletTotalAmountDistributor, x => x.Ignore())
            .ForMember(x => x.PartsTotalAmountDistributor, x => x.Ignore());
    }
}
