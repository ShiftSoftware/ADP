using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.DTOs;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using System.Security.Claims;

namespace ShiftSoftware.ADP.Cases.Data;

/// <summary>
/// The history of a claim type: a line for each claim a save creates or moves, whichever path saves it, and the notes.
/// Added to the host's context as an interceptor, so it sees every save of the change tracker. Bulk SQL passes it by.
/// </summary>
public abstract class ClaimHistory<TClaim, THistory> : SaveChangesInterceptor
    where TClaim : ShiftEntity<TClaim>, IClaim
    where THistory : ClaimStatusHistoryBase<THistory, TClaim>, new()
{
    /// <summary>Whether the user saving through <paramref name="db"/> belongs to the distributor.</summary>
    protected abstract bool IsDistributor(DbContext db);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } db)
            Record(db);

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } db)
            Record(db);

        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// Adds the lines of the pending changes. One that is already pending is left alone, so a repository with a comment
    /// for them calls this before it saves, and a save that failed can be tried again.
    /// </summary>
    public void Record(DbContext db, string? comment = null)
    {
        var lines = db.ChangeTracker.Entries<TClaim>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .SelectMany(x => LinesOf(x, Trimmed(comment)))
            .ToList();

        if (lines.Count == 0)
            return;

        var pending = db.ChangeTracker.Entries<THistory>().Where(x => x.State == EntityState.Added).Select(x => x.Entity).ToList();
        var actor = ActorOf(db);

        foreach (var line in lines)
        {
            if (!pending.Any(line.IsIn))
                Append(db, line, actor);
        }
    }

    /// <summary>Adds a line that moves nothing. The caller saves, and has checked that the user may.</summary>
    public void AddNote(DbContext db, TClaim claim, string? comment)
    {
        var note = Trimmed(comment) ?? throw new ShiftEntityException(new Message("Note Required", "Write the note before adding it."));

        if (claim.ClaimStatus is not { } status)
            throw new ShiftEntityException(new Message("Operation Prevented", "A claim without a status can not have notes."));

        Append(db, new Line(claim, status, status, note, DistributorOnly: false), ActorOf(db));
    }

    /// <summary>A claim's history, newest first, without the distributor's own lines for anyone else.</summary>
    public Task<List<CaseStatusHistoryDTO<ClaimStatus>>> ReadAsync(DbContext db, long claimID, bool isDistributor) =>
        db.Set<THistory>()
            .Where(x => x.ClaimID == claimID && (isDistributor || !x.DistributorOnly))
            .OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.ID)
            .Select(x => new CaseStatusHistoryDTO<ClaimStatus>
            {
                FromStatus = x.FromStatus,
                ToStatus = x.ToStatus,
                Timestamp = x.Timestamp,
                ActorUserName = x.ActorUserName,
                ActorRole = x.ActorRole,
                Comment = x.Comment,
                IsNote = x.FromStatus == x.ToStatus,
            })
            .ToListAsync();

    private static IEnumerable<Line> LinesOf(EntityEntry<TClaim> entry, string? comment)
    {
        var claim = entry.Entity;

        if (claim.ClaimStatus is not { } status)
            yield break;

        var added = entry.State == EntityState.Added;
        var from = added ? null : (ClaimStatus?)entry.Property(nameof(IClaim.ClaimStatus)).OriginalValue;
        var error = status == ClaimStatus.RejectedWithError ? Trimmed(claim.DistributorErrorMessage) : null;

        if (from != status)
            yield return new Line(claim, from, status, error ?? comment, DistributorOnly: false);
        else if (error is not null && entry.Property(nameof(IClaim.DistributorErrorMessage)).IsModified)
            yield return new Line(claim, status, status, error, DistributorOnly: false);

        // Not every claim type stores the manufacturer's side.
        if (added || entry.Metadata.FindProperty(nameof(IClaim.ManufacturerStatus)) is null)
            yield break;

        var before = (WarrantyManufacturerClaimStatus?)entry.Property(nameof(IClaim.ManufacturerStatus)).OriginalValue ?? WarrantyManufacturerClaimStatus.NA;
        var after = claim.ManufacturerStatus ?? WarrantyManufacturerClaimStatus.NA;

        if (before != after)
            yield return new Line(claim, status, status, $"Manufacturer: {before.Describe()} → {after.Describe()}", DistributorOnly: true);
    }

    private static void Append(DbContext db, Line line, CaseActor? actor)
    {
        var now = DateTimeOffset.UtcNow;

        // The audit columns are set here: a save has already stamped its rows by the time an interceptor adds one.
        var row = new THistory
        {
            Claim = line.Claim,
            DistributorOnly = line.DistributorOnly,
            CreateDate = now,
            LastSaveDate = now,
            CreatedByUserID = actor?.UserID,
            LastSavedByUserID = actor?.UserID,
            AuditFieldsAreSet = true,
        };

        row.Record(new(line.From, line.To, now, null, line.Comment), actor?.UserID, actor?.UserName, actor?.Role);

        db.Add(row);
    }

    // Null when nobody is signed in. A context without the services still saves, as it does for the audit columns.
    private CaseActor? ActorOf(DbContext db)
    {
        try
        {
            return PrintingServices.GetService<IdentityClaimProvider>(db)?.GetUserID() is { } userID
                ? new CaseActor(userID, PrintingServices.GetService<ICurrentUserProvider>(db)?.GetUser()?.FindFirst(ClaimTypes.GivenName)?.Value, IsDistributor(db))
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? Trimmed(string? comment) => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private sealed record Line(TClaim Claim, ClaimStatus? From, ClaimStatus To, string? Comment, bool DistributorOnly)
    {
        public bool IsIn(THistory row) =>
            ReferenceEquals(row.Claim, Claim) && row.FromStatus == From && row.ToStatus == To && row.DistributorOnly == DistributorOnly
            && (From != To || row.Comment == Comment);
    }
}
