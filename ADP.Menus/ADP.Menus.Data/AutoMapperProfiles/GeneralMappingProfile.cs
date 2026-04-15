using AutoMapper;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.BrandMapping;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRateMapping;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVersion;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceIntervalGroup;
using ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.AutoMapperProfiles;

public class GeneralMappingProfile : Profile
{
    public GeneralMappingProfile()
    {
        CreateMap<MenuItemPart, MenuItemPartDTO>().ReverseMap();
        CreateMap<MenuItemPartCountryPrice, PartPriceByCountryDTO>().ReverseMap();
        CreateMap<ReplacementItemVehicleModelPart, ReplacementItemDefaultPartDTO>().ReverseMap();

        CreateMap<ReplacementItem, ReplacementItemListDTO>()
            .ForMember(
            x => x.StandaloneReplacementItemGroup,
            x => x.MapFrom(o => o.StandaloneReplacementItemGroup != null ? new ShiftEntitySelectDTO {Value= o.StandaloneReplacementItemGroup.ID.ToString(), Text= o.StandaloneReplacementItemGroup.Name } : (ShiftEntitySelectDTO?)null));
        CreateMap<ReplacementItem, ReplacementItemDTO>()
            .ForMember(
                x => x.ServiceIntervalGroups,
                x => x.MapFrom(src => src.ReplacementItemServiceIntervalGroups
                    .Select(s => new ServiceIntervalGroupReplacaementItemDTO(s.ServiceIntervalGroupID.ToString())))
            )
            .ForMember(
                x => x.StandaloneReplacementItemGroup,
                x => x.MapFrom(src => src.StandaloneReplacementItemGroup != null ? new ShiftEntitySelectDTO { Value = src.StandaloneReplacementItemGroup.ID.ToString() } : null)
            )
        .ReverseMap()
            .ForMember(
                x => x.ReplacementItemServiceIntervalGroups,
                x => x.Ignore()
            )
            .ForMember(
                x => x.StandaloneReplacementItemGroupID,
                x => x.MapFrom(src => src.StandaloneReplacementItemGroup != null ? src.StandaloneReplacementItemGroup.Value.ToLong() : (long?)null)
            )
            .AfterMap((src, dest, ctx) =>
            {
                // Handle ReplacementItemServiceIntervalGroups mapping
                dest.ReplacementItemServiceIntervalGroups ??= [];

                // 1. Remove items that are not in the source
                var serviceIntervalItemsToRemove = dest.ReplacementItemServiceIntervalGroups
                    .Where(existing => !src.ServiceIntervalGroups.Any(r => r.ID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();

                foreach (var item in serviceIntervalItemsToRemove)
                    dest.ReplacementItemServiceIntervalGroups.Remove(item);

                // 2. Add new items
                foreach (var item in src.ServiceIntervalGroups)
                {
                    var existingItem = dest.ReplacementItemServiceIntervalGroups
                        .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ID);
                    if (existingItem == null)
                        dest.ReplacementItemServiceIntervalGroups.Add(new ReplacementItemServiceIntervalGroup { ServiceIntervalGroupID = item.ID.ToLong() });
                }
            });
        CreateMap<ReplacementItem, MenuItemReplacementItemDTO>()
            .ForMember(
                x => x.StandaloneReplacementItemGroup,
                x => x.MapFrom(s => s.StandaloneReplacementItemGroup == null? null : new ShiftEntitySelectDTO
                {
                    Text = s.StandaloneReplacementItemGroup != null ? s.StandaloneReplacementItemGroup.Name : null,
                    Value = s.StandaloneReplacementItemGroupID != null ? s.StandaloneReplacementItemGroupID.ToString() : null
                })
            )
            .ReverseMap();

        CreateMap<VehicleModelLabourDetails, LabourDetailsDTO>().ReverseMap();
        CreateMap<MenuLabourDetails, LabourDetailsDTO>().ReverseMap();
        CreateMap<VehicleModelLabourRate, LabourRateByCountryDTO>().ReverseMap();
        CreateMap<MenuVariantLabourRate, LabourRateByCountryDTO>().ReverseMap();
        CreateMap<VehicleModel, VehicleModelListDTO>()
            .ForMember(x => x.BrandID, x => x.MapFrom(s => s.BrandID.HasValue ? s.BrandID.ToString()! : null));
        CreateMap<VehicleModel, VehicleModelDTO>()
            .ForMember(x => x.Brand, x => x.MapFrom(s => s.BrandID.HasValue ? new ShiftEntitySelectDTO { Value = s.BrandID.ToString()! } : null))
            .ForMember(
                x => x.ReplacementItems,
                x => x.MapFrom(src =>
                src.ReplacementItemVehicleModels != null ?
                    src.ReplacementItemVehicleModels.Select(s => new VehicleModelDTOReplacementItem(s.ReplacementItemID.ToString())
                    {
                        Name = s.ReplacementItem != null ? s.ReplacementItem.Name : string.Empty,
                        Type = s.ReplacementItem != null ? s.ReplacementItem.Type : default,
                        AllowMultiplePartNumbers = s.ReplacementItem != null ? s.ReplacementItem.AllowMultiplePartNumbers : false,
                        StandaloneAllowedTime = s.StandaloneAllowedTime,
                        DefaultPartPriceMarginPercentage = s.DefaultPartPriceMarginPercentage,
                        DefaultParts = (s.DefaultParts != null ? s.DefaultParts : new List<ReplacementItemVehicleModelPart>())
                            .Where(x => !x.IsDeleted)
                            .OrderBy(x => x.SortOrder)
                            .Select(x => new ReplacementItemDefaultPartDTO
                            {
                                PartNumber = x.PartNumber,
                                DefaultPeriodicQuantity = x.DefaultPeriodicQuantity,
                                DefaultStandaloneQuantity = x.DefaultStandaloneQuantity
                            }).ToList(),
                        StandaloneReplacementItemGroup = s.ReplacementItem != null && s.ReplacementItem.StandaloneReplacementItemGroup != null
                            ? new ShiftEntitySelectDTO
                            {
                                Value = s.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                Text = s.ReplacementItem.StandaloneReplacementItemGroup.Name
                            }
                            : null
                    }) :
                        new HashSet<VehicleModelDTOReplacementItem>()))
        .ReverseMap()
            .ForMember(x => x.BrandID, x => x.MapFrom(s => s.Brand != null ? s.Brand.Value : null))
            .ForMember(
                x=> x.LabourDetails,
                x=> x.Ignore()
            )
            .ForMember(
                x => x.LabourRates,
                x => x.Ignore()
            )
            .AfterMap((src, dest, ctx) =>
            {
                // Hnadle ReplacementItemVehicleModels
                dest.ReplacementItemVehicleModels ??= [];

                // 1. Remove items that are not in the source
                var itemsToRemove = dest.ReplacementItemVehicleModels
                    .Where(existing => !src.ReplacementItems.Any(r => r.ReplacementItemID == existing.ReplacementItemID.ToString()))
                    .ToList();
                foreach (var item in itemsToRemove)
                    dest.ReplacementItemVehicleModels.Remove(item);

                // 2. Update existing items or add new items
                foreach (var item in src.ReplacementItems)
                {
                    var existingItem = dest.ReplacementItemVehicleModels
                        .FirstOrDefault(r => r.ReplacementItemID.ToString() == item.ReplacementItemID);
                    if (existingItem != null)
                    {
                        existingItem.StandaloneAllowedTime = item.StandaloneAllowedTime ?? existingItem.StandaloneAllowedTime;
                        existingItem.DefaultPartPriceMarginPercentage = item.DefaultPartPriceMarginPercentage ?? existingItem.DefaultPartPriceMarginPercentage;
                        existingItem.DefaultParts ??= [];
                        existingItem.DefaultParts.Clear();
                        for (int i = 0; i < item.DefaultParts.Count; i++)
                        {
                            var sourcePart = item.DefaultParts[i];
                            existingItem.DefaultParts.Add(new ReplacementItemVehicleModelPart
                            {
                                SortOrder = i,
                                PartNumber = sourcePart.PartNumber,
                                DefaultPeriodicQuantity = sourcePart.DefaultPeriodicQuantity,
                                DefaultStandaloneQuantity = sourcePart.DefaultStandaloneQuantity
                            });
                        }
                    }
                    else
                        dest.ReplacementItemVehicleModels.Add(ctx.Mapper.Map<ReplacementItemVehicleModel>(item));
                }

                // Hnadle LabourDetails
                dest.LabourDetails ??= [];

                // 1. Remove items that are not in the source
                var laborDetailItemsToRemove = dest.LabourDetails
                    .Where(existing => !src.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();
                foreach (var item in laborDetailItemsToRemove)
                    dest.LabourDetails.Remove(item);

                // 2. Update existing items or add new items
                foreach (var item in src.LabourDetails)
                {
                    var existingItem = dest.LabourDetails
                        .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ServiceIntervalGroupID);
                    if (existingItem != null)
                        ctx.Mapper.Map(item, existingItem);
                    else
                        dest.LabourDetails.Add(ctx.Mapper.Map<VehicleModelLabourDetails>(item));
                }

                // Handle LabourRates
                dest.LabourRates ??= [];
                var labourRatesToRemove = dest.LabourRates
                    .Where(existing => !src.LabourRates.Any(r => r.CountryID == existing.CountryID))
                    .ToList();
                foreach (var item in labourRatesToRemove)
                    dest.LabourRates.Remove(item);

                foreach (var item in src.LabourRates)
                {
                    var existingItem = dest.LabourRates
                        .FirstOrDefault(r => r.CountryID == item.CountryID);
                    if (existingItem is not null)
                        existingItem.LabourRate = item.LabourRate.GetValueOrDefault();
                    else
                        dest.LabourRates.Add(ctx.Mapper.Map<VehicleModelLabourRate>(item));
                }
            });
        CreateMap<VehicleModelDTOReplacementItem, ReplacementItemVehicleModel>()
            .ForMember(x => x.DefaultParts, x => x.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.DefaultParts = src.DefaultParts
                    .Select((x, index) => new ReplacementItemVehicleModelPart
                    {
                        SortOrder = index,
                        PartNumber = x.PartNumber,
                        DefaultPeriodicQuantity = x.DefaultPeriodicQuantity,
                        DefaultStandaloneQuantity = x.DefaultStandaloneQuantity
                    }).ToList();
            });

        CreateMap<MenuEntity, MenuDTO>()
            .ForMember(
                x => x.VehicleModel,
                x => x.MapFrom(src => src.VehicleModelID != null ? new ShiftEntitySelectDTO { Value = src.VehicleModelID!.ToString()! } : null)
            )
            .ForMember(
                x => x.BrandID,
                x => x.MapFrom(src => src.BrandID.HasValue ? src.BrandID.ToString() : null)
            )
        .ReverseMap()
            .ForMember(
                x => x.VehicleModelID,
                x => x.MapFrom(src => src.VehicleModel != null ? src.VehicleModel.Value.ToLong() : (long?)null)
            )
            .ForMember(
                x => x.BrandID,
                x => x.Ignore()
            )
            .ForMember(
                x => x.Variants,
                x => x.Ignore()
            );

        CreateMap<MenuVariant, MenuVariantDTO>()
            .ForMember(
                x => x.MenuID,
                x => x.MapFrom(src => src.MenuID.ToString())
            )
            .ForMember(
                x => x.PeriodicAvailabilities,
                x => x.MapFrom(src => src.PeriodicAvailabilities.Select(s => new ServiceIntervalIDSelectorDTO { ID = s.ServiceIntervalID.ToString() }))
            )
        .ReverseMap()
            .ForMember(
                x => x.MenuID,
                x => x.MapFrom(src => src.MenuID.ToLong())
            )
            .ForMember(
                x => x.PeriodicAvailabilities,
                x => x.Ignore()
            )
            .ForMember(
                x => x.LabourDetails,
                x => x.Ignore()
            )
            .ForMember(
                x => x.Items,
                x => x.Ignore()
            )
            .ForMember(
                x => x.LabourRates,
                x => x.Ignore()
            )
            .AfterMap((src, dest, ctx) =>
            {
                dest.LabourDetails ??= [];
                var itemsToRemove = dest.LabourDetails
                    .Where(existing => !src.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();
                foreach (var item in itemsToRemove)
                    dest.LabourDetails.Remove(item);

                foreach (var item in src.LabourDetails)
                {
                    var existingItem = dest.LabourDetails
                        .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ServiceIntervalGroupID);
                    if (existingItem != null)
                        ctx.Mapper.Map(item, existingItem);
                    else
                        dest.LabourDetails.Add(ctx.Mapper.Map<MenuLabourDetails>(item));
                }

                dest.PeriodicAvailabilities ??= [];
                var serviceIntervalItemsToRemove = dest.PeriodicAvailabilities
                    .Where(existing => !src.PeriodicAvailabilities.Any(r => r.ID == existing.ServiceIntervalID.ToString()))
                    .ToList();
                foreach (var item in serviceIntervalItemsToRemove)
                    dest.PeriodicAvailabilities.Remove(item);

                foreach (var item in src.PeriodicAvailabilities)
                {
                    var existingItem = dest.PeriodicAvailabilities
                        .FirstOrDefault(r => r.ServiceIntervalID.ToString() == item.ID);
                    if (existingItem == null)
                        dest.PeriodicAvailabilities.Add(new MenuPeriodicAvailability { ServiceIntervalID = item.ID.ToLong() });
                }

                dest.Items ??= [];
                var itemsToRemoveFromMenu = dest.Items
                    .Where(existing => !src.Items.Any(r => r.ReplacementItemVehicleModelID == existing.ReplacementItemVehicleModelID))
                    .ToList();
                foreach (var item in itemsToRemoveFromMenu)
                    dest.Items.Remove(item);

                foreach (var item in src.Items)
                {
                    var existingItem = dest.Items
                        .FirstOrDefault(r => r.ReplacementItemVehicleModelID == item.ReplacementItemVehicleModelID);
                    if (existingItem != null)
                        ctx.Mapper.Map(item, existingItem);
                    else
                        dest.Items.Add(ctx.Mapper.Map<MenuItem>(item));
                }

                dest.LabourRates ??= [];
                var labourRatesToRemove = dest.LabourRates
                    .Where(existing => !src.LabourRates.Any(r => r.CountryID == existing.CountryID))
                    .ToList();
                foreach (var item in labourRatesToRemove)
                    dest.LabourRates.Remove(item);

                foreach (var item in src.LabourRates)
                {
                    var existingItem = dest.LabourRates
                        .FirstOrDefault(r => r.CountryID == item.CountryID);
                    if (existingItem is not null)
                        existingItem.LabourRate = item.LabourRate.GetValueOrDefault();
                    else
                        dest.LabourRates.Add(ctx.Mapper.Map<MenuVariantLabourRate>(item));
                }
            });
        CreateMap<MenuItem, MenuItemDTO>()
            .ForMember(
                x => x.ReplacementItem,
                x => x.MapFrom(s => s.ReplacementItemVehicleModel == null || s.ReplacementItemVehicleModel.ReplacementItem == null
                    ? null
                    : new MenuItemReplacementItemDTO
                    {
                        ID = s.ReplacementItemVehicleModel.ReplacementItem.ID.ToString(),
                        Name = s.ReplacementItemVehicleModel.ReplacementItem.Name,
                        Type = s.ReplacementItemVehicleModel.ReplacementItem.Type,
                        AllowMultiplePartNumbers = s.ReplacementItemVehicleModel.ReplacementItem.AllowMultiplePartNumbers,
                        StandaloneAllowedTime = s.ReplacementItemVehicleModel.StandaloneAllowedTime,
                        DefaultPartPriceMarginPercentage = s.ReplacementItemVehicleModel.DefaultPartPriceMarginPercentage,
                        StandaloneReplacementItemGroup = s.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup == null
                            ? null
                            : new ShiftEntitySelectDTO
                            {
                                Value = s.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                Text = s.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.Name
                            }
                    })
            )
            .ForMember(
                x => x.Parts,
                x => x.MapFrom(s => s.Parts.OrderBy(p => p.SortOrder))
            )
            .ReverseMap()
            .ForMember(x => x.Parts, x => x.Ignore())
            .AfterMap((src, dest, ctx) =>
            {
                dest.Parts ??= [];
                dest.Parts.Clear();

                for (int i = 0; i < src.Parts.Count; i++)
                {
                    var sourcePart = src.Parts[i];
                    var sourceCountryPrices = sourcePart.CountryPrices ?? [];
                    dest.Parts.Add(new MenuItemPart
                    {
                        SortOrder = i,
                        PartNumber = sourcePart.PartNumber,
                        PeriodicQuantity = sourcePart.PeriodicQuantity,
                        StandaloneQuantity = sourcePart.StandaloneQuantity,
                        CountryPrices = sourceCountryPrices
                            .Select(cp => new MenuItemPartCountryPrice
                            {
                                CountryID = cp.CountryID.GetValueOrDefault(),
                                PartPrice = cp.PartPrice,
                                PartPriceMarginPercentage = cp.PartPriceMarginPercentage,
                                PartFinalPrice = cp.PartFinalPrice.GetValueOrDefault()
                            })
                            .ToList()
                    });
                }
            });
        CreateMap<MenuEntity, MenuListDTO>()
            .ForMember(
                x => x.VehilceModel,
                x => x.MapFrom(src => src.VehicleModel != null ? src.VehicleModel.Name : string.Empty)
            )
            .ForMember(
                x => x.BrandID,
                x => x.MapFrom(src => src.BrandID)
            )
            .ForMember(
                x => x.VariantsCount,
                x => x.MapFrom(src => src.Variants.Count(x => !x.IsDeleted))
            );

        CreateMap<MenuVariant, MenuVariantListDTO>();

        CreateMap<ServiceInterval, ServiceIntervalDTO>()
            .ForMember(
                x => x.ServiceIntervalGroup,
                x => x.MapFrom(src => new ShiftEntitySelectDTO { Value = src.ServiceIntervalGroupID.ToString() })
            )
            .ReverseMap()
            .ForMember(
                x => x.ServiceIntervalGroupID,
                x => x.MapFrom(src => src.ServiceIntervalGroup.Value.ToLong())
            );
        CreateMap<ServiceInterval, ServiceIntervalListDTO>()
            .ForMember(
                x => x.ServiceIntervalGroupName,
                x => x.MapFrom(src => src.ServiceIntervalGroup.Name)
            );

        CreateMap<ServiceIntervalGroup, ServiceIntervalGroupDTO>().ReverseMap();
        CreateMap<ServiceIntervalGroup, ServiceIntervalGroupListDTO>();

        CreateMap<LabourRateMapping, LabourRateMappingDTO>()
            .ForMember(x => x.Brand, x => x.MapFrom(s => s.BrandID.HasValue ? new ShiftEntitySelectDTO { Value = s.BrandID.ToString()! } : null))
        .ReverseMap()
            .ForMember(x => x.BrandID, x => x.MapFrom(s => s.Brand != null ? s.Brand.Value : null));

        CreateMap<BrandMapping, BrandMappingDTO>()
            .ForMember(x => x.Brand, x => x.MapFrom(s => s.BrandID.HasValue ? new ShiftEntitySelectDTO { Value = s.BrandID.ToString()! } : null))
        .ReverseMap()
            .ForMember(x => x.BrandID, x => x.MapFrom(s => s.Brand != null ? s.Brand.Value : null));

        CreateMap<BrandMapping, BrandMappingListDTO>()
            .ForMember(x => x.BrandID, x => x.MapFrom(s => s.BrandID.HasValue ? s.BrandID.ToString()! : null));

        CreateMap<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupListDTO>();
        CreateMap<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupDTO>().ReverseMap();

        CreateMap<MenuVersion, MenuVersionListDTO>()
            .ForMember(
                x => x.Text,
                x => x.MapFrom(src => src.Version.ToString() + " - " + src.VersionDateTime.ToString())
            );
    }
}


