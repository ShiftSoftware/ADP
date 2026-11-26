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
/// <param name="Value">What the claim is about (a phone, a VIN, a document number); normalized by the kind's settings.</param>
/// <param name="Method">The host's name for how it verified.</param>
/// <param name="Outcome"><see cref="VerificationOutcome"/>: verified, submitted, failed or revoked.</param>
/// <param name="Actor">Who did it (a user ID, a service name).</param>
/// <param name="ValueType">The value's type, for a kind whose values come in types (a document: national ID, passport ...).</param>
public record VerificationReport(
    long IdentityID, string Kind, string? Value, string Method, string Outcome, string Actor,
    string? Location = null, string? SourceSystem = null, string? SourceRecordId = null, string? EvidenceReference = null,
    string? ValueType = null);

/// <summary>What a report did.</summary>
/// <param name="Opened">The claim the report opened: verified, submitted, or held (see its level).</param>
/// <param name="ConflictsWith">Held only: the other customer's claim on the same value.</param>
/// <param name="Unchanged">A report that changed nothing because the customer already has an open claim on the
/// value that it must not replace (a submitted report on a value already verified for them, a second attempt
/// behind another customer's claim): that claim.</param>
public record VerificationResult(
    VerificationEntry Entry, VerifiedClaim? Opened, IReadOnlyList<VerifiedClaim> Ended,
    VerifiedClaim? ConflictsWith = null, VerifiedClaim? Unchanged = null);

/// <summary>A claim and where it stands now.</summary>
/// <param name="Holds">Verified, not ended and not expired.</param>
/// <param name="Pending">Open below the verified level: waiting for a review.</param>
public record ClaimState(VerifiedClaim Claim, bool Holds, DateTime? ExpiresUtc, bool Pending = false);

/// <summary>The answer to "is this customer verified for this context".</summary>
public record VerificationStatus(
    long IdentityID, string Context, bool Verified, IReadOnlyList<RequirementState> Requirements);

/// <param name="Missing">Why it is not met: "not-verified", "pending-review" (a claim is open below the level the
/// rule asks for), "value-missing" or "value-invalid".</param>
/// <param name="Claim">The strongest open claim for the requirement, met or not (never a held one).</param>
public record RequirementState(VerificationRequirement Requirement, bool Met, string? Missing, VerifiedClaim? Claim);

/// <summary>One claim waiting for a review.</summary>
/// <param name="Submitted">The report that opened it.</param>
/// <param name="LiveIdentityID">The customer it stands for now (after merges); null when that customer is gone.</param>
/// <param name="ConflictsWith">Held only: the other claim on the same value, when it still exists.</param>
/// <param name="ConflictLiveIdentityID">The customer that claim stands for now. Equal to
/// <paramref name="LiveIdentityID"/> once the two customers were merged: the conflict is then settled.</param>
public record ReviewItem(
    VerifiedClaim Claim, VerificationEntry Submitted, long? LiveIdentityID,
    VerifiedClaim? ConflictsWith, long? ConflictLiveIdentityID);

/// <summary>Why a report, a review or a question was refused.</summary>
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

    /// <summary>The kind's values come in types and the report names none, or one the host does not configure.</summary>
    ValueTypeInvalid,

    /// <summary>The method never proves the claim by itself: report it as submitted.</summary>
    MethodNeedsReview,

    /// <summary>The kind's values belong to one customer for good, but the host keeps no key to tell them apart.</summary>
    NotConfigured,

    /// <summary>No such claim, or it is not open, or (review) it is verified already.</summary>
    UnknownClaim,

    /// <summary>The reviewer is the person who reported the claim, and the host does not allow that.</summary>
    ReviewBySubmitter,

    /// <summary>Another customer still holds the value: merge the two customers or end the other claim first.</summary>
    ConflictUnresolved,
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
/// <para><b>Levels.</b> A report under a method that does not prove the claim by itself arrives as
/// <c>submitted</c>: its claim is open below the verified level and waits for <see cref="ReviewAsync"/>, which
/// approves it (the submitted claim ends as promoted and a verified one opens) or rejects it. A rule says which
/// level a context accepts.</para>
///
/// <para><b>Values that belong to one customer for good</b> (<see cref="VerificationKindOptions.OneHolder"/>: an
/// identity document). A report on a value another customer holds ends nothing: it is held, with the other
/// claim noted, and waits for a review. Approving it is refused until the other customer no longer holds the
/// value (the two were merged, or the other claim was revoked or rejected).</para>
///
/// <para>A customer's claims include those recorded on the numbers merged into them: a claim keeps the number
/// it was recorded on and is read through the merge, like every other read of the registry.</para>
/// </summary>
public sealed class VerificationService
{
    private const int MaxValueKey = 128, MaxMethod = 64, MaxActor = 128, MaxLocation = 128, MaxSource = 64, MaxEvidence = 256,
        MaxValueType = 32, MaxNote = 512;

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
    /// The stored type of a value for a kind: lower-cased, one of the kind's types. Empty for a kind without
    /// types, and for a report without a value. Throws when the kind has types and this is not one of them.
    /// </summary>
    public string ValueType(string kind, string? value, string? valueType)
    {
        var settings = KindOrThrow(kind);
        var type = valueType?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(value))
            return "";
        if (settings.ValueTypes.Count == 0)
            return type.Length == 0
                ? ""
                : throw new VerificationException(VerificationProblem.ValueTypeInvalid, $"A '{kind}' value has no type on this host.");
        if (type.Length == 0 || type.Length > MaxValueType || !settings.ValueTypes.Contains(type))
            throw new VerificationException(VerificationProblem.ValueTypeInvalid,
                type.Length == 0 ? $"A '{kind}' value names its type." : $"'{type}' is not a type of '{kind}' value this host accepts.");
        return type;
    }

    /// <summary>
    /// The stored key of a value for a kind: normalized by the kind's settings, hashed when the kind says so
    /// (with its type, when it has one). Empty for a kind that holds no value. Throws when the kind needs a
    /// value and this is not one.
    /// </summary>
    public string ValueKey(string kind, string? value, string? valueType = null)
    {
        var settings = KindOrThrow(kind);
        var trimmed = value?.Trim() ?? "";

        if (trimmed.Length == 0)
            return settings.ValueRequired
                ? throw new VerificationException(VerificationProblem.ValueMissing, $"A '{kind}' verification needs a value.")
                : "";

        var type = ValueType(kind, trimmed, valueType);

        if (settings.HashValue && string.IsNullOrEmpty(options.ValueHashKey))
            return settings.OneHolder
                // Without a key the module could not tell that two customers gave the same value.
                ? throw new VerificationException(VerificationProblem.NotConfigured,
                    $"A '{kind}' value belongs to one customer, and this host keeps no key to tell values apart (Verification.ValueHashKey).")
                : "";   // the host keeps no key for this kind's values: nothing is stored

        var key = settings.Normalize is null ? trimmed : settings.Normalize(trimmed);
        if (string.IsNullOrEmpty(key))
            throw new VerificationException(VerificationProblem.ValueInvalid, $"That is not a value a '{kind}' verification can be about.");

        if (settings.HashValue)
            key = Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(options.ValueHashKey!),
                Encoding.UTF8.GetBytes(kind.Trim().ToLowerInvariant() + "\n" + (type.Length == 0 ? "" : type + "\n") + key)));

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
    /// holds on an exclusive value, or this customer's previous period); <c>submitted</c> opens one that waits
    /// for a review; either is held instead when the value belongs to one customer for good and another has it;
    /// <c>revoked</c> ends this customer's open claim; <c>failed</c> is logged and changes nothing. Saves on the
    /// host's context, together.
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
        if (report.Outcome == VerificationOutcome.Verified && settings.ReviewedMethods.Contains(method))
            throw new VerificationException(VerificationProblem.MethodNeedsReview,
                $"'{method}' does not verify a '{kind}' by itself on this host: report it as submitted, for review.");

        var actor = Fit(report.Actor, MaxActor, "actor") ?? "unknown";
        Fit(method, MaxMethod, "method");
        var location = Fit(report.Location, MaxLocation, "location");
        var sourceSystem = Fit(report.SourceSystem, MaxSource, "source system");
        var sourceRecordId = Fit(report.SourceRecordId, MaxSource, "source record ID");
        var evidence = Fit(report.EvidenceReference, MaxEvidence, "evidence reference");

        var key = ValueKey(kind, report.Value, report.ValueType);
        var type = key.Length == 0 ? "" : ValueType(kind, report.Value, report.ValueType);
        // One customer for good only where the module holds something to compare.
        bool oneHolder = settings.OneHolder && key.Length > 0;

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
                ValueType = type,
                Method = method,
                Outcome = report.Outcome,
                Location = location,
                SourceSystem = sourceSystem,
                SourceRecordId = sourceRecordId,
                EvidenceReference = evidence,
            };
            db.Add(entry);

            var ending = new List<VerifiedClaim>();
            VerifiedClaim? opened = null, conflict = null, unchanged = null;

            if (report.Outcome != VerificationOutcome.Failed)
            {
                var open = await db.Set<VerifiedClaim>()
                    .Where(c => c.EndUtc == null && c.Kind == kind && c.ValueType == type && c.ValueKey == key)
                    .ToListAsync(ct);
                var mine = open.Where(c => family.Contains(c.IdentityID)).ToList();
                var others = open.Where(c => !family.Contains(c.IdentityID)).ToList();

                if (report.Outcome == VerificationOutcome.Revoked)
                {
                    ending.AddRange(mine);
                }
                else if (oneHolder && Holder(others) is { } holder)
                {
                    // Another customer has this value, and it can be one customer's only: nothing of theirs
                    // ends. This attempt waits for a review, which sees both.
                    entry.Outcome = VerificationOutcome.Held;
                    conflict = holder;
                    unchanged = mine.OrderByDescending(c => VerificationLevel.Rank(c.Level)).FirstOrDefault();
                    if (unchanged is null)
                    {
                        opened = NewClaim(VerificationLevel.Held, exclusive: false);
                        opened.ConflictClaimID = holder.ClaimID;
                    }
                }
                else if (report.Outcome == VerificationOutcome.Submitted)
                {
                    // Never a step down: a value already verified for the customer stays verified.
                    unchanged = mine.FirstOrDefault(c => c.Level == VerificationLevel.Verified);
                    if (unchanged is null)
                    {
                        ending.AddRange(mine);
                        opened = NewClaim(VerificationLevel.Submitted, exclusive: oneHolder);
                    }
                }
                else
                {
                    ending.AddRange(mine);
                    if (settings.Exclusive && !oneHolder) ending.AddRange(others);
                    opened = NewClaim(VerificationLevel.Verified, exclusive: settings.Exclusive || oneHolder);
                }
            }

            foreach (var claim in ending)
            {
                claim.EndUtc = now;
                claim.EndedBy = entry;
                claim.EndReason = report.Outcome == VerificationOutcome.Revoked ? VerificationEndReason.Revoked
                    : family.Contains(claim.IdentityID) ? VerificationEndReason.Renewed
                    : VerificationEndReason.Superseded;
            }
            if (opened is not null) db.Add(opened);

            try
            {
                await db.SaveChangesAsync(ct);
                return new VerificationResult(entry, opened, ending, conflict, unchanged);
            }
            catch (DbUpdateException) when (attempt < 3 && opened is not null)
            {
                // Another report opened a claim on this value between the read and the save (the unique index
                // refused this one). Forget this attempt and read again: the other claim is then the one to end,
                // or, for a value that is one customer's only, the one this attempt is held behind.
                db.Entry(entry).State = EntityState.Detached;
                db.Entry(opened).State = EntityState.Detached;
                foreach (var claim in ending) db.Entry(claim).State = EntityState.Detached;
            }

            VerifiedClaim NewClaim(string level, bool exclusive) => new()
            {
                IdentityID = live,
                Kind = kind,
                ValueKey = key,
                ValueType = type,
                Level = level,
                Exclusive = exclusive,
                Method = method,
                StartUtc = now,
                StartedBy = entry,
                Location = location,
                SourceSystem = sourceSystem,
                SourceRecordId = sourceRecordId,
            };
        }
    }

    /// <summary>The claim that makes a value taken: another customer's verified one, else their submitted one.
    /// A held claim takes nothing.</summary>
    private static VerifiedClaim? Holder(IEnumerable<VerifiedClaim> others) =>
        others.Where(c => c.Level != VerificationLevel.Held)
            .OrderByDescending(c => VerificationLevel.Rank(c.Level)).ThenBy(c => c.StartUtc).ThenBy(c => c.ClaimID)
            .FirstOrDefault();

    /// <summary>End one open claim by its number (any level), as a revocation. For a customer with several
    /// claims of a kind (several documents), where naming the value again is not possible.</summary>
    public async Task<VerificationResult> RevokeClaimAsync(long claimId, string actor, CancellationToken ct = default)
    {
        RequireEnabled();
        var claim = await db.Set<VerifiedClaim>().FirstOrDefaultAsync(c => c.ClaimID == claimId && c.EndUtc == null, ct)
            ?? throw new VerificationException(VerificationProblem.UnknownClaim, "No such open claim.");

        var now = Now;
        var entry = new VerificationEntry
        {
            AtUtc = now,
            Actor = Fit(actor, MaxActor, "actor") ?? "unknown",
            IdentityID = await LiveIdentityAsync(claim.IdentityID, ct) ?? claim.IdentityID,
            Kind = claim.Kind,
            ValueKey = claim.ValueKey,
            ValueType = claim.ValueType,
            Method = claim.Method,
            Outcome = VerificationOutcome.Revoked,
        };
        db.Add(entry);
        claim.EndUtc = now;
        claim.EndedBy = entry;
        claim.EndReason = VerificationEndReason.Revoked;
        await db.SaveChangesAsync(ct);
        return new VerificationResult(entry, null, [claim]);
    }

    /// <summary>
    /// Decide a claim that waits for a review. Approving ends it as promoted and opens a verified claim for the
    /// customer (with everything a verified report does: the customer's other period on the value ends, and for
    /// an exclusive kind any other customer's). Rejecting ends it. The log row is the decision: its actor is the
    /// reviewer. Approving a claim on a value that is one customer's only is refused while another customer
    /// still holds it.
    /// </summary>
    public async Task<VerificationResult> ReviewAsync(long claimId, bool approve, string actor, string? note = null, CancellationToken ct = default)
    {
        RequireEnabled();
        var claim = await db.Set<VerifiedClaim>().Include(c => c.StartedBy)
            .FirstOrDefaultAsync(c => c.ClaimID == claimId && c.EndUtc == null && c.Level != VerificationLevel.Verified, ct)
            ?? throw new VerificationException(VerificationProblem.UnknownClaim, "No such claim is waiting for a review.");

        var reviewer = Fit(actor, MaxActor, "actor") ?? "unknown";
        if (!options.AllowReviewBySubmitter && string.Equals(reviewer, claim.StartedBy.Actor, StringComparison.Ordinal))
            throw new VerificationException(VerificationProblem.ReviewBySubmitter, "A claim is reviewed by someone other than the person who reported it.");
        var words = Fit(note, MaxNote, "note");

        var live = await LiveIdentityAsync(claim.IdentityID, ct)
            ?? throw new VerificationException(VerificationProblem.UnknownIdentity, "The claim's customer no longer exists.");
        var family = await FamilyAsync(live, ct);
        options.Kinds.TryGetValue(claim.Kind, out var settings);
        bool oneHolder = settings?.OneHolder == true && claim.ValueKey.Length > 0;

        var now = Now;
        var entry = new VerificationEntry
        {
            AtUtc = now,
            Actor = reviewer,
            IdentityID = live,
            Kind = claim.Kind,
            ValueKey = claim.ValueKey,
            ValueType = claim.ValueType,
            Method = claim.Method,
            Outcome = approve ? VerificationOutcome.Verified : VerificationOutcome.Rejected,
            Location = claim.Location,
            SourceSystem = claim.SourceSystem,
            SourceRecordId = claim.SourceRecordId,
            EvidenceReference = claim.StartedBy.EvidenceReference,
            ReviewOfClaimID = claim.ClaimID,
            Note = words,
        };

        if (!approve)
        {
            db.Add(entry);
            claim.EndUtc = now;
            claim.EndedBy = entry;
            claim.EndReason = VerificationEndReason.Rejected;
            await db.SaveChangesAsync(ct);
            return new VerificationResult(entry, null, [claim]);
        }

        var open = await db.Set<VerifiedClaim>()
            .Where(c => c.EndUtc == null && c.Kind == claim.Kind && c.ValueType == claim.ValueType && c.ValueKey == claim.ValueKey)
            .ToListAsync(ct);
        var others = open.Where(c => !family.Contains(c.IdentityID)).ToList();
        if (oneHolder && Holder(others) is not null)
            throw new VerificationException(VerificationProblem.ConflictUnresolved,
                "Another customer still holds this value. Merge the two customers, or end the other claim, before approving.");

        db.Add(entry);
        var ending = open.Where(c => family.Contains(c.IdentityID)).ToList();
        if (settings?.Exclusive == true && !oneHolder) ending.AddRange(others);
        foreach (var ended in ending)
        {
            ended.EndUtc = now;
            ended.EndedBy = entry;
            ended.EndReason = ended.ClaimID == claim.ClaimID ? VerificationEndReason.Promoted
                : family.Contains(ended.IdentityID) ? VerificationEndReason.Renewed
                : VerificationEndReason.Superseded;
        }

        var opened = new VerifiedClaim
        {
            IdentityID = live,
            Kind = claim.Kind,
            ValueKey = claim.ValueKey,
            ValueType = claim.ValueType,
            Level = VerificationLevel.Verified,
            Exclusive = settings?.Exclusive == true || oneHolder,
            Method = claim.Method,
            StartUtc = now,
            StartedBy = entry,
            Location = claim.Location,
            SourceSystem = claim.SourceSystem,
            SourceRecordId = claim.SourceRecordId,
        };
        db.Add(opened);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another customer's claim on the value opened between the read and the save.
            throw new VerificationException(VerificationProblem.ConflictUnresolved,
                "The value was claimed for another customer just now. Look at the queue again.");
        }
        return new VerificationResult(entry, opened, ending);
    }

    /// <summary>The claims that wait for a review, oldest first, and how many there are in all.</summary>
    public async Task<(IReadOnlyList<ReviewItem> Items, int Total)> AwaitingReviewAsync(int skip = 0, int top = 50, CancellationToken ct = default)
    {
        RequireEnabled();
        var waiting = db.Set<VerifiedClaim>().AsNoTracking().Where(c => c.EndUtc == null && c.Level != VerificationLevel.Verified);
        int total = await waiting.CountAsync(ct);
        var claims = await waiting.Include(c => c.StartedBy)
            .OrderBy(c => c.StartUtc).ThenBy(c => c.ClaimID)
            .Skip(Math.Max(skip, 0)).Take(Math.Clamp(top, 1, 200))
            .ToListAsync(ct);

        var conflictIds = claims.Where(c => c.ConflictClaimID is not null).Select(c => c.ConflictClaimID!.Value).Distinct().ToList();
        var conflicts = conflictIds.Count == 0
            ? []
            : await db.Set<VerifiedClaim>().AsNoTracking().Where(c => conflictIds.Contains(c.ClaimID)).ToDictionaryAsync(c => c.ClaimID, ct);

        var items = new List<ReviewItem>(claims.Count);
        foreach (var claim in claims)
        {
            var conflict = claim.ConflictClaimID is { } id ? conflicts.GetValueOrDefault(id) : null;
            items.Add(new(claim, claim.StartedBy,
                await LiveIdentityAsync(claim.IdentityID, ct),
                conflict,
                conflict is null ? null : await LiveIdentityAsync(conflict.IdentityID, ct)));
        }
        return (items, total);
    }

    /// <summary>A customer's open claims, newest first: verified ones that hold and those waiting for a review;
    /// with <paramref name="history"/> the ended and expired ones too. Includes claims recorded on numbers
    /// merged into this customer.</summary>
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
        return [.. history ? claims : claims.Where(c => c.Holds || c.Pending)];
    }

    private ClaimState State(VerifiedClaim claim, DateTime now)
    {
        DateTime? expires = options.Kinds.TryGetValue(claim.Kind, out var settings) && settings.Validity is { } validity
            ? claim.StartUtc + validity
            : null;
        bool open = claim.EndUtc is null && (expires is null || expires > now);
        bool verified = claim.Level == VerificationLevel.Verified;
        return new(claim, open && verified, expires, open && !verified);
    }

    /// <summary>
    /// Whether the customer is verified under the rule of <paramref name="context"/> (the host's: a service
    /// visit, a sale; null for the default rule). <paramref name="values"/> holds the visit's own value per
    /// kind, for requirements that ask for a specific one. A requirement is met by a claim at the level it asks
    /// for or above; a held claim meets nothing.
    /// </summary>
    public async Task<VerificationStatus> StatusAsync(long identityId, string? context = null,
        IReadOnlyDictionary<string, string>? values = null, CancellationToken ct = default)
    {
        RequireEnabled();
        if (await LiveIdentityAsync(identityId, ct) is not { } live)
            throw new VerificationException(VerificationProblem.UnknownIdentity, "No such customer.");

        var open = (await ClaimsOfFamilyAsync(await FamilyAsync(live, ct), history: false, ct))
            .Select(c => c.Claim).Where(c => VerificationLevel.Rank(c.Level) > 0).ToList();
        var visit = values is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

        var states = new List<RequirementState>();
        foreach (var requirement in options.RuleFor(context))
        {
            var ofKind = open.Where(c => string.Equals(c.Kind, requirement.Kind, StringComparison.OrdinalIgnoreCase));
            if (requirement.SpecificValue)
            {
                if (!visit.TryGetValue(requirement.Kind, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    states.Add(new(requirement, false, "value-missing", null));
                    continue;
                }

                string key;
                try { key = ValueKey(requirement.Kind, value); }
                catch (VerificationException) { states.Add(new(requirement, false, "value-invalid", null)); continue; }
                ofKind = ofKind.Where(c => string.Equals(c.ValueKey, key, StringComparison.Ordinal));
            }

            var best = ofKind.OrderByDescending(c => VerificationLevel.Rank(c.Level)).ThenByDescending(c => c.StartUtc).FirstOrDefault();
            // A rule that names a level nobody can reach asks for verified.
            int needed = VerificationLevel.Rank(VerificationLevel.CanBeRequired(requirement.Level) ? requirement.Level : VerificationLevel.Verified);
            bool met = best is not null && VerificationLevel.Rank(best.Level) >= needed;
            states.Add(new(requirement, met, met ? null : best is null ? "not-verified" : "pending-review", best));
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
