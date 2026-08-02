using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>
/// The service menu for one basic model code — LAYER 3 of ADP.Menus/COSMOS_REPLICATION_PLAN.md §1.1,
/// the vehicle lookup's own shape.
///
/// The menu codes and labour codes here are produced by the SAME generator the DMS export uses, over
/// data replicated per row into Cosmos, so a code served here is the code the dealer's DMS received.
///
/// <para><b>Dealer cost is absent by construction, not by omission.</b> Cost only ever enters a
/// generated line when the caller sets <c>MenuGenerationConfig.IncludePartCost</c>, and the lookup
/// never does — so it is not in the object graph at all, and no mapper here has to remember to strip
/// it. Do not add a cost, margin or profit field to these types; those belong to the DMS export
/// (<c>MenuLineMargins</c>) and must not reach a public web component.</para>
/// </summary>
[Docable]
public class ServiceMenuLookupDTO
{
    /// <summary>The basic model code the menu was looked up by.</summary>
    public string BasicModelCode { get; set; }

    /// <summary>The country the part prices and labour rate were resolved for.</summary>
    public long CountryID { get; set; }

    /// <summary>The language the codes and descriptions were generated in.</summary>
    public string Language { get; set; }

    /// <summary>The transfer rate the consumable was scaled by. 1 means unscaled.</summary>
    public decimal TransferRate { get; set; }

    /// <summary>
    /// The menu variants authored for this model. Usually one; several when a model has variant-specific
    /// menus. Ordered by variant id.
    /// </summary>
    public List<ServiceMenuVariantDTO> Variants { get; set; } = new List<ServiceMenuVariantDTO>();

    /// <summary>
    /// True when no menu documents exist for this model code at all — as opposed to a menu that exists
    /// but generates no lines (every variant deleted, or no interval whose group carries a labour
    /// detail). Lets a UI distinguish "this model has no menu" from "nothing is due".
    /// </summary>
    public bool NotFound { get; set; }
}
