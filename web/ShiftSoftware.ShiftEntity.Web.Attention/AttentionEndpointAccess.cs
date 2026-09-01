namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// The kind of access an <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" /> hook is
/// asked to allow or deny for one entity type.
/// </summary>
public enum AttentionEndpointAccess
{
	/// <summary>Reading the entity type's active signals (<c>GET {prefix}/active</c>).</summary>
	Read,
	/// <summary>Clearing the entity type's signals (<c>POST {prefix}/clear</c>).</summary>
	Clear
}
