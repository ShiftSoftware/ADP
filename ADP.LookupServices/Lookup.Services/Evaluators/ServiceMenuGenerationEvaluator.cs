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
    /// Resolves the config a request generates under: the country from the request, then
    /// <see cref="ServiceMenuLookupOptions.DefaultCountryID"/>, then 0; the transfer rate and
    /// labour-rate mode from <see cref="ServiceMenuLookupOptions.CountrySettingsResolver"/> when the
    /// host wired one, otherwise from the request.
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
            TransferRate = settings?.TransferRate ?? request?.TransferRate ?? 1m,
            UsePrimaryLabourRate = settings?.UsePrimaryLabourRate ?? false,
            Language = request?.Language,

            // Left at its default on purpose — see the class remarks. Do not set this.
            // IncludePartCost = false
        };
    }

    /// <summary>
    /// Generates every line the documents produce under <paramref name="config"/>.
    ///
    /// Returns an empty list when the partition holds no live variant. It also returns an empty list
    /// when variants exist but nothing qualifies — an interval whose group carries no labour detail
    /// produces no line, silently, exactly as in the export. That is not a bug to guard against here;
    /// it is the behaviour being matched.
    /// </summary>
    /// <exception cref="ServiceMenuGenerationException">
    /// Reference data the documents point at is missing. The shared generator raises
    /// <see cref="KeyNotFoundException"/> for this and that is deliberate (open item O1 — the export
    /// fails too); it is wrapped so the failure names the model rather than surfacing as a bare
    /// dictionary miss from inside a fold.
    /// </exception>
    public IReadOnlyList<GeneratedMenuLine> Evaluate(ServiceMenuDocuments documents, MenuGenerationConfig config)
    {
        if (documents is null || documents.IsEmpty)
            return [];

        var request = CosmosToGenerationAggregator.Build(documents);

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
