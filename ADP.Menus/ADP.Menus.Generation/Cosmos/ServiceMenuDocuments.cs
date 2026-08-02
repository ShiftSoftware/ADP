using System.Collections.Generic;

using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// One basic model code's worth of <c>ServiceMenus</c> documents — the raw result of the single
/// partition query the lookup performs, split by item type.
///
/// It exists so the read path has ONE shape to pass around: the Cosmos reader fills it,
/// <see cref="CosmosToGenerationAggregator"/> consumes it, and a test can build one by hand without a
/// Cosmos account. Deliberately a dumb bag — it carries documents exactly as they were stored,
/// soft-delete flags and all. Every inclusion rule belongs to the aggregator and the generator.
/// </summary>
public class ServiceMenuDocuments
{
    /// <summary>The partition these documents came from. Informational; the aggregator does not read it.</summary>
    public string BasicModelCode { get; set; }

    public List<MenuVariantCosmosModel> Variants { get; set; } = [];

    public List<MenuPeriodCosmosModel> Periods { get; set; } = [];

    public List<MenuLabourCosmosModel> Labours { get; set; } = [];

    public List<MenuItemCosmosModel> Items { get; set; } = [];

    /// <summary>Total documents read, across all four types.</summary>
    public int DocumentCount =>
        (Variants?.Count ?? 0) + (Periods?.Count ?? 0) + (Labours?.Count ?? 0) + (Items?.Count ?? 0);

    /// <summary>
    /// True when the partition returned nothing at all — the model has no replicated menu (or the
    /// basic model code does not match any authored menu).
    ///
    /// NOT the same as "generates no lines": a partition can be non-empty and still produce nothing,
    /// e.g. every variant soft-deleted, or intervals whose groups carry no labour detail.
    /// </summary>
    public bool IsEmpty => DocumentCount == 0;
}
