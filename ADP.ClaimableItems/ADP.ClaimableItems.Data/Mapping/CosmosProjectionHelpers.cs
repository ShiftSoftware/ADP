using System.Text.Json;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Mapping;

/// <summary>
/// The two projection helpers that outlived the deleted AutoMapper profiles.
///
/// <para>
/// <b>Both are still load-bearing.</b> The entity/DTO maps went away with the profiles because the
/// generated mappers replace them, but the <b>Cosmos replication projections did not</b> - they are
/// hand-written delegates now (<c>ClaimableItemsReplicationExtensions</c>), and they call these two
/// methods exactly as the profiles did. Deleting the profile folder wholesale would have taken both
/// with it and broken those delegates.
/// </para>
///
/// <para>
/// <c>DeserializeModelCosts</c> is the one worth flagging: it was a nested
/// <c>ClaimableItemProfile.MappingHelpers</c> class, so it read as part of the profile rather than as
/// a shared helper, and it is easy to lose by deleting the file that happened to contain it.
/// </para>
///
/// <para>
/// Both are carried over verbatim - same serializer options (i.e. <b>default</b> options, not the
/// framework's configured ones), same null handling, same early returns. Changing either changes the
/// JSON written to live Cosmos documents.
/// </para>
/// </summary>
public static class CosmosProjectionHelpers
{
    /// <summary>
    /// Localized-name column to dictionary. Null input yields null - not an empty dictionary.
    /// Carried over from <c>GeneralMappingHelper.DeserializeDict</c> unchanged.
    /// </summary>
    public static Dictionary<string, string>? DeserializeDict(string? json) =>
        json == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);

    /// <summary>
    /// Per-model cost rows for the Cosmos document, decoded from the entity's <c>Costs</c> JSON
    /// column.
    ///
    /// <para>
    /// Two early returns are behaviour, not defensiveness: a <b>Fixed</b>-costing item yields
    /// <c>null</c> rather than an empty sequence (its cost lives in <c>FixedCost</c> instead), and so
    /// does an empty column or a payload that deserializes to null. The <c>ServiceItemID</c> stamped
    /// onto every row is the owning item's id, which is why the id has to be passed in.
    /// </para>
    /// </summary>
    public static IEnumerable<ShiftSoftware.ADP.Models.Vehicle.ServiceItemCostModel>? DeserializeModelCosts(
        string? json,
        ClaimableItemCostingType costingType,
        long serviceItemId)
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
