using AutoMapper;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.ClaimableItems.Data.AutoMapperProfiles;

public class CampaignProfile : Profile
{
    public CampaignProfile()
    {
        CreateMap<Campaign, ShiftSoftware.ADP.Models.Vehicle.ServiceCampaignModel>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.ID.ToString()))
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => GeneralMappingHelper.DeserializeDict(src.Name)))

            .ForMember(dest => dest.BrandIDs, opt => opt.MapFrom(src => src!.Brands.Select(y => (long?)y)))

            .ForMember(dest => dest.CountryIDs, opt => opt.MapFrom(src => src!.Countries.Select(y => (long?)y)))
            .ForMember(dest => dest.CompanyIDs, opt => opt.MapFrom(src => src!.Companies.Select(y => (long?)y)))
            .ForMember(dest => dest.VehicleInspectionTypeID, opt => opt.MapFrom(src => src.VehicleInspectionTypeID))
            ;

        CreateMap<Campaign, ShiftSoftware.ADP.Models.Vehicle.ServiceItemModel>()
            .ConvertUsing((src, dest) =>
            {
                dest.CampaignName = GeneralMappingHelper.DeserializeDict(src.Name);
                dest.CampaignUniqueReference = src.UniqueReference;
                dest.CampaignStartDate = src.StartDate;
                dest.CampaignEndDate = src.ExpireDate;
                dest.CampaignActivationTrigger = src.ActivationTrigger;
                dest.CampaignActivationType = src.ActivationType;
                dest.BrandIDs = src.Brands.Select(x => (long?)x);
                dest.CompanyIDs = src.Companies.Select(y => new Nullable<long>(y));
                dest.CountryIDs = src.Countries.Select(y => new Nullable<long>(y));

                dest.VehicleInspectionTypeID = src.VehicleInspectionTypeID;

                return dest;
            });

        // Entity <-> DTO map (moved here from Services.Data/AutoMapperProfiles/WarrantyClaim.cs, lines 114-123).
        CreateMap<Campaign, CampaignDTO>()
            .ForMember(x => x.Brands, x => x.MapFrom(y => y.Brands.Select(y => new ShiftEntitySelectDTO { Value = y.ToString() }).ToList()))
            .ForMember(x => x.Companies, x => x.MapFrom(y => y.Companies.Select(y => new ShiftEntitySelectDTO { Value = y.ToString() }).ToList()))
            .ForMember(x => x.Countries, x => x.MapFrom(y => y.Countries.Select(y => new ShiftEntitySelectDTO { Value = y.ToString() }).ToList()))
            .DefaultEntityToDtoAfterMap()
            .ReverseMap()
            .ForMember(x => x.Brands, x => x.MapFrom(y => y.Brands.Select(y => y.Value.ToLong()).ToList()))
            .ForMember(x => x.Companies, x => x.MapFrom(y => y.Companies.Select(y => y.Value.ToLong()).ToList()))
            .ForMember(x => x.Countries, x => x.MapFrom(y => y.Countries.Select(y => y.Value.ToLong()).ToList()))
            .DefaultDtoToEntityAfterMap();
    }
}
