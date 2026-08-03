using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Menus.Generation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Turns a model's replicated documents into generated menu lines: resolve the generation config,
/// aggregate the documents into the neutral request, run the SHARED generator.
///
/// <para><b>The generator is not reimplemented here, and must never be.</b>
/// <see cref="MenuCodeGenerator"/> is the same static the DMS export calls, which is what makes "a menu
/// code served by the lookup is the code the dealer's DMS received" a property of the build rather than
/// a convention. This evaluator only decides what goes IN.</para>
///
/// <para><b>Dealer cost is held off at its chokepoint.</b> <c>IncludePartCost</c> is left at its
/// <c>false</c> default, so cost is never populated on a generated part and therefore cannot reach a
/// lookup DTO or a public web component by anyone forgetting to strip it. The DMS export is the only
/// consumer that opts in.</para>
/// </summary>
public class ServiceMenuGenerationEvaluator
{
    private readonly ServiceMenuLookupOptions options;

    public ServiceMenuGenerationEvaluator(IOptions<ServiceMenuLookupOptions> options)
    {
        this.options = options?.Value ?? new ServiceMenuLookupOptions();
    }

    /// <summary>
    /// Resolves the config a request generates under.
    ///
    /// <para><b>Country:</b> the request, then <see cref="ServiceMenuLookupOptions.DefaultCountryID"/>, then 0.</para>
    ///
    /// <para><b>Transfer rate:</b> the request when it supplies one, then
    /// <see cref="ServiceMenuLookupOptions.CountrySettingsResolver"/>, then 1. An explicitly supplied rate
    /// wins over the host's resolver on purpose: a caller that sets a value and gets a different one back is
    /// the worse failure — silent, and only visible as money that does not add up. A host that wants the
    /// resolver to be the sole authority simply does not expose the field to its callers.</para>
    ///
    /// <para><b>Labour-rate mode:</b> the resolver only. The request has no way to express it, which is
    /// deliberate — it mirrors the menus host's country normalisation, not a caller's preference.</para>
    /// </summary>
    public async Task<MenuGenerationConfig> ResolveConfigAsync(ServiceMenuLookupRequest request)
    {
        var countryID = request?.CountryID ?? options.DefaultCountryID ?? 0;

        var settings = options.CountrySettingsResolver is not null
            ? await options.CountrySettingsResolver(countryID)
            : null;

        return new MenuGenerationConfig
        {
            CountryID = countryID,
            TransferRate = request?.TransferRate ?? settings?.TransferRate ?? 1m,
            UsePrimaryLabourRate = settings?.UsePrimaryLabourRate ?? false,
            Language = request?.Language,

            // Left at its default on purpose — see the class remarks. Do not set this.
            // IncludePartCost = false
        };
    }

    /// <summary>
    /// Generates every line the documents produce under <paramref name="config"/>, for the variants
    /// <paramref name="freeFilter"/> selects.
    ///
    /// Returns an empty list when the partition holds no live variant. It also returns an empty list
    /// when variants exist but nothing qualifies — an interval whose group carries no labour detail
    /// produces no line, silently, exactly as in the export. That is not a bug to guard against here;
    /// it is the behaviour being matched. A filter that excludes every variant is a third way to get an
    /// empty list, and the caller is the one holding the filter, so it is the one that can tell them apart.
    /// </summary>
    /// <param name="freeFilter">
    /// Which variants to generate, by their free-of-charge flag. Applied to the aggregated request
    /// BEFORE <see cref="MenuCodeGenerator"/> runs, so an excluded variant is never generated — which
    /// also means an excluded variant can never raise the exception below.
    /// </param>
    /// <exception cref="ServiceMenuGenerationException">
    /// Reference data the documents point at is missing. The shared generator raises
    /// <see cref="KeyNotFoundException"/> for this and that is deliberate (open item O1 — the export
    /// fails too); it is wrapped so the failure names the model rather than surfacing as a bare
    /// dictionary miss from inside a fold.
    /// </exception>
    public IReadOnlyList<GeneratedMenuLine> Evaluate(
        ServiceMenuDocuments documents,
        MenuGenerationConfig config,
        ServiceMenuFreeFilter freeFilter = ServiceMenuFreeFilter.All)
    {
        if (documents is null || documents.IsEmpty)
            return [];

        var request = CosmosToGenerationAggregator.Build(documents);

        // The filter lives here rather than in the generator or the aggregator on purpose: both of those
        // are SHARED with the DMS export, which has no such notion. Applying it to the neutral request
        // keeps the generator's input honest — it still receives "the variants to generate", and merely
        // receives fewer of them.
        if (freeFilter != ServiceMenuFreeFilter.All)
            request.Variants.RemoveAll(variant => variant.IsFree != (freeFilter == ServiceMenuFreeFilter.FreeOnly));

        if (request.Variants.Count == 0)
            return [];

        try
        {
            return MenuCodeGenerator.Generate(request, config).ToList();
        }
        catch (KeyNotFoundException ex)
        {
            throw new ServiceMenuGenerationException(documents.BasicModelCode, ex);
        }
    }
}
