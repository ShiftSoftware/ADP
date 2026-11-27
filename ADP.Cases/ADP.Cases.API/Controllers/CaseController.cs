using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ShiftSoftware.ADP.Cases.Data.Repositories;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.DTOs;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Web;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Cases.API.Controllers;

/// <summary>
/// What every case type's endpoints have besides the framework's own: the actions and the notes.
/// </summary>
/// <remarks>
/// Reading and writing a case is gated by the case type's access node. <c>ChangeStatus</c>, its second node, is what lets
/// the distributor move a case or comment on it; the dealer's side of both needs no such permission. Then the repository
/// checks that the action is the caller's side's, and the engine checks the move.
/// </remarks>
public abstract class CaseController<TRepository, TCase, TListDTO, TDTO, TStatus, TTrigger>
    : ShiftEntitySecureControllerAsync<TRepository, TCase, TListDTO, TDTO>
    where TRepository : class, IShiftRepositoryAsync<TCase, TListDTO, TDTO>, IShiftRepositoryWithOptions<TCase, TListDTO, TDTO>, ICaseRepository<TCase, TTrigger>
    where TCase : ShiftEntity<TCase>, ICase<TStatus>, new()
    where TListDTO : ShiftEntityDTOBase
    where TDTO : ShiftEntityViewAndUpsertDTO, ICaseDTO<TStatus>
    where TStatus : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly TRepository repository;
    private readonly IHashIdService hashIds;
    private readonly ReadWriteDeleteAction access;
    private readonly BooleanAction changeStatus;

    protected CaseController(
        TRepository repository,
        IHashIdService hashIds,
        ReadWriteDeleteAction access,
        BooleanAction changeStatus) : base(access)
    {
        this.repository = repository;
        this.hashIds = hashIds;
        this.access = access;
        this.changeStatus = changeStatus;
    }

    /// <summary>The case type's rules.</summary>
    protected abstract CaseDefinition<TStatus, TTrigger> Definition { get; }

    /// <summary>Whether the signed-in person belongs to the distributor.</summary>
    protected abstract bool IsDistributor { get; }

    /// <summary>An action on a saved case, from the form's buttons. Answers with the case as it now reads.</summary>
    [HttpPost("{key}/action/{trigger}")]
    [Authorize]
    public Task<IActionResult> Action(
        string key, TTrigger trigger, [FromBody] CaseActionDTO? action, [FromServices] ITypeAuthService typeAuth) =>
        IsUnknown(nameof(trigger)) ? Task.FromResult(UnknownAction())
        : MayAct(trigger, typeAuth)
            ? RespondAsync(() => repository.ApplyActionAsync(hashIds.Decode<TDTO>(key), trigger, action?.Comment))
            : Task.FromResult(NotAllowed($"You cannot change the status of a {Definition.Noun}."));

    /// <summary>
    /// An action on every case ticked on the list, all or none. The rows are found through the ordinary query, so one
    /// outside the caller's grants is not in it.
    /// </summary>
    [HttpPost("batch-action/{trigger}")]
    [Authorize]
    public async Task<IActionResult> BatchAction(
        TTrigger trigger, [FromBody] CaseBatchActionDTO<TListDTO> action, [FromServices] ITypeAuthService typeAuth)
    {
        if (IsUnknown(nameof(trigger)))
            return UnknownAction();

        if (!MayAct(trigger, typeAuth))
            return NotAllowed($"You cannot change the status of a {Definition.Noun}.");

        try
        {
            repository.ApplyActionToAll(await GetSelectedEntitiesAsync(action.Selection), trigger, action.Comment);

            await repository.SaveChangesAsync();

            return Ok(new ShiftEntityResponse<IList<TListDTO>> { Entity = action.Selection.Items });
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<IList<TListDTO>>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }

    /// <summary>
    /// A note on a saved case: a history line that moves nothing. Answers with the case as it now reads. A dealer's note
    /// needs only the access node; the distributor's also needs <c>ChangeStatus</c>.
    /// </summary>
    [HttpPost("{key}/note")]
    [Authorize]
    public Task<IActionResult> Note(
        string key, [FromBody] CaseActionDTO? note, [FromServices] ITypeAuthService typeAuth) =>
        typeAuth.CanWrite(access) && (!IsDistributor || typeAuth.CanAccess(changeStatus))
            ? RespondAsync(() => repository.AddNoteAsync(hashIds.Decode<TDTO>(key), note?.Comment))
            : Task.FromResult(NotAllowed($"You cannot add a note to a {Definition.Noun}."));

    /// <summary>
    /// Writing a case is enough for a dealer's action. The distributor's also need <c>ChangeStatus</c>.
    /// </summary>
    private bool MayAct(TTrigger trigger, ITypeAuthService typeAuth) =>
        typeAuth.CanWrite(access)
        && (!Definition.IsActionOf(trigger, CaseRoles.Distributor) || typeAuth.CanAccess(changeStatus));

    /// <summary>Saves what <paramref name="apply"/> changed and answers with the case, or with its refusal.</summary>
    private async Task<IActionResult> RespondAsync(Func<Task<TCase>> apply)
    {
        try
        {
            var changed = await apply();

            await repository.SaveChangesAsync();

            return Ok(new ShiftEntityResponse<TDTO>(await repository.ViewAsync(changed)));
        }
        catch (ShiftEntityException ex)
        {
            return StatusCode(ex.HttpStatusCode, new ShiftEntityResponse<TDTO>
            {
                Message = ex.Message,
                Additional = ex.AdditionalData,
            });
        }
    }

    // A value in the route that is not one of the trigger's binds as its zero.
    private bool IsUnknown(string trigger) => ModelState.GetFieldValidationState(trigger) == ModelValidationState.Invalid;

    private IActionResult UnknownAction() =>
        BadRequest(new ShiftEntityResponse<TDTO> { Message = new Message("Not Allowed", "Unknown action.") });

    private IActionResult NotAllowed(string body) =>
        StatusCode(StatusCodes.Status403Forbidden, new ShiftEntityResponse<TDTO> { Message = new Message("Not Allowed", body) });
}
