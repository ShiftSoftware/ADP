using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// The print/display data model for a vehicle's <b>Paint Thickness Certificate</b>: the readings from the
/// latest PDI (Pre-Delivery Inspection) paint-thickness inspection taken <i>before</i> the distributor's sale
/// invoice date, plus vehicle header information.
/// </summary>
/// <remarks>
/// Produced by <c>PaintThicknessCertificateEvaluator</c> and surfaced via
/// <c>VehicleLookupService.GetPaintThicknessCertificateAsync</c>. Intended to be consumed by a vehicle-lookup
/// HTTP function to render the certificate PDF and a customer-facing landing page (readings table + PDF viewer
/// + panel-image gallery).
/// </remarks>
[Docable]
public class PaintThicknessCertificateModel
{
    // ---- Vehicle header ----

    /// <summary>The Vehicle Identification Number.</summary>
    public string VIN { get; set; }

    /// <summary>A human-readable description of the vehicle model.</summary>
    public string ModelDescription { get; set; }

    /// <summary>The model code that identifies the vehicle model.</summary>
    public string ModelCode { get; set; }

    /// <summary>The model year of the vehicle.</summary>
    public string ModelYear { get; set; }

    /// <summary>The exterior color code of the vehicle.</summary>
    public string ExteriorColorCode { get; set; }

    /// <summary>The Katashiki (manufacturer model-specific identifier).</summary>
    public string Katashiki { get; set; }

    /// <summary>The variant code of the vehicle within its model range.</summary>
    public string VariantCode { get; set; }

    // ---- Sale / dealer header ----

    /// <summary>The sale invoice date — the anchor the inspection is selected against.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    /// <summary>The sale invoice number.</summary>
    public string InvoiceNumber { get; set; }

    /// <summary>The resolved selling company (dealer) name.</summary>
    public string CompanyName { get; set; }

    /// <summary>The resolved selling branch name.</summary>
    public string BranchName { get; set; }

    /// <summary>The resolved city name.</summary>
    public string CityName { get; set; }

    /// <summary>The resolved country name.</summary>
    public string CountryName { get; set; }

    // ---- Chosen inspection ----

    /// <summary>The source of the chosen inspection (e.g. "PDI").</summary>
    public string Source { get; set; }

    /// <summary>The date the chosen paint thickness inspection was performed.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InspectionDate { get; set; }

    // ---- Readings + gallery ----

    /// <summary>The per-panel <see cref="PaintThicknessCertificateReadingModel">readings</see> for the certificate table.</summary>
    public List<PaintThicknessCertificateReadingModel> Readings { get; set; } = [];

    /// <summary>All distinct resolved panel-image URLs, for the landing-page gallery.</summary>
    public List<string> PanelImages { get; set; } = [];
}
