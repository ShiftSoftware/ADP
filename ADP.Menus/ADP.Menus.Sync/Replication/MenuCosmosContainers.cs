using NoSQLConstants = ShiftSoftware.ADP.Models.Constants.NoSQLConstants;

namespace ShiftSoftware.ADP.Menus.Sync.Replication;

/// <summary>One Cosmos container the menu replication writes to, and the key it must be created with.</summary>
/// <param name="Name">The container id.</param>
/// <param name="PartitionKeyPaths">
/// One path for a single-level key, several for a hierarchical one. Order is significant.
/// </param>
public sealed record MenuCosmosContainer(string Name, IReadOnlyList<string> PartitionKeyPaths);

/// <summary>
/// Every container <c>AddMenuReplications</c> writes to, with the partition key each must be created
/// with — the deployment-side companion to the registration itself.
///
/// A partition key CANNOT be changed after a container is created: getting one wrong means dropping and
/// recreating the container once real data exists. So the list lives in one place that hosts, the
/// sample and the provisioning test all read, rather than being retyped wherever a container gets
/// created.
///
/// This declares WHAT to create, not WHEN. Hosts provision through their own deployment path; the
/// sample does it at startup for convenience, and <c>ServiceMenusProvisioningTests</c> does it against
/// a local emulator and asserts the result.
/// </summary>
public static class MenuCosmosContainers
{
    /// <summary>The database all of them live in.</summary>
    public const string DatabaseName = NoSQLConstants.Databases.Services;

    /// <summary>
    /// The menu variant graph, then one container per master entity.
    ///
    /// <c>ServiceMenus</c> is hierarchical — the basic model code, then the item type — so one query by
    /// model code returns a whole model's menu graph from a single partition, with the four document
    /// types coexisting under it. Each master container holds one document type and is keyed by its own
    /// id, matching how the Services database already provisions ServiceItems and
    /// ClaimableItemCampaigns. See COSMOS_REPLICATION_PLAN.md §16.
    /// </summary>
    public static IReadOnlyList<MenuCosmosContainer> All { get; } =
    [
        new(NoSQLConstants.Containers.ServiceMenus,
        [
            NoSQLConstants.PartitionKeys.ServiceMenus.Level1,
            NoSQLConstants.PartitionKeys.ServiceMenus.Level2,
        ]),
        new(NoSQLConstants.Containers.ServiceIntervals,
            [NoSQLConstants.PartitionKeys.ServiceIntervals.Level1]),
        new(NoSQLConstants.Containers.ServiceIntervalGroups,
            [NoSQLConstants.PartitionKeys.ServiceIntervalGroups.Level1]),
        new(NoSQLConstants.Containers.ReplacementItems,
            [NoSQLConstants.PartitionKeys.ReplacementItems.Level1]),
        new(NoSQLConstants.Containers.StandaloneReplacementItemGroups,
            [NoSQLConstants.PartitionKeys.StandaloneReplacementItemGroups.Level1]),
        new(NoSQLConstants.Containers.LabourRateMappings,
            [NoSQLConstants.PartitionKeys.LabourRateMappings.Level1]),
        new(NoSQLConstants.Containers.BrandMappings,
            [NoSQLConstants.PartitionKeys.BrandMappings.Level1]),
    ];
}
