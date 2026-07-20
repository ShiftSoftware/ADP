using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Linq;

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

        //Extended Warranty
        var lastExtendedWarrantyEntry = CompanyDataAggregate
            .ExtendedWarrantyEntries?
            .OrderByDescending(x => x.EndDate)?
            .FirstOrDefault();

        if (lastExtendedWarrantyEntry is not null)
        {
            result.ExtendedWarrantyStartDate = lastExtendedWarrantyEntry.StartDate;
            result.ExtendedWarrantyEndDate = lastExtendedWarrantyEntry.EndDate;
        }

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
        result.HasExtendedWarranty = result.ExtendedWarrantyEndDate.HasValue && result.ExtendedWarrantyEndDate.Value >= nowUtc;

        return result;
    }
}