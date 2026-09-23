using ShiftMapper;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Data.Mappers;

/// <summary>
/// The EF entity → Cosmos document maps for the menu catalog — one per replicated table, plus the children a
/// document owns. These are LAYER 2 only — persistence. They never generate menu codes; that is
/// <c>MenuCodeGenerator</c>'s job at read time, from documents these produce.
///
/// <para>A master entity's map serves double duty: it produces the document for the entity's own container AND
/// the copy embedded into the menu documents, so a master row has exactly one projection and the two places it
/// lands cannot drift. The menu documents' maps write only the document's own fields; every embedded master copy
/// is put in by <c>MenuCosmosDocuments</c> (ADP.Menus.Sync), from the master's map, the same way its fan-out
/// refreshes it.</para>
///
/// Rules that hold throughout:
///  • <c>id</c> is the source row's database id — filled from <c>ID</c> by the case-insensitive name match — so a
///    soft-deleted-and-recreated row cannot collide and the replication stamp has a stable key.
///  • <c>IsDeleted</c> is carried, because soft deleting a row upserts its document rather than removing it — only
///    a hard delete removes one. Readers filter on it, embedded copies included.
///  • Collections are carried UNFILTERED, and soft-deleted rows are REPLICATED rather than skipped. These are pure
///    projections, exactly like the export's <c>EfToGenerationAggregator</c>: they copy, they do not decide. Whether
///    a soft-deleted row contributes to a menu is decided once, by <c>MenuCodeGenerator</c>, so the DMS export and
///    the vehicle lookup cannot disagree. The one exception is a replacement item's interval-group LINKS — see
///    <c>MenuCosmosDocuments</c> for why.
///  • Navigation reads are ternaries (an expression tree has no <c>?.</c>): a document whose parent chain was not
///    loaded gets a null partition value, exactly as before, rather than throwing.
///
/// <para>Every navigation these maps touch must be loaded by the matching reload in <c>MenuReplicationReload</c>
/// (ADP.Menus.Sync); a missing include silently produces a document with holes.</para>
///
/// <para>Declared here rather than beside the replication so they are part of this assembly's generated mapper —
/// the one <c>AddMenuApiServices</c> already registers — and a host writes no mapping line to replicate. Keep the
/// constructor parameterless: the replication also builds this assembly's mapper outside a container, for a host
/// that registered none, and a dependency would make that impossible.</para>
/// </summary>
public class MenuReplicationMapper : ShiftMapperBase
{
    public MenuReplicationMapper()
    {
        // ---- master entities -------------------------------------------------------------------------------

        CreateMap<ServiceInterval, ServiceIntervalCosmosModel>()
            .ForMember(d => d.ServiceIntervalID, opt => opt.MapFrom(e => e.ID));

        // Membership is what lets generation match a periodic interval to this group. Copied whole, soft-deleted
        // intervals included, because that is what the export's unfiltered include yields — and an interval missing
        // from here silently drops the periodic line rather than erroring.
        CreateMap<ServiceIntervalGroup, ServiceIntervalGroupCosmosModel>()
            .ForMember(d => d.ServiceIntervalGroupID, opt => opt.MapFrom(e => e.ID))
            .ForMember(d => d.ServiceIntervalIDs, opt => opt.MapFrom(e => e.ServiceIntervals == null
                ? new List<long>()
                : e.ServiceIntervals.Select(interval => interval.ID).ToList()));

        // The LIVE interval-group links only — the same rule MenuCosmosDocuments applies to the groups it embeds.
        CreateMap<ReplacementItem, ReplacementItemCosmosModel>()
            .ForMember(d => d.ReplacementItemID, opt => opt.MapFrom(e => e.ID))
            .ForMember(d => d.ServiceIntervalGroupIDs, opt => opt.MapFrom(e => e.ReplacementItemServiceIntervalGroups == null
                ? new List<long>()
                : e.ReplacementItemServiceIntervalGroups
                    .Where(link => !link.IsDeleted)
                    .Select(link => link.ServiceIntervalGroupID)
                    .ToList()));

        CreateMap<StandaloneReplacementItemGroup, StandaloneReplacementItemGroupCosmosModel>()
            .ForMember(d => d.StandaloneReplacementItemGroupID, opt => opt.MapFrom(e => e.ID));

        CreateMap<LabourRateMapping, LabourRateMappingCosmosModel>();
        CreateMap<BrandMapping, BrandMappingCosmosModel>();

        // ---- the ServiceMenus container --------------------------------------------------------------------

        // The labour-rate and brand mappings have no navigation from a variant: they are resolved by the
        // replication and embedded by MenuCosmosDocuments.Variant.
        CreateMap<MenuVariant, MenuVariantCosmosModel>()
            .ForMember(d => d.VariantID, opt => opt.MapFrom(e => e.ID))
            .ForMember(d => d.VariantName, opt => opt.MapFrom(e => e.Name))
            .ForMember(d => d.BasicModelCode, opt => opt.MapFrom(e => e.Menu == null ? null! : e.Menu.BasicModelCode))
            .ForMember(d => d.BrandID, opt => opt.MapFrom(e => e.Menu == null || e.Menu.VehicleModel == null ? null : e.Menu.VehicleModel.BrandID))
            .ForMember(d => d.Model, opt => opt.MapFrom(e => e.Menu == null || e.Menu.VehicleModel == null ? null! : e.Menu.VehicleModel.Name))

            // The export selects on BOTH flags, and deleting a menu does not cascade to its variants — so without
            // this copy a deleted menu keeps serving menu codes from the lookup. See the field's remarks on
            // MenuVariantCosmosModel.
            .ForMember(d => d.MenuIsDeleted, opt => opt.MapFrom(e => e.Menu != null && e.Menu.IsDeleted))

            // Owned by the variant and never queried alone. Carried with their delete flags so the generator, not
            // this projection, decides which country rate applies.
            .ForMember(d => d.CountryLabourRates, opt => opt.MapFrom(e => e.LabourRates
                .Select(rate => new MenuCountryLabourRateCosmosModel
                {
                    CountryID = rate.CountryID,
                    LabourRate = rate.LabourRate,
                    IsDeleted = rate.IsDeleted,
                })
                .ToList()))
            .ForMember(d => d.LabourRateMapping, opt => opt.Ignore())
            .ForMember(d => d.BrandMapping, opt => opt.Ignore());

        // The embedded interval (group) is the master's own map, written by MenuCosmosDocuments exactly as the
        // master's fan-out refreshes it — which also keeps a document whose master was not loaded writing a null
        // copy rather than failing: the navigation is declared non-nullable, so a nested map would trust it.
        CreateMap<MenuPeriodicAvailability, MenuPeriodCosmosModel>()
            .ForMember(d => d.BasicModelCode, opt => opt.MapFrom(e => e.MenuVariant == null || e.MenuVariant.Menu == null ? null! : e.MenuVariant.Menu.BasicModelCode))
            .ForMember(d => d.VariantID, opt => opt.MapFrom(e => e.MenuVariantID))
            .ForMember(d => d.ServiceInterval, opt => opt.Ignore());

        CreateMap<MenuLabourDetails, MenuLabourCosmosModel>()
            .ForMember(d => d.BasicModelCode, opt => opt.MapFrom(e => e.MenuVariant == null || e.MenuVariant.Menu == null ? null! : e.MenuVariant.Menu.BasicModelCode))
            .ForMember(d => d.VariantID, opt => opt.MapFrom(e => e.MenuVariantID))
            .ForMember(d => d.ServiceIntervalGroup, opt => opt.Ignore());

        // Parts and their prices are embedded by their own maps below: they are owned by the item and never read
        // alone. The replacement-item slice — the item, its standalone group and the interval groups it serves —
        // is reached through the RIVM link, not a member of the item, so MenuCosmosDocuments.Item writes it from
        // the masters' own maps, exactly as the replacement item's fan-out refreshes it.
        CreateMap<MenuItem, MenuItemCosmosModel>()
            .ForMember(d => d.BasicModelCode, opt => opt.MapFrom(e => e.MenuVariant == null || e.MenuVariant.Menu == null ? null! : e.MenuVariant.Menu.BasicModelCode))
            .ForMember(d => d.MenuItemID, opt => opt.MapFrom(e => e.ID))
            .ForMember(d => d.VariantID, opt => opt.MapFrom(e => e.MenuVariantID))

            // The generator's own guards, resolved here so it never has to reason about EF nulls. These are about
            // the LINK row, not the replacement item's own delete flag.
            .ForMember(d => d.HasReplacementItem, opt => opt.MapFrom(e => e.ReplacementItemVehicleModel != null))
            .ForMember(d => d.ReplacementItemDeleted, opt => opt.MapFrom(e => e.ReplacementItemVehicleModel != null && e.ReplacementItemVehicleModel.IsDeleted))

            .ForMember(d => d.ReplacementItem, opt => opt.Ignore())
            .ForMember(d => d.ServiceIntervalGroups, opt => opt.Ignore())
            .ForMember(d => d.StandaloneGroup, opt => opt.Ignore());

        CreateMap<MenuItemPart, MenuItemPartCosmosModel>();
        CreateMap<MenuItemPartCountryPrice, MenuPartCountryPriceCosmosModel>();
    }

    /// <summary>A document is written member by member above; nothing is reached through a navigation by name.</summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;
}
