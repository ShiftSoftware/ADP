using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

using ShiftSoftware.ADP.Menus.Sync.Replication;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Constants;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Renders each <see cref="MenuReplicationFinders"/> query to the Cosmos SQL it will actually run.
///
/// This is the guard for the one part of the replication design that cannot fail loudly. The finders
/// decide which denormalized documents a master-row edit reaches; they run inside fire-and-forget
/// replication, so a predicate that stops translating — or that names a property the document no longer
/// has — produces no error anywhere. The query just matches nothing, master edits quietly stop
/// propagating, and the lookup serves stale menu codes with no signal at all. Rendering the SQL is what
/// turns that into a test failure.
///
/// OFFLINE. Building a LINQ query needs a <see cref="Container"/> handle but never touches the network,
/// so unlike <see cref="ServiceMenusProvisioningTests"/> this one needs no emulator and always runs.
///
/// Assertions stay on the parts that carry meaning — the item-type discriminator, the property path,
/// the operator — rather than the exact rendered string, which belongs to the SDK.
/// </summary>
public class MenuReplicationFinderTests
{
    /// <summary>
    /// The emulator's well-known endpoint, used only to construct a client. No request is ever made:
    /// CosmosClient construction is lazy and query translation is entirely client-side.
    /// </summary>
    private const string EmulatorConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private static IQueryable<TDocument> ServiceMenus<TDocument>()
    {
        var client = new CosmosClient(EmulatorConnectionString);

        return client
            .GetContainer(NoSQLConstants.Databases.Services, NoSQLConstants.Containers.ServiceMenus)
            .GetItemLinqQueryable<TDocument>();
    }

    private const long ServiceIntervalId = 501;
    private const long ServiceIntervalGroupId = 77;
    private const long ReplacementItemId = 3311;
    private const long StandaloneGroupId = 44;
    private const long BrandId = 7;

    [Fact]
    public void PeriodsEmbeddingInterval_FiltersByItemTypeAndIntervalId()
    {
        var sql = MenuReplicationFinders
            .PeriodsEmbeddingInterval(ServiceMenus<MenuPeriodCosmosModel>(), ServiceIntervalId)
            .ToQueryDefinition().QueryText;

        Assert.Contains(nameof(MenuPeriodCosmosModel.ItemType), sql);
        Assert.Contains((string)ModelTypes.MenuPeriod, sql);
        Assert.Contains(nameof(MenuPeriodCosmosModel.ServiceIntervalID), sql);
        Assert.Contains(ServiceIntervalId.ToString(), sql);
    }

    [Fact]
    public void LabourDetailsEmbeddingIntervalGroup_FiltersByItemTypeAndGroupId()
    {
        var sql = MenuReplicationFinders
            .LabourDetailsEmbeddingIntervalGroup(ServiceMenus<MenuLabourCosmosModel>(), ServiceIntervalGroupId)
            .ToQueryDefinition().QueryText;

        Assert.Contains(nameof(MenuLabourCosmosModel.ItemType), sql);
        Assert.Contains((string)ModelTypes.MenuLabour, sql);
        Assert.Contains(nameof(MenuLabourCosmosModel.ServiceIntervalGroupID), sql);
        Assert.Contains(ServiceIntervalGroupId.ToString(), sql);
    }

    /// <summary>
    /// The membership test has to reach INTO the embedded replacement item's flat id list, and it has
    /// to become an ARRAY_CONTAINS rather than being evaluated client-side — a scan of every menu item
    /// in the catalogue would be a very quiet way to make this expensive.
    /// </summary>
    [Fact]
    public void MenuItemsServingIntervalGroup_TranslatesToArrayContainsOnTheEmbeddedIdList()
    {
        var sql = MenuReplicationFinders
            .MenuItemsServingIntervalGroup(ServiceMenus<MenuItemCosmosModel>(), ServiceIntervalGroupId)
            .ToQueryDefinition().QueryText;

        Assert.Contains("ARRAY_CONTAINS", sql);
        Assert.Contains((string)ModelTypes.MenuItem, sql);
        Assert.Contains(nameof(MenuItemCosmosModel.ReplacementItem), sql);
        Assert.Contains(nameof(ReplacementItemCosmosModel.ServiceIntervalGroupIDs), sql);
        Assert.Contains(ServiceIntervalGroupId.ToString(), sql);
    }

    [Fact]
    public void MenuItemsEmbeddingReplacementItem_ReachesIntoTheEmbeddedItem()
    {
        var sql = MenuReplicationFinders
            .MenuItemsEmbeddingReplacementItem(ServiceMenus<MenuItemCosmosModel>(), ReplacementItemId)
            .ToQueryDefinition().QueryText;

        Assert.Contains((string)ModelTypes.MenuItem, sql);
        Assert.Contains(nameof(MenuItemCosmosModel.ReplacementItem), sql);
        Assert.Contains(nameof(ReplacementItemCosmosModel.ReplacementItemID), sql);
        Assert.Contains(ReplacementItemId.ToString(), sql);
    }

    [Fact]
    public void MenuItemsEmbeddingStandaloneGroup_ReachesIntoTheEmbeddedGroup()
    {
        var sql = MenuReplicationFinders
            .MenuItemsEmbeddingStandaloneGroup(ServiceMenus<MenuItemCosmosModel>(), StandaloneGroupId)
            .ToQueryDefinition().QueryText;

        Assert.Contains((string)ModelTypes.MenuItem, sql);
        Assert.Contains(nameof(MenuItemCosmosModel.StandaloneGroup), sql);
        Assert.Contains(nameof(StandaloneReplacementItemGroupCosmosModel.StandaloneReplacementItemGroupID), sql);
        Assert.Contains(StandaloneGroupId.ToString(), sql);
    }

    /// <summary>
    /// Both key components must reach the query. Matching on the brand alone would hand every variant
    /// of that brand a mapping meant for one labour rate.
    /// </summary>
    [Fact]
    public void VariantsEmbeddingLabourRateMapping_FiltersByBothKeyComponents()
    {
        var sql = MenuReplicationFinders
            .VariantsEmbeddingLabourRateMapping(ServiceMenus<MenuVariantCosmosModel>(), BrandId, 12.50m)
            .ToQueryDefinition().QueryText;

        Assert.Contains((string)ModelTypes.MenuVariant, sql);
        Assert.Contains(nameof(MenuVariantCosmosModel.BrandID), sql);
        Assert.Contains(nameof(MenuVariantCosmosModel.LabourRate), sql);
        Assert.Contains(BrandId.ToString(), sql);

        // Trailing zeros do not survive the trip to JSON, on either side of the comparison — which is
        // what keeps the mapping's decimal-VALUE semantics (12.50 and 12.5 are one key) intact here.
        Assert.Contains("12.5", sql);
    }

    [Fact]
    public void VariantsEmbeddingBrandMapping_FiltersByItemTypeAndBrand()
    {
        var sql = MenuReplicationFinders
            .VariantsEmbeddingBrandMapping(ServiceMenus<MenuVariantCosmosModel>(), BrandId)
            .ToQueryDefinition().QueryText;

        Assert.Contains((string)ModelTypes.MenuVariant, sql);
        Assert.Contains(nameof(MenuVariantCosmosModel.BrandID), sql);
        Assert.Contains(BrandId.ToString(), sql);
    }
}
