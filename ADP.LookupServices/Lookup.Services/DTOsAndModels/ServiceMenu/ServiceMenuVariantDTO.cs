using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;

/// <summary>One menu variant of a model, and the services it offers.</summary>
[Docable]
public class ServiceMenuVariantDTO
{
    /// <summary>The variant's id in the menu catalog.</summary>
    public long VariantID { get; set; }

    /// <summary>The variant's name as authored (e.g. a trim or drivetrain qualifier).</summary>
    public string VariantName { get; set; }

    // No model name. The caller looked the menu up BY the model, so it already has one — and the menus
    // catalog's own vehicle-model name is authored separately from the vehicle database's, so echoing it
    // here would put a second, occasionally disagreeing name next to the one the caller is displaying.

    /// <summary>The Brand Hash ID from the Identity System.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string BrandID { get; set; }

    /// <summary>
    /// The brand mapping's company code, as the DMS export writes it. Null when the brand has no
    /// mapping row. Distinct from the abbreviation embedded in each line's labour code.
    /// </summary>
    public string BrandCode { get; set; }

    /// <summary>The discount percentage applied to every line's total. Null when the variant has none.</summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// The variant's menu is offered free of charge.
    ///
    /// <para><b>The prices below are unaffected.</b> Nothing in the pipeline zeroes a total for a free
    /// variant, so a UI that shows the line totals verbatim quotes a customer for a menu the catalog calls
    /// free. Read this first and render "free" instead.</para>
    ///
    /// <para>Filter by it with <see cref="ServiceMenuLookupRequest.FreeFilter"/>.</para>
    /// </summary>
    public bool IsFree { get; set; }

    /// <summary>
    /// The scheduled services, one per service interval the variant is available for, ordered by
    /// distance. An interval whose group carries no labour detail produces no line — matching the export.
    /// </summary>
    public List<ServiceMenuLineDTO> PeriodicServices { get; set; } = new List<ServiceMenuLineDTO>();

    /// <summary>
    /// The standalone (non-scheduled) services — individual items and item groups that can be sold on
    /// their own. Empty when the variant has no standalone items.
    /// </summary>
    public List<ServiceMenuLineDTO> StandaloneServices { get; set; } = new List<ServiceMenuLineDTO>();
}
