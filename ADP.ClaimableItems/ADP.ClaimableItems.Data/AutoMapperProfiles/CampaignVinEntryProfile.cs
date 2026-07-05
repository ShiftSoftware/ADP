using AutoMapper;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;

namespace ShiftSoftware.ADP.ClaimableItems.Data.AutoMapperProfiles;

public class CampaignVinEntryProfile : Profile
{
    public CampaignVinEntryProfile()
    {
        CreateMap<CampaignVinEntry, CampaignVinEntryListDTO>()
            .ForMember(x => x.CampaignName, x => x.MapFrom(s => s.Campaign != null ? s.Campaign.Name : null))
            .ForMember(x => x.CampaignUniqueReference, x => x.MapFrom(s => s.Campaign != null ? s.Campaign.UniqueReference : null));

        CreateMap<CampaignVinEntry, ShiftSoftware.ADP.Models.Vehicle.CampaignVinEntryModel>()
            .ConvertUsing(x => new ShiftSoftware.ADP.Models.Vehicle.CampaignVinEntryModel
            {
                id = x.ID.ToString(),
                VIN = x.VIN,
                CampaignID = x.CampaignID,
                CampaignUniqueReference = x.Campaign != null ? x.Campaign.UniqueReference : null,
                RecordedDate = x.RecordedDate,
                CompanyID = x.CompanyID,
                IsDeleted = x.IsDeleted,
            });
    }
}
