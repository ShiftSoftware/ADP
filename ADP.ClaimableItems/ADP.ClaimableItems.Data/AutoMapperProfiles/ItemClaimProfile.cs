using AutoMapper;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.ClaimableItems.Data.AutoMapperProfiles;

/// <summary>
/// Item-claim maps. Moved from the original host application's Services.Data.AutoMapperProfiles.ItemClaim (Phase 2 Slice 5).
/// The ItemClaimModel map's composite Cosmos doc id is a LIVE prod document-identity contract —
/// byte-frozen (note it INCLUDES CampaignVinEntryID while the SQL UniqueHash does not; D16).
/// The ListDTO map's VehicleInspectionResult* flattened members have no source here (consumer-owned
/// navigation) — the consumer's derived repository overrides MapToList to fill them.
/// </summary>
public class ItemClaimProfile : Profile
{
    public ItemClaimProfile()
    {
        CreateMap<Entities.ItemClaim, ItemClaimListDTO>()
            .ForMember(
                x => x.HasAttachment,
                x => x.MapFrom(x => x.Attachments == null || x.Attachments == "[]" ? YesNoOptions.No : YesNoOptions.Yes)
            );

        CreateMap<Entities.ItemClaim, ItemClaimModel>()
            .ConvertUsing(x => new ShiftSoftware.ADP.Models.Vehicle.ItemClaimModel
            {
                id = $"{x.VIN}-{x.CampaignID}-{x.ClaimableItemID}-{x.VehicleInspectionResultID}-{x.CampaignVinEntryID}-{x.ClaimableItemContractID}",
                VIN = x.VIN,
                CompanyID = x.CompanyID,
                BranchID = x.CompanyBranchID,
                ClaimDate = x.ClaimDate,
                Cost = x.Cost ?? 0m,
                PackageCode = x.PackageCode,
                InvoiceNumber = x.InvoiceNumber,
                JobNumber = x.JobNumber,
                QRCode = x.QRCode,
                ServiceItemID = x.ClaimableItemID.ToString(),
                VehicleInspectionID = x.VehicleInspectionResultID == null ? null : x.VehicleInspectionResultID.ToString(),
                CampaignVinEntryID = x.CampaignVinEntryID == null ? null : x.CampaignVinEntryID.ToString(),
                IsDeleted = x.IsDeleted,
            });
    }
}
