using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// The Phase 2 dispatcher consumer that turns an <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRaised" /> event into a
/// real-time SignalR hint. Fans each event to the <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHub" /> group for its
/// entity type (<see cref="M:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtime.GroupFor(System.String)" />), where subscribed
/// <c>ShiftList</c> / <c>ShiftEntityForm</c> instances react. Registered by
/// <c>services.AddAttentionHub()</c>.
/// </summary>
/// <remarks>
/// Runs on the framework's background drain loop after the raising save has committed, like any
/// <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.IAttentionConsumer" /> — a slow or failing hub send never affects the save. The raw
/// <see cref="P:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRaised.EntityId" /> is hash-encoded here, at the process boundary, via the
/// entity's DTO type from <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityDtoMap" />, per the HashID convention.
/// </remarks>
public sealed class AttentionRealtimeNotifier : IAttentionConsumer, IAttentionRealtimeBroadcaster
{
	private readonly IHubContext<AttentionHub> hub;

	private readonly IHashIdService hashIdService;

	private readonly ShiftEntityDtoMap dtoMap;

	private readonly ILogger<AttentionRealtimeNotifier> logger;

	public AttentionRealtimeNotifier(IHubContext<AttentionHub> hub, IHashIdService hashIdService, ShiftEntityDtoMap dtoMap, ILogger<AttentionRealtimeNotifier> logger)
	{
		this.hub = hub;
		this.hashIdService = hashIdService;
		this.dtoMap = dtoMap;
		this.logger = logger;
	}

	/// <summary>
	/// Dispatcher path: a newly-raised signal becomes a real-time <see cref="F:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtimeKind.Raised" />
	/// hint, excluding the window that raised it (<see cref="P:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRaised.OriginConnectionId" />).
	/// </summary>
	public Task HandleAsync(AttentionRaised attentionRaised, CancellationToken cancellationToken)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return SendAsync(attentionRaised.EntityType, attentionRaised.EntityId, (AttentionRealtimeKind)0, attentionRaised.Signal.Severity, attentionRaised.Signal.RaisedAt, attentionRaised.OriginConnectionId, cancellationToken);
	}

	/// <inheritdoc />
	public Task BroadcastClearedAsync(string entityType, long entityId, string? originConnectionId = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(entityType, entityId, (AttentionRealtimeKind)1, (AttentionSeverity)1, DateTimeOffset.UtcNow, originConnectionId, cancellationToken);
	}

	/// <summary>
	/// Builds the hint payload and sends it to the entity-type group, excluding the originating
	/// window's connection when one was supplied so it isn't notified about its own change.
	/// </summary>
	private Task SendAsync(string entityType, long entityId, AttentionRealtimeKind kind, AttentionSeverity severity, DateTimeOffset raisedAt, string? originConnectionId, CancellationToken cancellationToken)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		AttentionRealtimePayload val = new AttentionRealtimePayload();
		val.set_EntityType(entityType);
		val.set_EntityId(EncodeEntityId(entityType, entityId));
		val.set_Kind(kind);
		val.set_Severity(severity);
		val.set_RaisedAt(raisedAt);
		AttentionRealtimePayload arg = val;
		string groupName = AttentionRealtime.GroupFor(entityType);
		return (string.IsNullOrEmpty(originConnectionId) ? hub.Clients.Group(groupName) : hub.Clients.GroupExcept(groupName, new string[1] { originConnectionId })).SendAsync("AttentionRaised", arg, cancellationToken);
	}

	/// <summary>
	/// Hash-encodes a raw entity ID using the entity type's DTO hash configuration.
	/// </summary>
	/// <remarks>
	/// An entity with attention is normally a registered repository, so its DTO type is in
	/// <see cref="T:ShiftSoftware.ShiftEntity.Core.ShiftEntityDtoMap" />. If it isn't, we can't hash-encode — rather than drop the
	/// refresh hint, we degrade to the raw ID as an invariant string (the same fallback as
	/// <c>GET api/attention/active</c>) and log a warning so the missing registration is visible.
	/// </remarks>
	private string EncodeEntityId(string entityType, long entityId)
	{
		Type type = default(Type);
		if (dtoMap.TryGetDtoType(entityType, ref type))
		{
			return hashIdService.Encode(entityId, type);
		}
		logger.LogWarning("No DTO type registered for entity type {EntityType}; the real-time attention hint falls back to the un-encoded entity ID. Register the entity's repository so its ID hash-encodes per the HashID convention.", entityType);
		return entityId.ToString(CultureInfo.InvariantCulture);
	}
}
