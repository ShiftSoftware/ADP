using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// Request payload for the standalone attention clear endpoint (<c>POST {prefix}/clear</c>).
/// Entity IDs are hash-encoded.
/// </summary>
public sealed class ClearAttentionRequest
{
	/// <summary>CLR type name of the entity whose signals should be cleared.</summary>
	public required string EntityType { get; set; }

	/// <summary>Hash-encoded entity ID. Decoded via <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityDtoMap" /> to resolve the DTO type.</summary>
	public required string EntityId { get; set; }

	/// <summary>
	/// Which signals to clear. <c>null</c> clears every active signal (the default); a scoped or
	/// per-signal filter clears only the matching subset and leaves the rest active.
	/// </summary>
	public AttentionClearFilter? Filter { get; set; }
}
