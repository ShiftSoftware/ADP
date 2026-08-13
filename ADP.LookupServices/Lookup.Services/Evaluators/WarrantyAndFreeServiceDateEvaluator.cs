using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class WarrantyAndFreeServiceDateEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;

    public WarrantyAndFreeServiceDateEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
    }

    public VehicleWarrantyDTO Evaluate(VehicleEntryModel vehicle, VehicleSaleInformation saleInformation, bool ignoreBrokerStock)
        => EvaluateCore(vehicle, saleInformation, ignoreBrokerStock);

    /// <summary>
    /// Evaluates warranty dates and resolves each extended-warranty provider logo through the
    /// host's existing company-logo resolver.
    /// </summary>
    public async Task<VehicleWarrantyDTO> EvaluateAsync(
        VehicleEntryModel vehicle,
        VehicleSaleInformation saleInformation,
        bool ignoreBrokerStock,
        string languageCode,
        IServiceProvider serviceProvider)
    {
        var result = EvaluateCore(vehicle, saleInformation, ignoreBrokerStock);

        if (Options.CompanyLogoResolver is null)
            return result;

        var logoByProvider = new Dictionary<long, string>();
        foreach (var warranty in result.ExtendedWarranties)
        {
            if (!long.TryParse(warranty.ProviderCompanyID, out var providerCompanyID))
                continue;

            if (!logoByProvider.TryGetValue(providerCompanyID, out var logo))
            {
                logo = await Options.CompanyLogoResolver(
                    new LookupOptionResolverModel<long?>(providerCompanyID, languageCode, serviceProvider));
                logoByProvider[providerCompanyID] = logo;
            }

            warranty.ProviderCompanyLogo = logo;
        }

        return result;
    }

    private VehicleWarrantyDTO EvaluateCore(
        VehicleEntryModel vehicle,
        VehicleSaleInformation saleInformation,
        bool ignoreBrokerStock)
    {
        DateTime? warrantyStartDate = null;
        DateTime? freeServiceStartDate = null;

        //Normal company Sale
        if (saleInformation?.Broker is null)
        {
            warrantyStartDate = CompanyDataAggregate.VehicleServiceActivations.FirstOrDefault()?.WarrantyActivationDate;

            // A distributor/intermediary entry is a supply-chain movement, not an end-customer sale, so its
            // warranty-activation/invoice date must never seed the warranty or free-service start. The anchor can
            // still be such an entry when the dealer's own entry has not synced yet (sync delay) or shares the
            // distributor's invoice date — in those cases the start date stays null until the dealer sale appears.
            // A direct distributor-to-customer sale is the exception (IsEndCustomerSale): it *is* the sale and does
            // seed the dates.
            if (warrantyStartDate is null && Options.IsEndCustomerSale(vehicle))
            {
                warrantyStartDate = saleInformation?.WarrantyActivationDate;

                if (warrantyStartDate is null && Options.WarrantyStartDateDefaultsToInvoiceDate)
                    warrantyStartDate = saleInformation?.InvoiceDate;
            }

            freeServiceStartDate = warrantyStartDate;
        }
        else
        {
            //Broker Stock
            if (saleInformation.Broker.InvoiceDate is null)
            {
                if (ignoreBrokerStock)
                {
                    warrantyStartDate = null;

                    freeServiceStartDate = CompanyDataAggregate.VehicleServiceActivations.FirstOrDefault()?.WarrantyActivationDate;

                    // Same end-customer-sale guard as the normal branch: a distributor/intermediary entry's
                    // dates must not seed the free-service start (a direct distributor-to-customer sale excepted).
                    if (freeServiceStartDate is null && Options.IsEndCustomerSale(vehicle))
                    {
                        freeServiceStartDate = saleInformation?.WarrantyActivationDate;

                        if (freeServiceStartDate is null && Options.WarrantyStartDateDefaultsToInvoiceDate)
                            freeServiceStartDate = saleInformation?.InvoiceDate;
                    }
                }
            }
            //Normal Broker Sale
            else
            {
                warrantyStartDate = saleInformation?.Broker?.InvoiceDate;
                freeServiceStartDate = saleInformation?.Broker?.InvoiceDate;
            }
        }

        VehicleWarrantyDTO result = new();

        var shiftDate = CompanyDataAggregate.WarrantyDateShifts?.FirstOrDefault();

        if (shiftDate is not null)
            warrantyStartDate = shiftDate.NewDate;

        if (warrantyStartDate is not null)
        {
            result.WarrantyStartDate = warrantyStartDate;

            if (Options.BrandStandardWarrantyPeriodsInYears.TryGetValue(vehicle.BrandID ?? 0, out var brandStandardWarrantyPeriodsInYears))
            {
                result.WarrantyEndDate = warrantyStartDate?.AddYears(brandStandardWarrantyPeriodsInYears);
            }
            else
            {
                result.WarrantyEndDate = warrantyStartDate?.AddYears(3);
            }
        }

        AddStoredExtendedWarranties(result);
        AddConfiguredExtendedWarranties(result);
        ApplyExtendedWarrantySummary(result);

        // De facto fallback: the earliest non-deleted ItemClaim.ClaimDate. A claim is a real
        // anchor for "service has begun" — if the regular chain produced nothing (typically
        // broker-no-invoice + IgnoreBrokerStock=false), use this so downstream items still
        // project. Always exposed on the DTO regardless of whether it's used.
        var deFactoServiceStartDate = CompanyDataAggregate.ItemClaims?
            .Where(x => x is not null && !x.IsDeleted)
            .Select(x => (DateTimeOffset?)x.ClaimDate)
            .Min()?
            .ToUniversalTime().Date;

        result.DeFactoServiceStartDate = deFactoServiceStartDate;

        if (freeServiceStartDate is null && deFactoServiceStartDate is not null)
            freeServiceStartDate = deFactoServiceStartDate;

        var freeServiceShiftDate = CompanyDataAggregate.FreeServiceItemDateShifts?.FirstOrDefault();

        if (freeServiceShiftDate is not null)
            freeServiceStartDate = freeServiceShiftDate.NewDate;

        result.FreeServiceStartDate = freeServiceStartDate;

        // Stamp the time-derived warranty flags here (against Options.TimeProvider) instead of computing
        // them on the DTO against the wall clock, so a fixed provider freezes them for deterministic
        // sample/doc generation. The DTO is always evaluated fresh per request and read within that same
        // request, so this is behaviourally equivalent to the previous compute-on-read getters.
        var nowUtc = Options.GetUtcNow();
        result.HasActiveWarranty = result.WarrantyEndDate.HasValue && result.WarrantyEndDate.Value >= nowUtc;

        return result;
    }

    private void AddStoredExtendedWarranties(VehicleWarrantyDTO result)
    {
        result.ExtendedWarranties.AddRange(
            (CompanyDataAggregate.ExtendedWarrantyEntries ?? Enumerable.Empty<ExtendedWarrantyModel>())
            .Where(entry => entry is not null &&
                !string.IsNullOrWhiteSpace(entry.id) &&
                entry.CompanyID is > 0)
            .Select(entry => new VehicleExtendedWarrantyDTO
            {
                ID = entry.id,
                ProviderCompanyID = entry.CompanyID?.ToString(),
                StartDate = entry.StartDate,
                EndDate = entry.EndDate,
            }));
    }

    private void AddConfiguredExtendedWarranties(VehicleWarrantyDTO result)
    {
        if (result.WarrantyEndDate is null)
            return;

        var conditionEvaluator = new VehicleEligibilityConditionEvaluator(CompanyDataAggregate);

        foreach (var definition in Options.ExtendedWarrantyDefinitions ?? Enumerable.Empty<ExtendedWarrantyDefinitionModel>())
        {
            var conditions = definition?.EligibilityConditions?.ToList();
            var providerCompanyID = definition?.ProviderCompanyID ?? Options.DistributorCompanyID;
            var hasSupportedDuration = definition?.ActiveForDurationType is { } durationType
                && durationType != DurationType.NotSpecified
                && Enum.IsDefined(typeof(DurationType), durationType);

            // Warranty definitions are opt-in and fail closed. Service items retain their legacy
            // empty-condition behaviour in the shared evaluator, but an empty warranty definition
            // must never silently award coverage to every vehicle.
            if (definition is null ||
                string.IsNullOrWhiteSpace(definition.ID) ||
                result.ExtendedWarranties.Any(warranty =>
                    string.Equals(warranty.ID, definition.ID, StringComparison.Ordinal)) ||
                providerCompanyID is null ||
                providerCompanyID <= 0 ||
                definition.ActiveFor is null ||
                definition.ActiveFor <= 0 ||
                !hasSupportedDuration ||
                conditions is null ||
                conditions.Count == 0 ||
                conditions.Any(condition => condition is null) ||
                !conditionEvaluator.MatchesAll(conditions))
                continue;

            var coverageStart = result.WarrantyEndDate.Value;
            DateTime coverageEnd;
            try
            {
                coverageEnd = DurationCalculator.AddInterval(
                    coverageStart,
                    definition.ActiveFor,
                    definition.ActiveForDurationType);
            }
            catch (ArgumentOutOfRangeException)
            {
                // A single invalid externally configured duration must fail closed without
                // taking down the whole vehicle lookup.
                continue;
            }

            result.ExtendedWarranties.Add(new VehicleExtendedWarrantyDTO
            {
                ID = definition.ID,
                ProviderCompanyID = providerCompanyID.Value.ToString(),
                StartDate = coverageStart,
                EndDate = coverageEnd,
            });
        }
    }

    private static void ApplyExtendedWarrantySummary(VehicleWarrantyDTO result)
    {
        result.HasExtendedWarranty = result.ExtendedWarranties.Count > 0;
        result.ExtendedWarrantyStartDate = result.ExtendedWarranties
            .Where(warranty => warranty.StartDate.HasValue)
            .Select(warranty => warranty.StartDate)
            .Min();
        result.ExtendedWarrantyEndDate = result.ExtendedWarranties
            .Where(warranty => warranty.EndDate.HasValue)
            .Select(warranty => warranty.EndDate)
            .Max();
    }
}
