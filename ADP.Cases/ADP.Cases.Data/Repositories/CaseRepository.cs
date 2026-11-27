using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Flow;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.DTOs;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Validation;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Flags;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace ShiftSoftware.ADP.Cases.Data.Repositories;

/// <summary>
/// What every case type's repository does: turns a save into a status change, applies the actions and notes, hides
/// drafts from the distributor and numbers a new case.
/// </summary>
/// <remarks>
/// The rules are the case type's validation, <see cref="CaseDefinition{TStatus, TTrigger}"/> and
/// <see cref="CaseFlow{TCase, THistory, TStatus, TTrigger}"/>; this is the part that needs a database. Who may read and
/// write a case is the framework's data-level access.
/// </remarks>
public abstract class CaseRepository<TDb, TCase, TListDTO, TDTO, THistory, TStatus, TTrigger>
    : ShiftRepository<TDb, TCase, TListDTO, TDTO>, ICaseRepository<TCase, TTrigger>
    where TDb : ShiftDbContext
    where TCase : ShiftEntity<TCase>, ICase<TStatus>, ICaseWithHistory<THistory>, IEntityHasCompany<TCase>, new()
    where TListDTO : ShiftEntityDTOBase
    where TDTO : ShiftEntityViewAndUpsertDTO, ICaseDTO<TStatus>
    where THistory : CaseStatusHistoryBase<THistory, TStatus>, new()
    where TStatus : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly CaseDefinition<TStatus, TTrigger> definition;
    private readonly CaseFlow<TCase, THistory, TStatus, TTrigger> flow;
    private readonly Expression<Func<TCase, bool>> isNotDraft;

    protected CaseRepository(
        TDb db,
        CaseDefinition<TStatus, TTrigger> definition,
        Action<ShiftRepositoryOptions<TCase, TListDTO, TDTO>> options) : base(db, options)
    {
        this.definition = definition;
        flow = new(definition);
        isNotDraft = IsNot(definition.Draft);
    }

    // ----- What a case type says -----

    /// <summary>Whether the signed-in person belongs to the distributor.</summary>
    protected abstract bool IsDistributor { get; }

    /// <summary>The case type's own rules that depend on who is saving or on the stored case.</summary>
    protected virtual IReadOnlyList<Message> Validate(TCase stored, TDTO dto, CaseActorContext actor) => [];

    /// <summary>Runs before anything else looks at what was sent.</summary>
    protected virtual void Prepare(TDTO dto)
    {
    }

    /// <summary>The numbers already given to a dealership's cases, deleted ones included.</summary>
    protected abstract IQueryable<long> NumbersOf(long companyID);

    /// <summary>Gives a new case its number.</summary>
    protected abstract void SetNumber(TCase raised, long number);

    // ----- Reading -----

    /// <summary>
    /// Everything the grants allow, less dealers' drafts when the distributor is asking. Here rather than in the list,
    /// so opening a case and acting on it get it too.
    /// </summary>
    public override async ValueTask<IQueryable<TCase>> GetIQueryable(
        DateTimeOffset? asOf,
        List<string>? includes,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters)
    {
        var query = await base.GetIQueryable(asOf, includes, disableDefaultDataLevelAccess, disableGlobalFilters);

        return IsDistributor ? query.Where(isNotDraft) : query;
    }

    // ----- Saving -----

    /// <summary>Saves a case, and applies the status change the save implies.</summary>
    public override async ValueTask<TCase> UpsertAsync(
        TCase entity,
        TDTO dto,
        ActionTypes actionType,
        long? userId,
        Guid? idempotencyKey,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters)
    {
        Prepare(dto);

        // Resolved the way the base class resolves it, so the history names the actor the audit columns will name.
        var actor = CurrentActor(userId);

        // Both run before anything is written, so the entity still holds what is stored. The case type's map must not
        // write the status.
        CaseMessageAggregator.ThrowIfAny(Validate(entity, dto, actor.Context).ToList());

        var resolution = flow.Resolve(entity, dto.Status, actor);

        var upserted = await base.UpsertAsync(
            entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        if (actionType == ActionTypes.Insert)
            await RaiseAsync(upserted, dto, actor);
        else
            flow.Save(upserted, resolution, actor);

        return upserted;
    }

    /// <summary>
    /// Gives a newly raised case its number and its first status, and opens its history. A draft is numbered straight
    /// away, so one that is never sent leaves a gap.
    /// </summary>
    private async Task RaiseAsync(TCase raised, TDTO dto, CaseActor actor)
    {
        // Stamped from the signed-in user by the base call. The number is counted per dealership.
        if (raised.CompanyID is not { } companyID)
            throw new ShiftEntityException(new Message(
                "Not Allowed",
                $"A {definition.Noun} has to belong to a dealership, and your account is not linked to one."));

        flow.Raise(raised, dto.Status, actor);

        // Two cases raised at the same moment can read the same last number; a unique index on dealership and number
        // turns that into a failed save.
        SetNumber(raised, (await NumbersOf(companyID).MaxAsync(x => (long?)x) ?? 0) + 1);
    }

    // ----- Actions and notes -----

    /// <inheritdoc/>
    public async Task<TCase> ApplyActionAsync(long id, TTrigger trigger, string? comment)
    {
        var actor = CurrentActor();

        // Before the case is looked for, so someone who may not act learns nothing about whether it exists.
        flow.EnsureActionOf(actor, trigger);

        var found = await FindForActionAsync(id);

        flow.Act(found, trigger, comment, actor);

        return found;
    }

    /// <inheritdoc/>
    public void ApplyActionToAll(IReadOnlyCollection<TCase> cases, TTrigger trigger, string? comment) =>
        flow.ActOnAll(cases, trigger, comment, CurrentActor());

    /// <inheritdoc/>
    public async Task<TCase> AddNoteAsync(long id, string? comment)
    {
        var note = CaseFlow<TCase, THistory, TStatus, TTrigger>.RequireNote(comment);

        var found = await FindForActionAsync(id);

        flow.AddNote(found, note, CurrentActor());

        return found;
    }

    /// <summary>The case an action or a note is for, with the caller's grants applied.</summary>
    private async Task<TCase> FindForActionAsync(long id)
    {
        var found = await FindAsync(id);

        return found is null || found.IsDeleted ? throw NotFound() : found;
    }

    private CaseActor CurrentActor(long? userID = null) => new(
        userID ?? identityClaimProvider.GetUserID(),
        currentUserProvider?.GetUser()?.FindFirst(ClaimTypes.GivenName)?.Value,
        IsDistributor);

    protected ShiftEntityException NotFound() =>
        new(new Message("Not Found", $"This {definition.Noun} does not exist."), (int)HttpStatusCode.NotFound);

    private static Expression<Func<TCase, bool>> IsNot(TStatus status)
    {
        var x = Expression.Parameter(typeof(TCase), "x");

        return Expression.Lambda<Func<TCase, bool>>(
            Expression.NotEqual(
                Expression.Property(x, nameof(ICase<TStatus>.Status)),
                Expression.Constant(status, typeof(TStatus?))),
            x);
    }
}

/// <summary>The actions of a case type's repository, as its endpoints call them. The caller saves.</summary>
public interface ICaseRepository<TCase, TTrigger> where TTrigger : struct, Enum
{
    /// <summary>
    /// Applies an action to a saved case. Only the status moves, and the comment goes on the history line. The caller has
    /// checked the permission; this checks that the action is the caller's side's, and the engine checks the move.
    /// </summary>
    Task<TCase> ApplyActionAsync(long id, TTrigger trigger, string? comment);

    /// <summary>Applies an action to several cases at once, all or none.</summary>
    void ApplyActionToAll(IReadOnlyCollection<TCase> cases, TTrigger trigger, string? comment);

    /// <summary>Adds a note to a saved case: a history line that moves nothing.</summary>
    Task<TCase> AddNoteAsync(long id, string? comment);
}
