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
/// over many models should use <see cref="GetMenusAsync"/> or cache its own results rather than this
/// service growing a cache with an invalidation problem.</para>
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
    private readonly IServiceMenuLookupStorageService storageService;
    private readonly ServiceMenuGenerationEvaluator generationEvaluator;

    /// <param name="storageService">
    /// Where the documents come from — Cosmos in a live deployment, DuckDB over a synced snapshot.
    /// Everything above the read is storage-agnostic: the fold, the pricing and the NotFound semantics
    /// cannot differ between backends because they never see which one answered.
    /// </param>
    public ServiceMenuLookupService(IServiceMenuLookupStorageService storageService, ServiceMenuGenerationEvaluator generationEvaluator)
    {
        this.storageService = storageService;
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

        var documents = await storageService.GetMenuDocumentsAsync(request.BasicModelCode, cancellationToken);

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
        var documents = await storageService.GetMenuDocumentsAsync(request.BasicModelCode, cancellationToken);

        return generationEvaluator.Evaluate(documents, config, request.FreeFilter);
    }

    /// <summary>
    /// The bulk lookup: service menus for many basic model codes under ONE shared configuration — the
    /// menu counterpart of the vehicle lookup's multi-VIN <c>LookupAsync</c>, and like it a
    /// <b>DuckDB-storage flow</b>: the Cosmos storage does not implement the bulk read and throws
    /// <see cref="NotImplementedException"/>. A Cosmos-backed caller loops the single lookup instead.
    ///
    /// <para>Two costs collapse against calling <see cref="GetMenuAsync(ServiceMenuLookupRequest, CancellationToken)"/>
    /// in a loop: the config (country, transfer rate, labour-rate mode) resolves once instead of per
    /// model, and the storage read is one bulk call, which the DuckDB backend answers with a handful of
    /// <c>IN</c>-clause queries for the whole set. The fold still runs per model — it is the same fold,
    /// so a bulk result for a code cannot differ from a single lookup of that code.</para>
    ///
    /// <para>Results come back one per DISTINCT code (after trimming), in first-appearance order, each
    /// with the same NotFound semantics as the single lookup — so the answer for a code is always at the
    /// position its first occurrence held. Codes that are null or whitespace are dropped.</para>
    ///
    /// <para><see cref="ServiceMenuGenerationException"/> aborts the whole call rather than skipping the
    /// broken model: silently returning N−1 menus would make an incomplete replication look like a
    /// smaller catalog. A caller that wants per-model containment loops the single lookup, or goes
    /// through the vehicle lookup, whose menu section already contains these faults per vehicle.</para>
    /// </summary>
    /// <param name="sharedRequest">
    /// The configuration every code generates under: country, language, transfer rate and free filter.
    /// Its <see cref="ServiceMenuLookupRequest.BasicModelCode"/> is ignored — the codes come from
    /// <paramref name="basicModelCodes"/>. Null means all defaults, exactly like the single lookup.
    /// </param>
    public async Task<IReadOnlyList<ServiceMenuLookupDTO>> GetMenusAsync(
        IEnumerable<string> basicModelCodes,
        ServiceMenuLookupRequest sharedRequest = null,
        CancellationToken cancellationToken = default)
    {
        var config = await generationEvaluator.ResolveConfigAsync(sharedRequest);
        var results = new List<ServiceMenuLookupDTO>();

        if (basicModelCodes is null)
            return results;

        var documentSets = await storageService.GetMenuDocumentsAsync(basicModelCodes, cancellationToken);

        foreach (var documents in documentSets)
        {
            var result = new ServiceMenuLookupDTO
            {
                BasicModelCode = documents.BasicModelCode,
                CountryID = config.CountryID,
                Language = config.Language,
                TransferRate = config.TransferRate,
                NotFound = true,
            };

            // The same two-step answer as the single lookup: NotFound comes from the stored documents,
            // and the free filter runs after it — a model whose menu the filter empties is still Found.
            if (!documents.IsEmpty)
            {
                result.NotFound = false;
                result.Variants = ServiceMenuScheduleEvaluator.Evaluate(
                    generationEvaluator.Evaluate(documents, config, sharedRequest?.FreeFilter ?? ServiceMenuFreeFilter.All));
            }

            results.Add(result);
        }

        return results;
    }
}
