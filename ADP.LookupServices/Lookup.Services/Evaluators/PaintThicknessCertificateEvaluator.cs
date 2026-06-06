using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Builds the <see cref="PaintThicknessCertificateModel">Paint Thickness Certificate</see> for a vehicle:
/// the latest PDI paint-thickness inspection taken strictly <i>before</i> the distributor's sale invoice date,
/// plus vehicle header information.
/// </summary>
/// <remarks>
/// Plain evaluator (no DI), modeled on <see cref="VehiclePaintThicknessEvaluator"/>. Reads everything off the
/// shared <see cref="CompanyDataAggregateModel"/> (no extra data access). The invoice-date anchor is the
/// <b>distributor's</b> VehicleEntry — scoped via <see cref="LookupOptions.DistributorCompanyID"/> — never a
/// dealer's: a VIN can have entries for both the distributor and the selling dealer(s), and the dealer's sale
/// invoice is later, so an unscoped "latest invoice" would pick the wrong date.
/// </remarks>
public class PaintThicknessCertificateEvaluator
{
    /// <summary>The <see cref="PaintThicknessInspectionModel.Source"/> value that identifies a PDI inspection.</summary>
    public const string PdiSource = "PDI";

    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;

    public PaintThicknessCertificateEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider serviceProvider)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Whether a certificate can be produced for this vehicle, using exactly the same anchor + PDI
    /// selection as <see cref="Evaluate"/> but skipping the model assembly and image resolution.
    /// Surfaced on <c>VehicleLookupDTO.PaintThicknessCertificateAvailable</c> so UIs can offer the
    /// certificate without re-implementing the anchor logic.
    /// </summary>
    public bool EvaluateAvailability()
    {
        return SelectQualifyingInspection() is not null;
    }

    /// <param name="languageCode">Language used to resolve panel-image URLs.</param>
    /// <returns>The certificate model, or <c>null</c> when there is no distributor invoice date or no qualifying PDI inspection.</returns>
    public async Task<PaintThicknessCertificateModel> Evaluate(string languageCode)
    {
        // 1. Anchor on the distributor's sale invoice date — never a dealer's later sale (see SelectAnchorEntry).
        var anchorEntry = SelectAnchorEntry();

        // 2. Latest PDI inspection strictly before the invoice date.
        var chosen = SelectQualifyingInspection(anchorEntry);

        if (chosen is null)
            return null; // no distributor invoice or no qualifying PDI inspection -> no certificate

        var invoiceDate = anchorEntry.InvoiceDate;

        // 3. Assemble the print model.
        var readings = new List<PaintThicknessCertificateReadingModel>();
        var allImages = new List<string>();

        foreach (var panel in chosen.Panels ?? Enumerable.Empty<PaintThicknessInspectionPanelModel>())
        {
            if (panel is null)
                continue;

            var images = panel.Images is null
                ? new List<string>()
                : (await Task.WhenAll(panel.Images.Select(i => GetPaintThicknessImageFullUrl(i, languageCode)))).ToList();

            readings.Add(new PaintThicknessCertificateReadingModel
            {
                PanelType = panel.PanelType,
                PanelSide = panel.PanelSide,
                PanelPosition = panel.PanelPosition,
                MeasuredThickness = panel.MeasuredThickness,
                PanelLabel = BuildPanelLabel(panel),
                Images = images,
            });

            allImages.AddRange(images);
        }

        // Company/branch/city/country header names are intentionally left unset for now.
        // Vehicle descriptors and invoice fields come from the distributor's anchor entry.
        return new PaintThicknessCertificateModel
        {
            SerialNumber = await ResolveSerialNumber(chosen.id, languageCode),
            VIN = anchorEntry.VIN ?? CompanyDataAggregate.VIN ?? chosen.VIN,
            ModelDescription = anchorEntry.ModelDescription ?? chosen.ModelDescription,
            ModelCode = anchorEntry.ModelCode,
            ModelYear = anchorEntry.ModelYear?.ToString() ?? chosen.ModelYear,
            ExteriorColorCode = anchorEntry.ExteriorColorCode ?? chosen.ColorCode,
            Katashiki = anchorEntry.Katashiki,
            VariantCode = anchorEntry.VariantCode,

            InvoiceDate = invoiceDate,
            InvoiceNumber = anchorEntry.InvoiceNumber,

            Source = chosen.Source,
            InspectionDate = chosen.InspectionDate,

            Readings = readings,
            PanelImages = allImages.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
        };
    }

    /// <summary>
    /// Selects the distributor's invoiced VehicleEntry that anchors the certificate: the latest entry with a
    /// non-null InvoiceDate whose <c>CompanyID</c> equals <see cref="LookupOptions.DistributorCompanyID"/>.
    /// </summary>
    /// <remarks>
    /// Strict: only the distributor's own invoiced entry qualifies. If <c>DistributorCompanyID</c> is not
    /// configured, or the VIN has no invoiced entry for it, this returns <c>null</c> and no certificate is
    /// produced — it never falls back to a dealer's (later) invoice. Deliberately does NOT reuse
    /// <see cref="VehicleEntryEvaluator"/> (prefers null-invoice in-stock entries) nor
    /// <see cref="VehicleSaleInformation.InvoiceDate"/> (can be a dealer's date, or null via its
    /// service-activation branch). Note: <c>VehicleEntries</c> is not filtered by IsDeleted by the storage loader.
    /// </remarks>
    private VehicleEntryModel SelectAnchorEntry()
    {
        if (Options?.DistributorCompanyID is null)
            return null;

        return CompanyDataAggregate.VehicleEntries?
            .Where(e => e?.InvoiceDate is not null && e.CompanyID == Options.DistributorCompanyID)
            .OrderByDescending(e => e.InvoiceDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Selects the PDI inspection the certificate is based on: the latest inspection with a trimmed,
    /// case-insensitive <c>"PDI"</c> source dated strictly before the anchor invoice date (same-day ties
    /// break by <c>id</c>). Returns <c>null</c> when there is no anchor entry or no qualifying inspection.
    /// </summary>
    private PaintThicknessInspectionModel SelectQualifyingInspection(VehicleEntryModel anchorEntry = null)
    {
        var invoiceDate = (anchorEntry ?? SelectAnchorEntry())?.InvoiceDate;

        if (invoiceDate is null)
            return null;

        return CompanyDataAggregate.PaintThicknessInspections?
            .Where(p => p != null
                     && string.Equals(p.Source?.Trim(), PdiSource, StringComparison.OrdinalIgnoreCase)
                     && p.InspectionDate.HasValue
                     && p.InspectionDate.Value.Date < invoiceDate.Value.Date)
            .OrderByDescending(p => p.InspectionDate)
            .ThenByDescending(p => p.id)
            .FirstOrDefault();
    }

    private static string BuildPanelLabel(PaintThicknessInspectionPanelModel panel)
    {
        // Mirrors the panel label format used in the LegacyPaintThickness branch of VehicleLookupService,
        // but omits an empty "()" qualifier when neither position nor side is set.
        var qualifier = string.Join(" ", new[] { panel.PanelPosition.Describe(), panel.PanelSide.Describe() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.IsNullOrWhiteSpace(qualifier)
            ? panel.PanelType.Describe()
            : $"{panel.PanelType.Describe()} ({qualifier})";
    }

    private async Task<string> GetPaintThicknessImageFullUrl(string image, string languageCode)
    {
        if (Options?.PaintThickneesImageUrlResolver is not null)
            return await Options.PaintThickneesImageUrlResolver(new(image, languageCode, ServiceProvider));

        return image;
    }

    private async Task<string> ResolveSerialNumber(string inspectionId, string languageCode)
    {
        if (Options?.PaintThicknessCertificateSerialNumberResolver is not null)
            return await Options.PaintThicknessCertificateSerialNumberResolver(new(inspectionId, languageCode, ServiceProvider));

        return null;
    }
}
