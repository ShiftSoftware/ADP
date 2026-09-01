using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Web.Attention;

namespace Microsoft.Extensions.DependencyInjection;

public static class AttentionHubServiceCollectionExtensions
{
	/// <summary>
	/// Registers the real-time attention surface: SignalR (so <see cref="T:Microsoft.AspNetCore.SignalR.IHubContext`1" />
	/// resolves) plus <c>AttentionRealtimeNotifier</c> as an attention consumer (which also
	/// brings in the emission dispatcher). After this, every committed save that raises a signal
	/// pushes an <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtimePayload" /> to the <c>AttentionHub</c> group for the
	/// entity type. Pair with <c>endpoints.MapAttentionHub()</c> to expose the hub endpoint.
	/// </summary>
	/// <remarks>
	/// Opt-in: apps that don't call this expose no hub endpoint and get no notifier in their DI
	/// graph. Idempotent — <c>AddSignalR()</c> and the consumer registration are both safe to
	/// call alongside an app's own <c>AddSignalR()</c>; call <c>AddSignalR()</c> yourself first
	/// if you need to configure protocols or limits. This overload uses the default
	/// <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHubOptions" />. The defaults include the JwtBearer WebSocket token
	/// handling. Use the configuring overload to turn that off, or to match a custom hub route.
	/// </remarks>
	public static IServiceCollection AddAttentionHub(this IServiceCollection services)
	{
		return services.AddAttentionHub(null);
	}

	/// <summary>
	/// <inheritdoc cref="M:Microsoft.Extensions.DependencyInjection.AttentionHubServiceCollectionExtensions.AddAttentionHub(Microsoft.Extensions.DependencyInjection.IServiceCollection)" path="/summary" />
	/// <paramref name="configure" /> modifies the default <see cref="T:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHubOptions" />.
	/// For example, disable <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHubOptions.EnableWebSocketBearerToken" /> when
	/// the host reads the query-string token itself, or set
	/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHubOptions.HubPath" /> to a custom hub route.
	/// </summary>
	public static IServiceCollection AddAttentionHub(this IServiceCollection services, Action<AttentionHubOptions>? configure)
	{
		AttentionHubOptions attentionHubOptions = new AttentionHubOptions();
		configure?.Invoke(attentionHubOptions);
		services.AddSignalR();
		IServiceCollectionExtensions.AddAttentionConsumer<AttentionRealtimeNotifier>(services);
		services.TryAddScoped<IAttentionRealtimeBroadcaster, AttentionRealtimeNotifier>();
		services.AddHttpContextAccessor();
		services.TryAddSingleton<IAttentionOriginProvider, HttpHeaderAttentionOriginProvider>();
		services.TryAddSingleton<IEntityViewerTracker, InMemoryEntityViewerTracker>();
		if (attentionHubOptions.EnableWebSocketBearerToken)
		{
			AddWebSocketBearerToken(services, attentionHubOptions.HubPath);
		}
		return services;
	}

	/// <summary>
	/// For requests under the hub path, uses the <c>access_token</c> query parameter as the
	/// JwtBearer token. Browsers cannot set the <c>Authorization</c> header on WebSockets,
	/// so SignalR clients send the JWT in the query string instead.
	/// </summary>
	/// <remarks>
	/// This uses <c>PostConfigureAll</c>. Because of that, it works together with
	/// <c>AddJwtBearer</c>/<c>Configure</c> calls, whether they run before or after
	/// <c>AddAttentionHub</c>. When JwtBearer is not configured at all, it does nothing: the
	/// post-configure simply never runs against a resolved scheme. Any previously assigned
	/// <c>OnMessageReceived</c> is chained, not replaced. The previous delegate runs first,
	/// and if it sets a token, that token is kept. Two limits to know about. First, this
	/// query-token handling applies to <em>every</em> registered JwtBearer scheme. It only
	/// acts on requests under the hub path, and only when no token was set yet, so other
	/// schemes are not affected outside those two conditions. Second, a host that assigns
	/// <c>OnMessageReceived</c> inside its own <c>PostConfigure</c> registered <em>after</em>
	/// <c>AddAttentionHub</c> replaces this handling, because post-configures run in
	/// registration order. Such a host should call the previous delegate from its own, or
	/// disable
	/// <see cref="P:ShiftSoftware.ShiftEntity.Web.Attention.AttentionHubOptions.EnableWebSocketBearerToken" />
	/// and add the query-token handling itself.
	/// </remarks>
	private static void AddWebSocketBearerToken(IServiceCollection services, string hubPath)
	{
		PathString path = new PathString(hubPath);
		services.PostConfigureAll(delegate(JwtBearerOptions jwtOptions)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected O, but got Unknown
			//IL_0024: Expected O, but got Unknown
			if (jwtOptions.Events == null)
			{
				JwtBearerEvents val = new JwtBearerEvents();
				JwtBearerEvents val2 = val;
				jwtOptions.Events = val;
			}
			Func<MessageReceivedContext, Task> previous = jwtOptions.Events.OnMessageReceived;
			jwtOptions.Events.OnMessageReceived = async delegate(MessageReceivedContext context)
			{
				if (previous != null)
				{
					await previous(context);
				}
				if (string.IsNullOrEmpty(context.Token))
				{
					string text = ((BaseContext<JwtBearerOptions>)(object)context).Request.Query["access_token"];
					if (!string.IsNullOrEmpty(text) && ((BaseContext<JwtBearerOptions>)(object)context).Request.Path.StartsWithSegments(path))
					{
						context.Token = text;
					}
				}
			};
		});
	}
}
