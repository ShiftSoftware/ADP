using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared.Validation;

/// <summary>
/// Contract for a case type's upsert-time validation layer (skr-dtr D4/D15: pure, DB-free,
/// test-first — lookups and identity go behind interfaces/delegates in the implementing service).
/// Invoked from the repository's <c>UpsertAsync</c> BEFORE the workflow engine applies a trigger.
/// </summary>
/// <typeparam name="TCase">The case entity type.</typeparam>
/// <typeparam name="TInput">The incoming DTO/input the entity is being updated from.</typeparam>
public interface ICaseValidationService<in TCase, in TInput>
{
    /// <summary>
    /// Validates the pending change; returns aggregated problems (empty = valid). The caller wraps
    /// non-empty results in a <c>ShiftEntityException</c> using the shared "Error" dialog shape.
    /// </summary>
    IReadOnlyList<Message> Validate(TCase @case, TInput input, CaseActorContext actor);
}

/// <summary>
/// Who is performing the operation, as far as the pure validation/workflow layer needs to know.
/// The consumer resolves this from its own TypeAuth/capability provider (e.g. a consumer's
/// <c>ICapabilityProvider.IsDistributor</c> or an elevated-role check) — the engine never touches TypeAuth.
/// </summary>
/// <param name="UserId">Acting user id (for history attribution).</param>
/// <param name="IsElevated">True for the org-side elevated role (org staff / distributor).</param>
/// <param name="Role">Optional role tag matched against <c>CaseTransition.RequiredRole</c> by the consumer.</param>
public sealed record CaseActorContext(string? UserId = null, bool IsElevated = false, string? Role = null);
