using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Menus.Generation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// Turns a basic model code into a service menu: the DMS menu codes, labour codes and prices for every
/// service the model's menu offers.
///
/// <para><b>The codes are the export's codes.</b> The menu catalog is replicated per row into Cosmos and
/// folded at read time by the SAME <see cref="MenuCodeGenerator"/> the DMS export calls — so this is not
/// a second implementation that has to be kept in step, it is the same one over the same contract. See
/// ADP.Menus/COSMOS_REPLICATION_PLAN.md §1.1.</para>
///
/// <para><b>One partition read per lookup, then a pure fold.</b> The documents are fully denormalized
/// (§16), so there is no reference cache, no second round trip and no staleness window on this side:
/// keeping the embedded master data fresh is replication's job. The cost that remains is the fold
/// itself, which runs per lookup (open item O9) — cheap for one model, and the reason a caller looping
/// over many models should cache its own results rather than this service growing a cache with an
/// invalidation problem.</para>
///
/// <para>The pipeline reads as three steps:</para>
/// <code>
/// read partition ─▶ ServiceMenuGenerationEvaluator (aggregate → the shared generator)
///                ─▶ ServiceMenuScheduleEvaluator   (group by variant, order by distance)
///                ─▶ ServiceMenuPricingEvaluator    (parts / labour / discount / total)
/// </code>
///
/// <para><b>Every live variant of the model is returned</b>, and the caller picks. There is no variant
/// filter because nothing outside the menus database holds a variant id — see
/// <see cref="ServiceMenuLookupRequest"/>.</para>
/// </summary>
public class ServiceMenuLookupService
{
    private readonly ServiceMenuCosmosService cosmosService;
    private readonly ServiceMenuGenerationEvaluator generationEvaluator;

    public ServiceMenuLookupService(ServiceMenuCosmosService cosmosService, ServiceMenuGenerationEvaluator generationEvaluator)
    {
        this.cosmosService = cosmosService;
        this.generationEvaluator = generationEvaluator;
    }

    /// <summary>
    /// The service menu for one basic model code, in one language, for one country.
    ///
    /// A model with no replicated menu returns a result with <c>NotFound = true</c> and no variants
    /// rather than null — "no menu" is an ordinary answer for a model nobody has authored one for, and
    /// callers should not have to null-check the common case.
    /// </summary>
    /// <exception cref="ServiceMenuContainerNotFoundException">The container has not been provisioned.</exception>
    /// <exception cref="ServiceMenuGenerationException">The documents reference master data they do not carry.</exception>
    public async Task<ServiceMenuLookupDTO> GetMenuAsync(ServiceMenuLookupRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var config = await generationEvaluator.ResolveConfigAsync(request);

        var result = new ServiceMenuLookupDTO
        {
            BasicModelCode = request.BasicModelCode?.Trim(),
            CountryID = config.CountryID,
            Language = config.Language,
            TransferRate = config.TransferRate,
            NotFound = true,
        };

        var documents = await cosmosService.GetMenuDocumentsAsync(request.BasicModelCode, cancellationToken);

        // NotFound describes the PARTITION, not the outcome: a model whose every variant is deleted, or
        // whose intervals have no labour details, has a menu that generates nothing — which is a
        // different thing from having no menu at all, and a UI renders the two differently.
        if (documents.IsEmpty)
            return result;

        // NotFound is answered by the partition, above — the free filter runs after it and can legitimately
        // leave Variants empty on a model that HAS a menu. Do not fold the two together.
        result.NotFound = false;
        result.Variants = ServiceMenuScheduleEvaluator.Evaluate(
            generationEvaluator.Evaluate(documents, config, request.FreeFilter));

        return result;
    }

    /// <summary>
    /// Convenience overload for the common call: one model code, one language, the configured default
    /// country.
    /// </summary>
    public Task<ServiceMenuLookupDTO> GetMenuAsync(string basicModelCode, string language = null, long? countryID = null, CancellationToken cancellationToken = default) =>
        GetMenuAsync(
            new ServiceMenuLookupRequest
            {
                BasicModelCode = basicModelCode,
                Language = language,
                CountryID = countryID,
            },
            cancellationToken);

    /// <summary>
    /// The generated lines before they are priced and grouped — every field that composed each menu and
    /// labour code, straight off the shared generator.
    ///
    /// For callers that need to re-compose or re-format codes themselves (a DMS integration, a
    /// diagnostic view) rather than render the customer-facing shape. Uses the same read and the same
    /// generator, so it cannot disagree with <see cref="GetMenuAsync(ServiceMenuLookupRequest, CancellationToken)"/>;
    /// it simply stops one step earlier. Dealer cost is absent here too — the generator is never asked
    /// for it.
    /// </summary>
    public async Task<IReadOnlyList<GeneratedMenuLine>> GetGeneratedLinesAsync(ServiceMenuLookupRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var config = await generationEvaluator.ResolveConfigAsync(request);
        var documents = await cosmosService.GetMenuDocumentsAsync(request.BasicModelCode, cancellationToken);

        return generationEvaluator.Evaluate(documents, config, request.FreeFilter);
    }
}
