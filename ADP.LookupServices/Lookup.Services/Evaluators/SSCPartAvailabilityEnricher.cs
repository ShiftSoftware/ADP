using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Sets <see cref="SSCPartDTO.IsAvailable"/> for the parts of open (not-yet-repaired) SSC (safety recall)
/// records.
/// <para>
/// Enrichment is gated first by the master switch <see cref="LookupOptions.EnableSSCPartAvailability"/> (off by
/// default — nothing runs and all parts stay <c>null</c> until a deployment opts in). Beyond that, availability
/// is only meaningful for a requester that has a stock scope (a Hub user tied to a region/branch), so it also
/// runs only when the host has configured <see cref="LookupOptions.SSCPartStockScopeResolver"/>.
/// That resolver returns the stock <see cref="StockPartModel.Location"/> key(s) the requester may see — a
/// logic-free identity→scope mapping. <b>This class owns every availability decision</b>: filtering rows to the
/// in-scope locations, the per-part rule (<c>AvailableQuantity &gt; 0</c>), and the gate below. When no resolver
/// is configured (anonymous / bulk / reporting callers) nothing is fetched and parts are left unchecked
/// (<c>IsAvailable = null</c>), which the UI shows as a neutral grey chip rather than "unavailable".
/// </para>
/// <para>
/// SSC part numbers arrive in the manufacturer / standard form (e.g. <c>04007-07212</c>), which is not
/// necessarily how the deployment stores them in the Cosmos Parts container (one deployment stores
/// <c>T0400707212</c>; another keeps <c>04007-07212</c>). The host maps between the two via
/// <see cref="LookupOptions.PartNumberStorageKeyResolver"/>; ADP applies a neutral default (dash-strip +
/// upper-case) when it is unset.
/// </para>
/// <para>
/// Availability is only evaluated for recalls whose <see cref="SscDTO.Repaired"/> is <c>false</c>. For a
/// repaired recall the part was already replaced (often years ago), so reporting current stock is at best noise
/// and at worst contradictory ("repaired" + "part unavailable"); those parts are left unchecked
/// (<c>IsAvailable = null</c>).
/// </para>
/// </summary>
public sealed class SSCPartAvailabilityEnricher
{
    private readonly LookupOptions options;

    public SSCPartAvailabilityEnricher(LookupOptions options)
    {
        this.options = options;
    }

    /// <summary>
    /// Resolves the requester's stock scope (host-provided), fetches stock for the open recalls' part numbers
    /// (mapped to the deployment's storage keys), and applies availability onto <paramref name="sscs"/>. A no-op
    /// when no scope resolver is configured, there are no open recalls with parts, the resolver yields no scope,
    /// or no stock data source is available.
    /// </summary>
    public async Task EnrichAsync(
        IEnumerable<SscDTO>? sscs,
        string vin,
        VehicleLookupRequestOptions requestOptions,
        IServiceProvider services)
    {
        // Master switch: the feature ships off (LookupOptions.EnableSSCPartAvailability defaults to false) so a
        // deployment can take the NuGet and wire the resolver without turning availability on until it has
        // confirmed its stock-scope mapping. Off => no stock read and every IsAvailable stays null (not checked).
        if (options?.EnableSSCPartAvailability != true)
            return;

        var resolver = options?.SSCPartStockScopeResolver;

        if (resolver is null || sscs is null || services is null)
            return;

        // Only open (not-yet-repaired) recalls need availability — skip repaired ones entirely, including their
        // stock fetch.
        var distinctPartNumbers = sscs
            .Where(s => s is not null && !s.Repaired)
            .SelectMany(s => s.Parts ?? Enumerable.Empty<SSCPartDTO>())
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.PartNumber))
            .Select(p => p.PartNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctPartNumbers.Count == 0)
            return;

        var scope = await resolver(new LookupOptionResolverModel<SSCPartAvailabilityScopeRequest>(
            new SSCPartAvailabilityScopeRequest(vin, distinctPartNumbers),
            requestOptions?.LanguageCode,
            services));

        if (scope is null || scope.Count == 0)
            return;

        var stockService = services.GetService<PartLookupCosmosService>();
        if (stockService is null)
            return;

        var storageKeyResolver = options?.PartNumberStorageKeyResolver;

        // Query stock by the deployment's storage keys (e.g. a T-prefixed, dash-stripped form), not the raw
        // manufacturer part numbers on the recall.
        var storageKeys = distinctPartNumbers
            .Select(p => ToStorageKey(p, storageKeyResolver))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var stockRows = await stockService.GetStockPartsAsync(storageKeys);

        ApplyAvailability(sscs, stockRows, scope, storageKeyResolver);
    }

    /// <summary>
    /// The pure availability decision (no I/O). For each open (<see cref="SscDTO.Repaired"/> = false) recall, sets
    /// <see cref="SSCPartDTO.IsAvailable"/> to <c>true</c> when a stock row for the part in one of the
    /// <paramref name="scopeLocationKeys"/> has a positive <see cref="StockPartModel.AvailableQuantity"/>, otherwise
    /// <c>false</c>. Parts that are never evaluated — those on repaired recalls, and every part when the requester
    /// has no stock scope — are left unchecked (<c>IsAvailable = null</c>), distinct from an evaluated "not in
    /// stock" (<c>false</c>). Each recall part number is mapped to its stored key via
    /// <paramref name="partNumberStorageKeyResolver"/> (or the neutral default) before comparing to the stock
    /// rows, which are already stored in that key form.
    /// </summary>
    public static void ApplyAvailability(
        IEnumerable<SscDTO>? sscs,
        IEnumerable<StockPartModel>? stockRows,
        IReadOnlyCollection<string>? scopeLocationKeys,
        Func<string, string>? partNumberStorageKeyResolver = null)
    {
        if (sscs is null || stockRows is null)
            return;

        var scope = new HashSet<string>(
            (scopeLocationKeys ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        // A requester with no stock scope has no availability visibility, so every part is left "not checked"
        // (null) rather than reported "not in stock" (false).
        if (scope.Count == 0)
            return;

        // Stored keys that have positive stock in at least one in-scope location. The stock row's PartNumber is
        // already the storage key, so it is used as-is (only trimmed / case-folded).
        var availableStorageKeys = new HashSet<string>(
            stockRows
                .Where(s => s is not null && !string.IsNullOrWhiteSpace(s.PartNumber))
                .Where(s => scope.Contains(s.Location ?? string.Empty) && s.AvailableQuantity > 0m)
                .Select(s => s.PartNumber.Trim()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var ssc in sscs)
        {
            if (ssc is null || ssc.Repaired)
                continue;

            foreach (var part in ssc.Parts ?? Enumerable.Empty<SSCPartDTO>())
            {
                if (part is null || string.IsNullOrWhiteSpace(part.PartNumber))
                    continue;

                part.IsAvailable = availableStorageKeys.Contains(ToStorageKey(part.PartNumber, partNumberStorageKeyResolver));
            }
        }
    }

    private static string ToStorageKey(string partNumber, Func<string, string>? resolver)
    {
        if (string.IsNullOrWhiteSpace(partNumber))
            return string.Empty;

        var key = resolver is not null ? resolver(partNumber) : DefaultStorageKey(partNumber);
        return key?.Trim() ?? string.Empty;
    }

    // Neutral fallback when the host does not supply a PartNumberStorageKeyResolver: dashes removed, upper-cased.
    private static string DefaultStorageKey(string partNumber) =>
        partNumber?.Trim().Replace("-", string.Empty).ToUpperInvariant() ?? string.Empty;
}
