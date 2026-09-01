using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.EFCore.Attention;
using ShiftSoftware.ShiftEntity.EFCore.Entities;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// Standalone (non-controller) attention endpoints for cross-entity operations.
/// Supplements the per-entity controller endpoints on <c>ShiftEntitySecureControllerAsync</c>.
/// </summary>
public static class AttentionEndpoints
{
	/// <summary>
	/// Maps standalone attention endpoints: <c>POST {prefix}/clear</c> (clears signals for a
	/// specific entity) and <c>GET {prefix}/active</c> (returns all uncleared indexed-mode
	/// signals with hash-encoded entity IDs). Both require an authenticated user, and access is
	/// then decided per entity type: by the <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" />
	/// hook when one is supplied through the configuring overload; otherwise by the TypeAuth
	/// action registered for the type in the <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> (read requires
	/// <c>CanRead</c>, clear requires <c>CanWrite</c>); otherwise by
	/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" />, which denies by default.
	/// See the <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions" /> remarks for the full order and for how
	/// the registry is fed.
	/// </summary>
	public static IEndpointRouteBuilder MapAttentionEndpoints<TDbContext>(this IEndpointRouteBuilder endpoints, string prefix = "api/attention") where TDbContext : ShiftDbContext
	{
		return endpoints.MapAttentionEndpoints<TDbContext>(prefix, null);
	}

	/// <summary>
	/// <inheritdoc cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpoints.MapAttentionEndpoints``1(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder,System.String)" path="/summary" />
	/// <paramref name="configure" /> modifies the default <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions" />.
	/// Its main purpose is to supply <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" />.
	/// </summary>
	public static IEndpointRouteBuilder MapAttentionEndpoints<TDbContext>(this IEndpointRouteBuilder endpoints, string prefix, Action<AttentionEndpointsOptions>? configure) where TDbContext : ShiftDbContext
	{
		ShiftEntityDtoMap entityDtoMap = endpoints.ServiceProvider.GetRequiredService<ShiftEntityDtoMap>();
		AttentionEndpointsOptions options = new AttentionEndpointsOptions();
		configure?.Invoke(options);
		endpoints.MapPost(prefix + "/clear", (Func<HttpContext, TDbContext, IHashIdService, IdentityClaimProvider, IServiceProvider, ClearAttentionRequest, Task<IResult>>)(async (HttpContext httpContext, TDbContext db, IHashIdService hashIdService, IdentityClaimProvider identityClaimProvider, IServiceProvider serviceProvider, [FromBody] ClearAttentionRequest request) =>
		{
			if (!(await IsEntityTypeAllowed(httpContext, options, request.EntityType, AttentionEndpointAccess.Clear)))
			{
				return Results.StatusCode(403);
			}
			Type dtoType = entityDtoMap.GetDtoType(request.EntityType);
			if ((object)dtoType == null)
			{
				return Results.NotFound("Unknown entity type: " + request.EntityType);
			}
			long entityId = hashIdService.Decode(request.EntityId, dtoType);
			long? userID = identityClaimProvider.GetUserID();
			try
			{
				DateTimeOffset? lastSaveDate = await AttentionPipeline.ClearSignals((ShiftDbContext)(object)db, request.EntityType, entityId, userID, request.Filter);
				IAttentionRealtimeBroadcaster service = serviceProvider.GetService<IAttentionRealtimeBroadcaster>();
				if (service != null)
				{
					IAttentionOriginProvider? service2 = serviceProvider.GetService<IAttentionOriginProvider>();
					string originConnectionId = ((service2 != null) ? service2.OriginConnectionId : null);
					try
					{
						await service.BroadcastClearedAsync(request.EntityType, entityId, originConnectionId);
					}
					catch
					{
					}
				}
				ClearAttentionResponse val = new ClearAttentionResponse();
				val.set_LastSaveDate(lastSaveDate);
				return Results.Ok<ClearAttentionResponse>(val);
			}
			catch (InvalidOperationException ex)
			{
				return Results.NotFound(ex.Message);
			}
		})).RequireAuthorization();
		endpoints.MapGet(prefix + "/active", (Func<HttpContext, TDbContext, IHashIdService, Task<IResult>>)async delegate(HttpContext httpContext, TDbContext db, IHashIdService hashIdService)
		{
			List<AttentionSignalEntry> entries = await EntityFrameworkQueryableExtensions.ToListAsync<AttentionSignalEntry>((IQueryable<AttentionSignalEntry>)((IQueryable<AttentionSignalEntry>)((DbContext)(object)db).Set<AttentionSignalEntry>()).Where((Expression<Func<AttentionSignalEntry, bool>>)((AttentionSignalEntry x) => x.ClearedAt == null)).OrderByDescending((Expression<Func<AttentionSignalEntry, AttentionSeverity>>)((AttentionSignalEntry x) => x.Severity)).ThenByDescending((Expression<Func<AttentionSignalEntry, DateTimeOffset>>)((AttentionSignalEntry x) => x.RaisedAt)), default(CancellationToken));
			Dictionary<string, bool> allowedTypes = new Dictionary<string, bool>(StringComparer.Ordinal);
			foreach (string item in entries.Select((AttentionSignalEntry x) => x.EntityType).Distinct<string>(StringComparer.Ordinal))
			{
				Dictionary<string, bool> dictionary = allowedTypes;
				string key = item;
				dictionary[key] = await IsEntityTypeAllowed(httpContext, options, item, AttentionEndpointAccess.Read);
			}
			entries = entries.Where((AttentionSignalEntry x) => allowedTypes[x.EntityType]).ToList();
			return Results.Ok(entries.Select(delegate(AttentionSignalEntry x)
			{
				StoredAttentionSignal val = x.ToStoredSignal();
				Type dtoType = entityDtoMap.GetDtoType(x.EntityType);
				if ((object)dtoType != null)
				{
					StoredAttentionSignal obj = val._003CClone_003E_0024();
					obj.set_EntityId(hashIdService.Encode(x.EntityId, dtoType));
					val = obj;
				}
				return val;
			}).ToList());
		}).RequireAuthorization();
		return endpoints;
	}

	/// <summary>
	/// The per-entity-type access decision, in the order documented on
	/// <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions" />: the <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.AuthorizeEntityType" />
	/// hook alone when set; otherwise the TypeAuth action registered in the
	/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" /> (<see cref="M:ShiftSoftware.TypeAuth.Core.ITypeAuthService.CanRead(ShiftSoftware.TypeAuth.Core.Actions.ReadWriteDeleteAction)" />
	/// for read, <see cref="M:ShiftSoftware.TypeAuth.Core.ITypeAuthService.CanWrite(ShiftSoftware.TypeAuth.Core.Actions.ReadWriteDeleteAction)" /> for clear — the
	/// same calls the secure controller makes); otherwise
	/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionEndpointsOptions.UnmappedEntityTypeAccess" />.
	/// </summary>
	/// <remarks>
	/// The registry is resolved with <c>GetService</c>: when no <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityActionMap" />
	/// is registered at all, the behavior is the same as an empty map — every type is unmapped.
	/// </remarks>
	internal static async ValueTask<bool> IsEntityTypeAllowed(HttpContext httpContext, AttentionEndpointsOptions options, string entityType, AttentionEndpointAccess access)
	{
		Func<HttpContext, string, AttentionEndpointAccess, ValueTask<bool>> authorizeEntityType = options.AuthorizeEntityType;
		if (authorizeEntityType != null)
		{
			return await authorizeEntityType(httpContext, entityType, access);
		}
		ShiftEntityActionMap service = httpContext.RequestServices.GetService<ShiftEntityActionMap>();
		ReadWriteDeleteAction val = default(ReadWriteDeleteAction);
		if (service != null && service.TryGetAction(entityType, ref val))
		{
			ITypeAuthService requiredService = httpContext.RequestServices.GetRequiredService<ITypeAuthService>();
			return (access == AttentionEndpointAccess.Read) ? requiredService.CanRead(val) : requiredService.CanWrite(val);
		}
		return options.UnmappedEntityTypeAccess == AttentionUnmappedEntityTypeAccess.AllowAuthenticated;
	}

	/// <summary>
	/// Maps the <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub" /> SignalR endpoint (default route
	/// <see cref="F:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtime.DefaultHubRoute" />). The hub itself requires authentication
	/// (<c>[Authorize]</c>). Call alongside <c>services.AddAttentionHub()</c>; apps that do
	/// neither expose no hub.
	/// </summary>
	public static HubEndpointConventionBuilder MapAttentionHub(this IEndpointRouteBuilder endpoints, string pattern = "/hubs/attention")
	{
		return endpoints.MapHub<AttentionHub>(pattern);
	}
}
