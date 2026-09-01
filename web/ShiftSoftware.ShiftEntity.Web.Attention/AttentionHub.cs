using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// Real-time fan-out hub for attention signals. Connected clients join a group per entity type
/// they care about (<see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.SubscribeToEntityType(System.String)" />); when a signal is raised, the
/// framework's <c>AttentionRealtimeNotifier</c> pushes a hint to that group. Clients also report
/// which record they are viewing right now (<see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.StartViewingEntity(System.String,System.String,System.String)" />). Those reports
/// update the <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.IEntityViewerTracker" />, which evaluators can read.
/// </summary>
/// <remarks>
/// <para>
/// Requires authentication (<see cref="T:Microsoft.AspNetCore.Authorization.AuthorizeAttribute" />) — standard ASP.NET Core SignalR
/// auth. Groups are keyed on entity type only (<see cref="M:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtime.GroupFor(System.String)" />), never
/// per row, so subscribing reveals nothing about individual records; per-row access is enforced
/// when the client re-reads on reload, not here. The pushed
/// <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtimePayload" /> is a refresh hint with no row data, so it cannot leak a
/// row the connection's user can't see.
/// </para>
/// <para>
/// A note on trust: presence reports (<see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.StartViewingEntity(System.String,System.String,System.String)" /> /
/// <see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.StopViewingEntity(System.String,System.String,System.String)" />) come from the client, and are only checked for
/// authentication — not for per-record permission. A client could report viewing a record its
/// user cannot open, and evaluators reading the tracker would then skip raising for that
/// record. An app that needs stronger guarantees should not map this hub's presence methods as
/// its source of truth; it should update the <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.IEntityViewerTracker" /> from its own
/// server-side logic instead.
/// </para>
/// <para>
/// Register and map the hub with <c>services.AddAttentionHub()</c> +
/// <c>app.MapAttentionHub()</c>. Apps that call neither expose no hub endpoint and get no
/// notifier in their DI graph.
/// </para>
/// </remarks>
[Authorize]
public sealed class AttentionHub : Hub
{
	private readonly IEntityViewerTracker viewerTracker;

	private readonly ShiftEntityDtoMap dtoMap;

	private readonly IHashIdService hashIdService;

	public AttentionHub(IEntityViewerTracker viewerTracker, ShiftEntityDtoMap dtoMap, IHashIdService hashIdService)
	{
		this.viewerTracker = viewerTracker;
		this.dtoMap = dtoMap;
		this.hashIdService = hashIdService;
	}

	/// <summary>
	/// Joins the caller's connection to the group for <paramref name="entityType" />, so it
	/// receives <see cref="F:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtime.MessageName" /> hints for that type. A list or form
	/// calls this for the entity type it displays.
	/// </summary>
	public Task SubscribeToEntityType(string entityType)
	{
		return base.Groups.AddToGroupAsync(base.Context.ConnectionId, AttentionRealtime.GroupFor(entityType));
	}

	/// <summary>
	/// Removes the caller's connection from the group for <paramref name="entityType" />. The
	/// client calls this when a list/form is disposed or stops listening.
	/// </summary>
	public Task UnsubscribeFromEntityType(string entityType)
	{
		return base.Groups.RemoveFromGroupAsync(base.Context.ConnectionId, AttentionRealtime.GroupFor(entityType));
	}

	/// <summary>
	/// Records that the caller's connection is viewing one record right now — optionally a
	/// named <paramref name="scope" /> of it, for example a specific tab. This is presence
	/// (<see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.IEntityViewerTracker" />), not a subscription. Evaluators can read it to
	/// skip raising a signal that the viewer would acknowledge right away. A connection may
	/// hold many viewer entries at once (different records, or the same record with different
	/// scopes); calling this adds an entry next to the existing ones, and adding an entry that
	/// already exists does nothing. <paramref name="entityId" /> arrives hash-encoded, because
	/// clients only hold hashed ids. It is decoded here — the reverse of the encoding the
	/// notifier applies when sending. Clients always send all three parameters; a <c>null</c>
	/// scope is normal and means the record as a whole, with no named part.
	/// </summary>
	/// <remarks>
	/// Presence is best-effort. An unknown entity type, or an id that fails to decode, is
	/// silently ignored. The caller never gets an error. When a viewer is not recorded,
	/// evaluators simply raise signals as normal.
	/// </remarks>
	public Task StartViewingEntity(string entityType, string entityId, string? scope)
	{
		if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
		{
			return Task.CompletedTask;
		}
		if (TryDecodeEntityId(entityType, entityId, out var decodedId))
		{
			viewerTracker.AddViewer(base.Context.ConnectionId, entityType, decodedId, scope);
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// Removes exactly one of the caller's viewer entries (the client navigated away from that
	/// record, or closed that part of it). The entry is identified by all three parameters, so
	/// the <paramref name="scope" /> must match the one passed to
	/// <see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.StartViewingEntity(System.String,System.String,System.String)" />. The caller's other entries are kept. Disconnecting
	/// removes every entry at once instead.
	/// </summary>
	/// <remarks>
	/// Best-effort, like <see cref="M:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub.StartViewingEntity(System.String,System.String,System.String)" />: invalid input, an unknown entity
	/// type, or an id that fails to decode is silently ignored.
	/// </remarks>
	public Task StopViewingEntity(string entityType, string entityId, string? scope)
	{
		if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
		{
			return Task.CompletedTask;
		}
		if (TryDecodeEntityId(entityType, entityId, out var decodedId))
		{
			viewerTracker.RemoveViewer(base.Context.ConnectionId, entityType, decodedId, scope);
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// SignalR removes the connection from its groups by itself. The viewer entries are the
	/// only per-connection state that we must clean up ourselves — all of them at once.
	/// </summary>
	public override Task OnDisconnectedAsync(Exception? exception)
	{
		viewerTracker.RemoveConnection(base.Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// The reverse of the encoding the notifier applies when sending. The DTO type comes from
	/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityDtoMap" />, and the id is decoded through the hash-id service,
	/// following the HashID convention. Returns false — it never throws — when the type is not
	/// registered, or when the id does not decode to a valid database id.
	/// </summary>
	private bool TryDecodeEntityId(string entityType, string entityId, out long decodedId)
	{
		decodedId = 0L;
		Type type = default(Type);
		if (!dtoMap.TryGetDtoType(entityType, ref type))
		{
			return false;
		}
		try
		{
			decodedId = hashIdService.Decode(entityId, type);
			return decodedId > 0;
		}
		catch
		{
			return false;
		}
	}
}
