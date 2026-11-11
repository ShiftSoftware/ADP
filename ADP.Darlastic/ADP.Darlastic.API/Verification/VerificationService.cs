using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Verification;

/// <summary>One verification attempt, as a host's own check reports it.</summary>
/// <param name="IdentityID">The golden customer; a merged customer's old number is followed to the live one.</param>
/// <param name="Kind">A configured kind.</param>
/// <param name="Value">What the claim is about (a phone, a VIN); normalized by the kind's settings.</param>
/// <param name="Method">The host's name for how it verified.</param>
/// <param name="Outcome"><see cref="VerificationOutcome"/>.</param>
/// <param name="Actor">Who did it (a user ID, a service name).</param>
public record VerificationReport(
    long IdentityID, string Kind, string? Value, string Method, string Outcome, string Actor,
    string? Location = null, string? SourceSystem = null, string? SourceRecordId = null, string? EvidenceReference = null);

/// <summary>What a report did.</summary>
public record VerificationResult(
    VerificationEntry Entry, VerifiedClaim? Opened, IReadOnlyList<VerifiedClaim> Ended);

/// <summary>A claim and whether it holds now.</summary>
public record ClaimState(VerifiedClaim Claim, bool Holds, DateTime? ExpiresUtc);

/// <summary>The answer to "is this customer verified for this context".</summary>
public record VerificationStatus(
    long IdentityID, string Context, bool Verified, IReadOnlyList<RequirementState> Requirements);

/// <param name="Missing">Why it is not met: "not-verified", "value-missing" or "value-invalid".</param>
public record RequirementState(VerificationRequirement Requirement, bool Met, string? Missing, VerifiedClaim? Claim);

/// <summary>Why a report or a question was refused.</summary>
public enum VerificationProblem
{
    UnknownIdentity,
    UnknownKind,
    UnknownOutcome,
    MethodMissing,
    MethodNotAllowed,
    ValueMissing,
    ValueInvalid,
    TooLong,
}

public sealed class VerificationException(VerificationProblem problem, string message) : Exception(message)
{
    public VerificationProblem Problem { get; } = problem;
}

/// <summary>
/// Records verification results and answers what is verified. The one writer of the verification tables: a
/// host's own check (a code, a document reader, a rule, a tick box) calls <see cref="ReportAsync"/> when it has
/// an outcome; nothing here knows how the check was done.
///
/// <para>A verified report opens a claim. For an exclusive kind it first ends the open claim any other customer
/// holds on the same value; for the same customer it ends the previous period and starts a new one. The log row,
/// the ended claims and the new claim are saved together. Two reports racing for one exclusive value: the
/// database lets one open claim exist, the other report reads again and supersedes it.</para>
///
/// <para>A customer's claims include those recorded on the numbers merged into them: a claim keeps the number
/// it was recorded on and is read through the merge, like every other read of the registry.</para>
/// </summary>
public sealed class VerificationService
{
    private const int MaxValueKey = 128, MaxMethod = 64, MaxActor = 128, MaxLocation = 128, MaxSource = 64, MaxEvidence = 256;

    private readonly ShiftDbContext db;
    private readonly DarlasticVerificationOptions options;
    private readonly TimeProvider time;

    public VerificationService(ShiftDbContext db, IOptions<DarlasticApiOptions> options, TimeProvider? time = null)
    {
        this.db = db;
        this.options = options.Value.Verification;
        this.time = time ?? TimeProvider.System;
    }

    /// <summary>The host turned verification on (and so has the tables).</summary>
    public bool Enabled => options.Enabled;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private void RequireEnabled()
    {
        if (!options.Enabled)
            throw new InvalidOperationException(
                "Verification is off for this host (DarlasticApiOptions.Verification.Enabled). Turning it on adds two tables to the host's model.");
    }

    /// <summary>The live customer a number stands for now (itself, or the one it was merged into); null when no
    /// such customer exists.</summary>
    public async Task<long?> LiveIdentityAsync(long identityId, CancellationToken ct = default)
    {
        long live = identityId;
        for (int hop = 0; hop < 64; hop++)
        {
            var next = await db.Set<IdentityRedirect>().AsNoTracking()
                .Where(x => x.OldIdentityID == live)
                .Select(x => (long?)x.NewIdentityID)
                .FirstOrDefaultAsync(ct);
            if (next is null) break;
            live = next.Value;
        }
        return await db.Set<GoldenIdentity>().AsNoTracking().AnyAsync(x => x.IdentityID == live, ct) ? live : null;
    }

    /// <summary>The live number and every number merged into it.</summary>
    private async Task<List<long>> FamilyAsync(long live, CancellationToken ct)
    {
        var family = await db.Set<IdentityRedirect>().AsNoTracking()
            .Where(x => x.NewIdentityID == live)
            .Select(x => x.OldIdentityID)
            .ToListAsync(ct);
        family.Add(live);
        return family;
    }

    /// <summary>
    /// The stored key of a value for a kind: normalized by the kind's settings, hashed when the kind says so.
    /// Empty for a kind that holds no value. Throws when the kind needs a value and this is not one.
    /// </summary>
    public string ValueKey(string kind, string? value)
    {
        var settings = KindOrThrow(kind);
        var trimmed = value?.Trim() ?? "";

        if (trimmed.Length == 0)
            return settings.ValueRequired
                ? throw new VerificationException(VerificationProblem.ValueMissing, $"A '{kind}' verification needs a value.")
                : "";

        if (settings.HashValue && string.IsNullOrEmpty(options.ValueHashKey))
            return "";   // the host keeps no key for this kind's values: nothing is stored

        var key = settings.Normalize is null ? trimmed : settings.Normalize(trimmed);
        if (string.IsNullOrEmpty(key))
            throw new VerificationException(VerificationProblem.ValueInvalid, $"That is not a value a '{kind}' verification can be about.");

        if (settings.HashValue)
            key = Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(options.ValueHashKey!), Encoding.UTF8.GetBytes(kind.ToLowerInvariant() + "\n" + key)));

        if (key.Length > MaxValueKey)
            throw new VerificationException(VerificationProblem.TooLong, $"A '{kind}' value is at most {MaxValueKey} characters.");
        return key;
    }

    private VerificationKindOptions KindOrThrow(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) && options.Kinds.TryGetValue(kind.Trim(), out var settings)
            ? settings
            : throw new VerificationException(VerificationProblem.UnknownKind, $"Unknown verification kind '{kind}'.");

    /// <summary>
    /// Record one verification attempt. <c>verified</c> opens a claim (ending the open claim another customer
    /// holds on an exclusive value, or this customer's previous period); <c>revoked</c> ends this customer's
    /// open claim; <c>failed</c> is logged and changes nothing. Saves on the host's context, together.
    /// </summary>
    public async Task<VerificationResult> ReportAsync(VerificationReport report, CancellationToken ct = default)
    {
        RequireEnabled();

        var settings = KindOrThrow(report.Kind);
        var kind = report.Kind.Trim().ToLowerInvariant();

        if (!VerificationOutcome.IsKnown(report.Outcome))
            throw new VerificationException(VerificationProblem.UnknownOutcome, $"Unknown outcome '{report.Outcome}'.");

        var method = report.Method?.Trim() ?? "";
        if (method.Length == 0)
            throw new VerificationException(VerificationProblem.MethodMissing, "A verification report names its method.");
        if (settings.Methods.Count > 0 && !settings.Methods.Contains(method))
            throw new VerificationException(VerificationProblem.MethodNotAllowed, $"'{method}' is not a method this host accepts for '{kind}'.");

        var actor = Fit(report.Actor, MaxActor, "actor") ?? "unknown";
        Fit(method, MaxMethod, "method");
        var location = Fit(report.Location, MaxLocation, "location");
        var sourceSystem = Fit(report.SourceSystem, MaxSource, "source system");
        var sourceRecordId = Fit(report.SourceRecordId, MaxSource, "source record ID");
        var evidence = Fit(report.EvidenceReference, MaxEvidence, "evidence reference");

        var key = ValueKey(kind, report.Value);

        var live = await LiveIdentityAsync(report.IdentityID, ct)
            ?? throw new VerificationException(VerificationProblem.UnknownIdentity, "No such customer.");
        var family = await FamilyAsync(live, ct);

        for (int attempt = 0; ; attempt++)
        {
            var now = Now;
            var entry = new VerificationEntry
            {
                AtUtc = now,
                Actor = actor,
                IdentityID = live,
                Kind = kind,
                ValueKey = key,
                Method = method,
                Outcome = report.Outcome,
                Location = location,
                SourceSystem = sourceSystem,
                SourceRecordId = sourceRecordId,
                EvidenceReference = evidence,
            };
            db.Add(entry);

            // The open claims this report ends: this customer's own on the value, and, when the report verifies
            // an exclusive value, whoever else holds it.
            var open = db.Set<VerifiedClaim>().Where(c => c.EndUtc == null && c.Kind == kind && c.ValueKey == key);
            bool takesFromOthers = report.Outcome == VerificationOutcome.Verified && settings.Exclusive;
            var ending = report.Outcome == VerificationOutcome.Failed
                ? []
                : await (takesFromOthers ? open : open.Where(c => family.Contains(c.IdentityID))).ToListAsync(ct);

            foreach (var claim in ending)
            {
                claim.EndUtc = now;
                claim.EndedBy = entry;
                claim.EndReason = report.Outcome == VerificationOutcome.Revoked ? VerificationEndReason.Revoked
                    : family.Contains(claim.IdentityID) ? VerificationEndReason.Renewed
                    : VerificationEndReason.Superseded;
            }

            VerifiedClaim? opened = null;
            if (report.Outcome == VerificationOutcome.Verified)
            {
                opened = new VerifiedClaim
                {
                    IdentityID = live,
                    Kind = kind,
                    ValueKey = key,
                    Exclusive = settings.Exclusive,
                    Method = method,
                    StartUtc = now,
                    StartedBy = entry,
                    Location = location,
                    SourceSystem = sourceSystem,
                    SourceRecordId = sourceRecordId,
                };
                db.Add(opened);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                return new VerificationResult(entry, opened, ending);
            }
            catch (DbUpdateException) when (attempt < 3 && opened is not null)
            {
                // Another report opened a claim on this value between the read and the save (the unique index
                // refused this one). Forget this attempt and read again: the other claim is then the one to end.
                db.Entry(entry).State = EntityState.Detached;
                db.Entry(opened).State = EntityState.Detached;
                foreach (var claim in ending) db.Entry(claim).State = EntityState.Detached;
            }
        }
    }

    /// <summary>A customer's claims, newest first: those that hold, and with <paramref name="history"/> the
    /// ended and expired ones too. Includes claims recorded on numbers merged into this customer.</summary>
    public async Task<IReadOnlyList<ClaimState>> ClaimsAsync(long identityId, bool history = false, CancellationToken ct = default)
    {
        RequireEnabled();
        if (await LiveIdentityAsync(identityId, ct) is not { } live)
            throw new VerificationException(VerificationProblem.UnknownIdentity, "No such customer.");
        return await ClaimsOfFamilyAsync(await FamilyAsync(live, ct), history, ct);
    }

    private async Task<IReadOnlyList<ClaimState>> ClaimsOfFamilyAsync(List<long> family, bool history, CancellationToken ct)
    {
        var query = db.Set<VerifiedClaim>().AsNoTracking().Where(c => family.Contains(c.IdentityID));
        if (!history) query = query.Where(c => c.EndUtc == null);

        var now = Now;
        var claims = (await query.ToListAsync(ct))
            .OrderByDescending(c => c.StartUtc).ThenByDescending(c => c.ClaimID)
            .Select(c => State(c, now));
        return [.. history ? claims : claims.Where(c => c.Holds)];
    }

    private ClaimState State(VerifiedClaim claim, DateTime now)
    {
        DateTime? expires = options.Kinds.TryGetValue(claim.Kind, out var settings) && settings.Validity is { } validity
            ? claim.StartUtc + validity
            : null;
        return new(claim, claim.EndUtc is null && (expires is null || expires > now), expires);
    }

    /// <summary>
    /// Whether the customer is verified under the rule of <paramref name="context"/> (the host's: a service
    /// visit, a sale; null for the default rule). <paramref name="values"/> holds the visit's own value per
    /// kind, for requirements that ask for a specific one.
    /// </summary>
    public async Task<VerificationStatus> StatusAsync(long identityId, string? context = null,
        IReadOnlyDictionary<string, string>? values = null, CancellationToken ct = default)
    {
        RequireEnabled();
        if (await LiveIdentityAsync(identityId, ct) is not { } live)
            throw new VerificationException(VerificationProblem.UnknownIdentity, "No such customer.");

        var holding = (await ClaimsOfFamilyAsync(await FamilyAsync(live, ct), history: false, ct)).Select(c => c.Claim).ToList();
        var visit = values is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

        var states = new List<RequirementState>();
        foreach (var requirement in options.RuleFor(context))
        {
            var ofKind = holding.Where(c => string.Equals(c.Kind, requirement.Kind, StringComparison.OrdinalIgnoreCase));
            if (!requirement.SpecificValue)
            {
                var any = ofKind.FirstOrDefault();
                states.Add(new(requirement, any is not null, any is null ? "not-verified" : null, any));
                continue;
            }

            if (!visit.TryGetValue(requirement.Kind, out var value) || string.IsNullOrWhiteSpace(value))
            {
                states.Add(new(requirement, false, "value-missing", null));
                continue;
            }

            string key;
            try { key = ValueKey(requirement.Kind, value); }
            catch (VerificationException) { states.Add(new(requirement, false, "value-invalid", null)); continue; }

            var match = ofKind.FirstOrDefault(c => string.Equals(c.ValueKey, key, StringComparison.Ordinal));
            states.Add(new(requirement, match is not null, match is null ? "not-verified" : null, match));
        }

        // A rule that asks for nothing verifies nobody: an emptied configuration must not read as "all verified".
        return new(live, context?.Trim() ?? "", states.Count > 0 && states.All(s => s.Met), states);
    }

    private static string? Fit(string? text, int max, string what)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return trimmed.Length <= max
            ? trimmed
            : throw new VerificationException(VerificationProblem.TooLong, $"The {what} is at most {max} characters.");
    }
}
