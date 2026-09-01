using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>Options for <see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpoints.MapAttentionEndpoints``1(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder,System.String)" />.</summary>
/// <remarks>
/// Per-entity-type authorization runs in this order:
/// <list type="number">
///   <item>When <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" /> is set, it alone decides. The registry and
///   <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" /> are not consulted at all.</item>
///   <item>Otherwise, when the <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> registry has an action for the
///   signal's entity type, that action is checked with TypeAuth: reading the type's signals
///   requires <c>CanRead</c>, clearing them requires <c>CanWrite</c> — the same checks the
///   entity's own secure endpoints apply.</item>
///   <item>Otherwise (no hook, no registry entry) <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" />
///   decides. The default is <see cref="F:ShiftSoftware.ShiftEntity.Web.Attention.AttentionUnmappedEntityTypeAccess.Deny" />.</item>
/// </list>
/// The registry is fed automatically by secure attribute endpoints
/// (<c>[ShiftEntitySecureEndpoint&lt;…&gt;]</c>, via <c>RegisterShiftRepositories</c>) and by
/// <c>MapShiftEntitySecureCrud</c> when it is called with a non-null action. Entities served by
/// a classic <c>ShiftEntitySecureControllerAsync</c> must be registered explicitly with
/// <c>services.AddShiftEntityAction&lt;TEntity&gt;(action)</c>, because the controller receives
/// its action through its constructor and the framework cannot see it at startup.
/// </remarks>
public sealed class AttentionEndpointsOptions
{
	/// <summary>
	/// An authorization hook that decides access per entity type — the full override for special
	/// cases the registry cannot express. It receives the request's <see cref="T:Microsoft.AspNetCore.Http.HttpContext" />
	/// (resolve your authorization service from it), the signal's entity CLR type name, and the
	/// kind of access being requested. When the hook is set, it alone decides: the
	/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> registry and <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" /> are
	/// not consulted. <c>GET {prefix}/active</c> leaves out signals of every type for which the
	/// hook denies <see cref="F:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointAccess.Read" /> (the hook runs once per distinct
	/// type in a request, not once per signal). <c>POST {prefix}/clear</c> returns 403 when the
	/// hook denies <see cref="F:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointAccess.Clear" /> for the requested type.
	/// </summary>
	/// <remarks>
	/// When <c>null</c> (the default), access is decided by the <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" />
	/// registry, and for entity types without a registry entry by
	/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" />. See the class remarks for the full order.
	/// </remarks>
	public Func<HttpContext, string, AttentionEndpointAccess, ValueTask<bool>>? AuthorizeEntityType { get; set; }

	/// <summary>
	/// What happens for signals of an entity type that has no entry in the
	/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> registry, when no <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" />
	/// hook is set. The default is <see cref="F:ShiftSoftware.ShiftEntity.Web.Attention.AttentionUnmappedEntityTypeAccess.Deny" />: such
	/// signals are left out of <c>GET {prefix}/active</c>, and <c>POST {prefix}/clear</c>
	/// returns 403 for them.
	/// </summary>
	public AttentionUnmappedEntityTypeAccess UnmappedEntityTypeAccess { get; set; }
}
