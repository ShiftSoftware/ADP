using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

/// <summary>
/// Runs the whole request — the action and the writing of its result — under the match configuration
/// the registry's latest resolve recorded (<see cref="MatchSettings"/>), for controllers that re-score
/// staged pairs at request time.
///
/// <para><b>Why.</b> A pair's score, trace and flags are recomputed per request from its two staged
/// records, but the engine's per-tenant rules are run configuration: the batch runner switches them on
/// in its own process before it resolves, and computes one of them (the attested name parts) from the
/// very records it resolves. This host never saw that configuration. Scoring with the host's own
/// explained pairs with rules the resolve did not use, so the served decision contradicted the staged
/// one and the tenant's own rules never appeared in an explanation.</para>
///
/// <para><b>How.</b> The configuration is read through the singleton <see cref="RecordedMatchSettings"/>
/// (once per finished resolve, one cheap probe per request) and applied with
/// <see cref="MatchSettings.Use"/>: a scope on this request's flow, so every engine read — score,
/// trace, flags, block keys, name survivorship, categories — sees it, and nothing process-wide
/// changes; concurrent requests cannot race on it. A registry that recorded no configuration (a tenant
/// that only ever resolved on the engine defaults, or a registry from before configurations were
/// recorded) leaves the request on the process configuration, exactly as before.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
internal sealed class RecordedMatchSettingsAttribute : Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<ShiftDbContext>();
        var settings = await services.GetRequiredService<RecordedMatchSettings>().GetAsync(
            ct => LatestFinishedRunAsync(db, ct),
            ct => ReadRecordedAsync(db, ct),
            context.HttpContext.RequestAborted);

        if (settings is null)
        {
            await next();
            return;
        }
        using (settings.Use())
            await next();
    }

    private static async Task<int> LatestFinishedRunAsync(ShiftDbContext db, CancellationToken ct) =>
        await db.Set<ResolveRun>().AsNoTracking()
            .Where(r => r.FinishedUtc != null)
            .MaxAsync(r => (int?)r.RunID, ct) ?? 0;

    private static async Task<(string? ContentHash, string? Payload)> ReadRecordedAsync(ShiftDbContext db, CancellationToken ct)
    {
        // A key seek on ProjectionState's clustered primary key (ArtifactType, ArtifactKey).
        var row = await db.Set<ProjectionState>().AsNoTracking()
            .Where(p => p.ArtifactType == MatchSettings.ArtifactType && p.ArtifactKey == MatchSettings.ArtifactKey)
            .Select(p => new { p.ContentHash, p.Payload })
            .FirstOrDefaultAsync(ct);
        return (row?.ContentHash, row?.Payload);
    }
}
