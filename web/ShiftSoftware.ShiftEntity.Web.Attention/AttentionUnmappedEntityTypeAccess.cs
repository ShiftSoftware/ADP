namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// What the standalone attention endpoints do with signals of an entity type that has no entry
/// in the <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> (and no
/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" /> hook is set).
/// </summary>
public enum AttentionUnmappedEntityTypeAccess
{
	/// <summary>
	/// The default. Signals of an unmapped type are left out of <c>GET {prefix}/active</c>, and
	/// <c>POST {prefix}/clear</c> for an unmapped type returns 403.
	/// </summary>
	Deny,
	/// <summary>
	/// Any authenticated user can read and clear signals of an unmapped type. Only use this when
	/// every authenticated user is trusted to see and clear those signals.
	/// </summary>
	AllowAuthenticated
}
