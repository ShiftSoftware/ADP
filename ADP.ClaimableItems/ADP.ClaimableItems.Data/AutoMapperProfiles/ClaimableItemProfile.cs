using AutoMapper;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json;

namespace ShiftSoftware.ADP.ClaimableItems.Data.AutoMapperProfiles;

public class ClaimableItemProfile : Profile
{
    public ClaimableItemProfile()
    {
        CreateMap<ClaimableItem, ShiftSoftware.ADP.Models.Vehicle.ServiceItemModel>()
            .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.ID.ToString()))
            .ForMember(dest => dest.IntegrationID, opt => opt.MapFrom(src => src.ID))

            .ForMember(dest => dest.CampaignStartDate, opt => opt.MapFrom(src => src.Campaign!.StartDate))
            .ForMember(dest => dest.CampaignEndDate, opt => opt.MapFrom(src => src.Campaign!.ExpireDate))



            .ForMember(dest => dest.ActiveFor, opt => opt.MapFrom(src => src.ValidityMode == ClaimableItemValidityMode.RelativeToActivation ? new Nullable<int>(src.ActiveFor) : null))
            .ForMember(dest => dest.ActiveForDurationType, opt => opt.MapFrom(src => src.ValidityMode == ClaimableItemValidityMode.RelativeToActivation ? new Nullable<DurationType>(src.ActiveForDurationType) : null))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidityMode == ClaimableItemValidityMode.FixedDateRange ? src.ValidFrom : null))
            .ForMember(dest => dest.ValidTo, opt => opt.MapFrom(src => src.ValidityMode == ClaimableItemValidityMode.FixedDateRange ? src.ValidTo : null))



            .ForMember(dest => dest.CampaignUniqueReference, opt => opt.MapFrom(src => src.Campaign!.UniqueReference))

            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => GeneralMappingHelper.DeserializeDict(src.Name)))
            .ForMember(dest => dest.CampaignName, opt => opt.MapFrom(src => GeneralMappingHelper.DeserializeDict(src.Campaign!.Name)))
            .ForMember(dest => dest.PrintoutTitle, opt => opt.MapFrom(src => src.PrintoutTitle == null ? null : GeneralMappingHelper.DeserializeDict(src.PrintoutTitle)))
            .ForMember(dest => dest.PrintoutDescription, opt => opt.MapFrom(src => src.PrintoutDescription == null ? null : GeneralMappingHelper.DeserializeDict(src.PrintoutDescription)))

            .ForMember(dest => dest.BrandIDs, opt => opt.MapFrom(src => src.Campaign!.Brands.Select(y => (long?)y)))
            .ForMember(dest => dest.CountryIDs, opt => opt.MapFrom(src => src.Campaign!.Countries.Select(y => (long?)y)))
            .ForMember(dest => dest.CompanyIDs, opt => opt.MapFrom(src => src.Campaign!.Companies.Select(y => (long?)y)))

            .ForMember(dest => dest.FixedCost, opt => opt.MapFrom(src =>
                src.CostingType == ClaimableItemCostingType.Fixed ? src.FixedCost : null))

            .ForMember(dest => dest.ModelCosts, opt => opt.MapFrom(src => MappingHelpers.DeserializeModelCosts(src.Costs, src.CostingType, src.ID)))!


            .ForMember(dest => dest.CampaignActivationTrigger, opt => opt.MapFrom(src => src.Campaign!.ActivationTrigger))
            .ForMember(dest => dest.CampaignActivationType, opt => opt.MapFrom(src => src.Campaign!.ActivationType))
            .ForMember(dest => dest.AttachmentFieldBehavior, opt => opt.MapFrom(src => src.AttachmentFieldBehavior))
            .ForMember(dest => dest.VehicleInspectionTypeID, opt => opt.MapFrom(src => src.Campaign!.VehicleInspectionTypeID));

        // Entity <-> DTO map (moved here from Services.Data/AutoMapperProfiles/WarrantyClaim.cs, lines 107-112).
        CreateMap<ClaimableItem, ClaimableItemDTO>()
            .ForMember(x => x.Costs, x => x.MapFrom(y => JsonSerializer.Deserialize<List<ClaimableItemCostDTO>>(y.Costs, new JsonSerializerOptions { })))
            .DefaultEntityToDtoAfterMap()
            .ReverseMap()
            .ForMember(x => x.Costs, x => x.MapFrom(y => JsonSerializer.Serialize(y.Costs, new JsonSerializerOptions { })))
            .DefaultDtoToEntityAfterMap();
    }

    public static class MappingHelpers
    {
        public static IEnumerable<ShiftSoftware.ADP.Models.Vehicle.ServiceItemCostModel>? DeserializeModelCosts(
           string? json,
           ClaimableItemCostingType costingType,
           long serviceItemId
        )
        {
            if (costingType == ClaimableItemCostingType.Fixed || string.IsNullOrEmpty(json))
                return null;

            var list = JsonSerializer.Deserialize<List<ClaimableItemCostDTO>>(json);
            if (list == null)
                return null;

            return list.Select(y => new ShiftSoftware.ADP.Models.Vehicle.ServiceItemCostModel
            {
                Cost = y.Cost,
                Katashiki = y.Katashiki,
                Variant = y.Variant,
                ServiceItemID = serviceItemId,
                PackageCode = y.PackageCode,
            });
        }
    }
}
