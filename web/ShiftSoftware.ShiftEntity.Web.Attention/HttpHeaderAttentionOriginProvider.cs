using Microsoft.AspNetCore.Http;
using ShiftSoftware.ShiftEntity.Core.Attention;

namespace ShiftSoftware.ShiftEntity.Web.Attention;

/// <summary>
/// HTTP-bound <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.IAttentionOriginProvider" />: reads the originating hub connection id from
/// the <see cref="F:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRealtime.OriginHeader" /> header of the current request. Registered by
/// <c>AddAttentionHub()</c>.
/// </summary>
/// <remarks>
/// Wraps the singleton <see cref="T:Microsoft.AspNetCore.Http.IHttpContextAccessor" />, so it reflects whatever request is
/// ambient on the current async flow — the save / clear request while it runs, and nothing once
/// that request has completed. The real-time fan-out for a <em>raise</em> runs later on a
/// background drain loop where no request is ambient, which is exactly why the raise path captures
/// the origin during the save and carries it on the <see cref="T:ShiftSoftware.ShiftEntity.Core.Attention.AttentionRaised" /> event rather
/// than re-reading it here; the <em>clear</em> path broadcasts in-request and reads it directly.
/// </remarks>
internal sealed class HttpHeaderAttentionOriginProvider : IAttentionOriginProvider
{
	private readonly IHttpContextAccessor httpContextAccessor;

	public string? OriginConnectionId
	{
		get
		{
			HttpContext httpContext = httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				return null;
			}
			string text = httpContext.Request.Headers["X-Attention-Origin"].ToString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return null;
		}
	}

	public HttpHeaderAttentionOriginProvider(IHttpContextAccessor httpContextAccessor)
	{
		this.httpContextAccessor = httpContextAccessor;
	}
}
