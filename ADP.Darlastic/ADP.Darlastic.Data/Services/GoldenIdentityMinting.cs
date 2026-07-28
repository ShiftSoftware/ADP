using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Data.Services;

/// <summary>
/// Create-to-gold: mint a golden identity for a record a HOST just created interactively, and bind
/// it as a source profile so the next resolve INHERITS that identity instead of minting a duplicate.
///
/// <para>This is the write half of the batch-first contract. A host's operator creates a customer
/// and must be able to work with them immediately, but matching only runs in batch — so the create
/// allocates the identity synchronously and leaves the unification to the engine.</para>
///
/// <para><b>Why this lives in the module rather than each host.</b> Every value written here is an
/// ENGINE contract, not a host preference: the <c>1 + MAX(IdentityID)</c> allocation must match the
/// resolve's own, the <c>UPDLOCK, HOLDLOCK</c> range-lock is what stops two concurrent creates (or a
/// create racing a resolve) colliding on the primary key, and the RunID-0 / pending-hash sentinels
/// are what the first resolve keys off. A second host re-deriving these from scratch would get them
/// subtly wrong — and a missing HOLDLOCK produces a race that only appears under real concurrency,
/// which is the worst kind of bug to ship to a tenant.</para>
///
/// <para>Deliberately takes primitives rather than a host entity: the caller supplies the source
/// system and record id and stamps the returned identity onto its own row. That keeps the module
/// free of any host's customer shape — a host's customer-entry entity stays the host's, since its
/// attributes and localization are deployment-specific.</para>
///
/// <para>Does NOT call <c>SaveChanges</c> — the caller owns the transaction, because the mint is
/// only correct as part of the same commit that writes the host's record.</para>
/// </summary>
public static class GoldenIdentityMinting
{
    /// <summary>Content-hash sentinel meaning "the CRM wrote this before any resolve saw it". The
    /// owning feed computes the real hash on the first resolve — a one-time, benign rehash, because
    /// inheritance keys on (SourceSystem, SourceRecordId), never on the hash.</summary>
    public const string PendingContentHash = "crm-pending";

    /// <summary>RunID for rows written outside a resolve. Real <c>ResolveRun</c> ids start at 1.</summary>
    public const int PreResolveRunID = 0;

    /// <summary>
    /// Allocate an identity for <paramref name="sourceRecordId"/> and bind it. Returns the new
    /// identity id, which the caller stamps onto its own record.
    /// </summary>
    /// <param name="db">The host's context — the registry is mounted in the host's own database.</param>
    /// <param name="sourceSystem">The channel this record arrived on (e.g. "crm").</param>
    /// <param name="sourceRecordId">The host record's id, as the feed will emit it.</param>
    public static async Task<long> MintAsync(
        DbContext db, string sourceSystem, string sourceRecordId, CancellationToken ct = default)
    {
        // Same allocation the engine's resolve uses. UPDLOCK + HOLDLOCK range-locks the max within
        // the ambient transaction so concurrent minters serialize instead of colliding on the PK.
        long identityId = await db.Database
            .SqlQueryRaw<long>(
                "SELECT ISNULL(MAX(IdentityID), 0) + 1 AS Value FROM [Darlastic].[Identity] WITH (UPDLOCK, HOLDLOCK)")
            .SingleAsync(ct);

        db.Set<GoldenIdentity>().Add(new GoldenIdentity
        {
            IdentityID = identityId,
            Status = IdentityStatus.Active,
            CreatedRunID = PreResolveRunID,
            LastChangedRunID = PreResolveRunID,
        });

        db.Set<SourceProfile>().Add(new SourceProfile
        {
            SourceSystem = sourceSystem,
            SourceRecordId = sourceRecordId,
            IdentityID = identityId,
            ContentHash = PendingContentHash,
            Removed = false,
            FirstRunID = PreResolveRunID,
            LastChangedRunID = PreResolveRunID,
        });

        return identityId;
    }
}
