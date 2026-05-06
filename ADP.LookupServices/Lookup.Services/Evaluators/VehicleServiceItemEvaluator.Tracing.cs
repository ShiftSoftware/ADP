using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Trace-only methods for <see cref="VehicleServiceItemEvaluator"/>. Lives in a separate
/// partial file so the main file reads as the 7-step recipe without trace plumbing in
/// the way. New trace features (more entity resolutions, additional notes, etc.) belong here.
/// </summary>
public partial class VehicleServiceItemEvaluator
{
    /// <summary>
    /// Single async pass after evaluation — collects every distinct brand / company / country
    /// ID that appears in the trace and resolves it via the configured
    /// <see cref="LookupOptions"/> resolvers. One round-trip per unique ID. Skipped entirely
    /// when trace is off (the dispatcher in <c>Evaluate</c> guards on <c>Trace.IsEnabled</c>)
    /// or when a resolver is not configured. The resolved names land on
    /// <see cref="ServiceItemTrace.ResolvedNames"/> for the renderer to use.
    /// </summary>
    private async Task EnrichTraceWithResolvedNames(string languageCode)
    {
        var trace = Trace.Peek();
        if (trace is null) return;

        var brandIds = new HashSet<long>();
        var companyIds = new HashSet<long>();
        var countryIds = new HashSet<long>();

        if (trace.Inputs?.BrandID is long br) brandIds.Add(br);
        if (trace.Inputs?.CompanyID is long c) companyIds.Add(c);
        if (trace.Inputs?.SaleCountryID is long co) countryIds.Add(co);

        foreach (var d in trace.Eligibility?.Decisions ?? new())
        {
            if (d.Item?.BrandIDs is { } bs)
                foreach (var id in bs) if (id is long v) brandIds.Add(v);
            if (d.Item?.CompanyIDs is { } cs)
                foreach (var id in cs) if (id is long v) companyIds.Add(v);
            if (d.Item?.CountryIDs is { } ks)
                foreach (var id in ks) if (id is long v) countryIds.Add(v);
        }

        var names = trace.ResolvedNames ??= new ServiceItemTraceNameResolutions();

        await ResolveAll(options?.BrandNameResolver, brandIds, names.Brands, languageCode);
        await ResolveAll(options?.CompanyNameResolver, companyIds, names.Companies, languageCode);
        await ResolveAll(options?.CountryNameResolver, countryIds, names.Countries, languageCode);
    }

    private async Task ResolveAll(
        Func<LookupOptionResolverModel<long?>, ValueTask<string?>>? resolver,
        HashSet<long> ids,
        Dictionary<long, string> bucket,
        string languageCode)
    {
        if (resolver is null) return;
        foreach (var id in ids)
        {
            var name = await resolver(new(id, languageCode, services));
            if (!string.IsNullOrWhiteSpace(name)) bucket[id] = name;
        }
    }
}
