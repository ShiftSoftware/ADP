using Microsoft.Azure.Cosmos;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Attaches the model's service menu to a vehicle lookup: derive the join key, ask the service-menu lookup,
/// flatten the answer, and — the part that matters — never let any of that fail the VIN lookup.
///
/// <para><b>The join is on a derived key.</b> <see cref="VehicleLookupDTO.BasicModelCode"/> is reduced from the
/// vehicle's Katashiki; the menus catalog's <c>BasicModelCode</c> is typed by an author. Nothing guarantees the
/// two agree, so a miss is an ordinary outcome and is reported as
/// <see cref="VehicleServiceMenuStatus.NotFound"/> rather than as an absence. Counting those against
/// <see cref="VehicleServiceMenuStatus.Found"/> is how a deployment measures the hit rate open item O3 asks
/// about (COSMOS_REPLICATION_PLAN.md §11).</para>
///
/// <para><b>Menu faults are contained, not swallowed.</b> An unprovisioned container, or documents referencing
/// master data they do not carry, would otherwise take down every VIN lookup in a deployment that never
/// finished setting menus up — for a section that is additive. Those become
/// <see cref="VehicleServiceMenuStatus.Unavailable"/>, which is visible in the response rather than silent.
/// <b>Only the menu subsystem's own enumerated faults are contained</b> — the two named service-menu
/// exceptions and <see cref="CosmosException"/>. Anything else propagates, deliberately: a bug in this
/// assembly, or in a host's <c>CountrySettingsResolver</c>, should surface as a failure rather than as a
/// section that is quietly "unavailable" forever. Keep that resolver total.</para>
///
/// <para>Cancellation is not a fault and is never converted to a status — it propagates.</para>
/// </summary>
public class VehicleServiceMenuEvaluator
{
    private readonly ServiceMenuLookupService serviceMenuLookupService;

    /// <param name="serviceMenuLookupService">
    /// May be null: a host can build the vehicle lookup without ever registering service menus, and the
    /// section then reports <see cref="VehicleServiceMenuStatus.NotRegistered"/> instead of throwing.
    /// </param>
    public VehicleServiceMenuEvaluator(ServiceMenuLookupService serviceMenuLookupService)
    {
        this.serviceMenuLookupService = serviceMenuLookupService;
    }

    /// <summary>
    /// The section for one vehicle, or <c>null</c> when the request did not ask for one.
    ///
    /// <para>The opt-in check lives here rather than at the call site so that "did the caller want a menu"
    /// is decided in the same place as every other menu question, and can be tested without standing up a
    /// whole vehicle lookup. Once a caller HAS asked, this always returns a DTO — "why is there no menu" is
    /// then part of the answer, not an absence.</para>
    /// </summary>
    /// <param name="basicModelCode">The derived join key, usually <see cref="VehicleLookupDTO.BasicModelCode"/>.</param>
    /// <param name="requestOptions">
    /// Carries the opt-in (<see cref="VehicleServiceMenuRequestOptions.Include"/>), the language, and the
    /// optional country and transfer rate.
    /// </param>
    public async Task<VehicleServiceMenuDTO> EvaluateAsync(
        string basicModelCode,
        VehicleLookupRequestOptions requestOptions,
        CancellationToken cancellationToken = default)
    {
        var menuOptions = requestOptions?.ServiceMenuOptions;

        // No options object and an un-set Include mean the same thing. Note this is NOT "options were
        // supplied, therefore include" — a caller that fills in a country but never asks for the section
        // gets no section, and that has to stay true through any later tidying of this line.
        if (menuOptions is null || !menuOptions.Include)
            return null;

        var code = basicModelCode?.Trim();

        var section = new VehicleServiceMenuDTO
        {
            BasicModelCode = string.IsNullOrEmpty(code) ? null : code,
            Language = requestOptions?.LanguageCode,
            Status = VehicleServiceMenuStatus.NoBasicModelCode,
        };

        if (section.BasicModelCode is null)
            return section;

        if (serviceMenuLookupService is null)
        {
            section.Status = VehicleServiceMenuStatus.NotRegistered;
            return section;
        }

        ServiceMenuLookupDTO menu;

        try
        {
            menu = await serviceMenuLookupService.GetMenuAsync(
                new ServiceMenuLookupRequest
                {
                    BasicModelCode = code,
                    CountryID = menuOptions?.CountryID,
                    TransferRate = menuOptions?.TransferRate,
                    Language = requestOptions?.LanguageCode,
                },
                cancellationToken);
        }
        catch (ServiceMenuContainerNotFoundException)
        {
            // The menu containers were never provisioned. A provisioning fault is worth raising to a caller
            // that asked for a menu and nothing else (ServiceMenuLookupService does exactly that) — but here
            // it would fail a VIN lookup that has nothing to do with menus, permanently and for every vehicle.
            section.Status = VehicleServiceMenuStatus.Unavailable;
            return section;
        }
        catch (ServiceMenuGenerationException)
        {
            // The documents point at master data they do not carry — an incomplete replication. Same reasoning:
            // one model's broken menu must not be able to break that model's VIN lookups.
            section.Status = VehicleServiceMenuStatus.Unavailable;
            return section;
        }
        catch (CosmosException)
        {
            // Throttling, a transient read failure, a container the host cannot reach. Storage-layer faults on
            // an additive section do not get to fail the request.
            section.Status = VehicleServiceMenuStatus.Unavailable;
            return section;
        }

        section.CountryID = menu.CountryID;
        section.TransferRate = menu.TransferRate;
        section.Language = menu.Language ?? section.Language;

        if (menu.NotFound)
        {
            section.Status = VehicleServiceMenuStatus.NotFound;
            return section;
        }

        section.Status = VehicleServiceMenuStatus.Found;
        section.Services = Flatten(menu);

        return section;
    }

    /// <summary>
    /// Nested → flat, preserving order: per variant, the scheduled services (already in distance order) then
    /// the standalone ones. Nothing is dropped, merged or re-sorted — the variant simply moves onto the line
    /// so a VIN-shaped caller can render one list.
    /// </summary>
    private static List<VehicleServiceMenuLineDTO> Flatten(ServiceMenuLookupDTO menu) =>
        (menu.Variants ?? new List<ServiceMenuVariantDTO>())
            .SelectMany(variant =>
                (variant.PeriodicServices ?? new List<ServiceMenuLineDTO>())
                    .Concat(variant.StandaloneServices ?? new List<ServiceMenuLineDTO>())
                    .Select(line => Map(variant, line)))
            .ToList();

    private static VehicleServiceMenuLineDTO Map(ServiceMenuVariantDTO variant, ServiceMenuLineDTO line) =>
        new VehicleServiceMenuLineDTO
        {
            VariantID = variant.VariantID,
            VariantName = variant.VariantName,

            LineKey = line.LineKey,
            Code = line.Code,
            LabourCode = line.LabourCode,
            Description = line.Description,
            LineType = line.LineType,
            IsStandalone = line.IsStandalone,

            ServiceIntervalCode = line.ServiceIntervalCode,
            ServiceIntervalValueInMeter = line.ServiceIntervalValueInMeter,

            LabourRate = line.LabourRate,
            AllowedTime = line.AllowedTime,
            LabourPrice = line.LabourPrice,
            Consumable = line.Consumable,
            LabourTotalPrice = line.LabourTotalPrice,

            Parts = (line.Parts ?? new List<ServiceMenuPartDTO>()).Select(MapPart).ToList(),
            PartsTotalPrice = line.PartsTotalPrice,

            DiscountPercentage = line.DiscountPercentage,
            DiscountAmount = line.DiscountAmount,
            TotalPrice = line.TotalPrice,

            HasUnpricedParts = line.HasUnpricedParts,

            // No Cost / TotalCost / margin of any kind, on either type. See VehicleServiceMenuLineDTO.
        };

    private static VehicleServiceMenuPartDTO MapPart(ServiceMenuPartDTO part) =>
        new VehicleServiceMenuPartDTO
        {
            PartNumber = part.PartNumber,
            SortOrder = part.SortOrder,
            Quantity = part.Quantity,
            UnitPrice = part.UnitPrice,
            TotalPrice = part.TotalPrice,
            HasCountryPrice = part.HasCountryPrice,
        };
}
