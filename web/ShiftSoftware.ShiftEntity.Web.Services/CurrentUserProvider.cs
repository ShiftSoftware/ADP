using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ShiftEntity.Web.Services;

public class CurrentUserProvider : ICurrentUserProvider
{
	private readonly ClaimsPrincipal? claimsPrincipal;

	public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
	{
		claimsPrincipal = httpContextAccessor.HttpContext?.User;
	}

	public ClaimsPrincipal? GetUser()
	{
		return claimsPrincipal;
	}
}
