using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Selects the vehicle entry that anchors a lookup. Preference order:
/// <list type="number">
/// <item>An <b>end-customer sale</b> entry — a dealer's, or a direct distributor-to-customer sale (see
/// <see cref="LookupOptions.IsEndCustomerSale"/>). A plain distributor or intermediary entry is a supply-chain
/// movement, not a sale to the end customer, so it must not anchor the warranty/free-service start, service-item
/// eligibility, or the reported sale when a real end-customer sale exists.</item>
/// <item>If no end-customer entry exists yet (e.g. the dealer's entry has not synced), <b>any</b> entry, so the
/// VIN is still found for spec/identifiers. The warranty evaluator then withholds the start date until a
/// genuine end-customer sale appears.</item>
/// </list>
/// Within the chosen set the rule is unchanged: the entry still in stock (no invoice date) if any, otherwise
/// the most recently invoiced. The entry provides spec and sale/invoice fields only; the owning
/// company/country/region is resolved separately by <see cref="VehicleOwnershipEvaluator"/>, because the
/// entry's own ownership fields can be stale or empty (e.g. national stock not yet allocated to the
/// activating company).
/// </summary>
public class VehicleEntryEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;

    public VehicleEntryEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
    }

    public VehicleEntryModel Evaluate()
    {
        var vehicles = this.CompanyDataAggregate.VehicleEntries;

        if (!(vehicles?.Count > 0))
            return null;

        // Prefer the end-customer-sale entries; fall back to the full set only when none exist yet, so the VIN is
        // still found while the dealer's own entry has not synced. An end-customer sale is a dealer's entry or a
        // direct distributor-to-customer sale (IsEndCustomerSale); a distributor/intermediary supply-chain leg is not.
        var endCustomerEntries = vehicles.Where(x => Options.IsEndCustomerSale(x)).ToList();
        var pool = endCustomerEntries.Count > 0 ? endCustomerEntries : vehicles;

        // In-stock (no invoice date) takes priority; otherwise the most recently invoiced.
        return pool.FirstOrDefault(x => x.InvoiceDate is null)
            ?? pool.OrderByDescending(x => x.InvoiceDate).FirstOrDefault();
    }
}
