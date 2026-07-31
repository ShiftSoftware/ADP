using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Data.Replication;

/// <summary>
/// "Which menu documents embed this master row?" — the queries that drive the <c>UpdateReference</c>
/// fan-outs in <c>MenuReplicationExtensions</c>.
///
/// They are named and gathered here rather than written inline for one reason: they run against Cosmos
/// as translated SQL, inside fire-and-forget replication. A predicate that stops translating, or that
/// names a property the document no longer has, does not fail a build or surface an error — the query
/// simply matches nothing, and master edits quietly stop reaching the documents that denormalize them.
/// Being ordinary methods, they can be rendered to SQL offline and pinned by a test.
///
/// Two rules hold throughout:
///
///  • Narrow on <c>ItemType</c> FIRST. The ServiceMenus container holds four document shapes in one
///    partition, so without it a query is asked to deserialize all of them.
///
///  • The discriminator goes through a local string. What reaches the expression tree is then a plain
///    string comparison rather than an implicit conversion the LINQ provider would have to fold.
/// </summary>
public static class MenuReplicationFinders
{
    /// <summary>Periodic-availability documents that embed a given service interval.</summary>
    public static IQueryable<MenuPeriodCosmosModel> PeriodsEmbeddingInterval(
        IQueryable<MenuPeriodCosmosModel> query, long serviceIntervalId)
    {
        string itemType = ModelTypes.MenuPeriod;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.ServiceIntervalID == serviceIntervalId);
    }

    /// <summary>Labour-detail documents keyed to a given service-interval group.</summary>
    public static IQueryable<MenuLabourCosmosModel> LabourDetailsEmbeddingIntervalGroup(
        IQueryable<MenuLabourCosmosModel> query, long serviceIntervalGroupId)
    {
        string itemType = ModelTypes.MenuLabour;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.ServiceIntervalGroupID == serviceIntervalGroupId);
    }

    /// <summary>
    /// Menu items whose replacement item serves a given interval group. The flat id list on the
    /// embedded replacement item is what makes this an ARRAY_CONTAINS; the group's denormalized detail
    /// hangs off <see cref="MenuItemCosmosModel.ServiceIntervalGroups"/>, which is not queryable that way.
    /// </summary>
    public static IQueryable<MenuItemCosmosModel> MenuItemsServingIntervalGroup(
        IQueryable<MenuItemCosmosModel> query, long serviceIntervalGroupId)
    {
        string itemType = ModelTypes.MenuItem;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.ReplacementItem.ServiceIntervalGroupIDs.Contains(serviceIntervalGroupId));
    }

    /// <summary>Menu items that embed a given replacement item.</summary>
    public static IQueryable<MenuItemCosmosModel> MenuItemsEmbeddingReplacementItem(
        IQueryable<MenuItemCosmosModel> query, long replacementItemId)
    {
        string itemType = ModelTypes.MenuItem;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.ReplacementItem.ReplacementItemID == replacementItemId);
    }

    /// <summary>Menu items that fold into a given standalone replacement-item group.</summary>
    public static IQueryable<MenuItemCosmosModel> MenuItemsEmbeddingStandaloneGroup(
        IQueryable<MenuItemCosmosModel> query, long standaloneReplacementItemGroupId)
    {
        string itemType = ModelTypes.MenuItem;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.StandaloneGroup.StandaloneReplacementItemGroupID == standaloneReplacementItemGroupId);
    }

    /// <summary>
    /// Variants whose (brand, primary labour rate) pair is the one a labour-rate mapping is keyed by —
    /// both components, so a variant on a different rate keeps its own mapping.
    ///
    /// A NULL brand matches nothing: Cosmos SQL evaluates <c>c.BrandID = null</c> to undefined, not
    /// true. That is acceptable rather than papered over — a variant with no brand is excluded from the
    /// export by construction, so it generates no menu lines for the lookup to have to match.
    /// </summary>
    public static IQueryable<MenuVariantCosmosModel> VariantsEmbeddingLabourRateMapping(
        IQueryable<MenuVariantCosmosModel> query, long? brandId, decimal labourRate)
    {
        string itemType = ModelTypes.MenuVariant;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.BrandID == brandId &&
            document.LabourRate == labourRate);
    }

    /// <summary>Variants of a given brand. Same null-brand caveat as above.</summary>
    public static IQueryable<MenuVariantCosmosModel> VariantsEmbeddingBrandMapping(
        IQueryable<MenuVariantCosmosModel> query, long? brandId)
    {
        string itemType = ModelTypes.MenuVariant;

        return query.Where(document =>
            document.ItemType == itemType &&
            document.BrandID == brandId);
    }
}
