using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Region;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Team;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;

namespace Microsoft.AspNetCore.Mvc;

public static class HttpContextExtensions
{
	public static List<string>? GetClaimValues(this HttpContext httpContext, string claimId)
	{
		if (httpContext == null || httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
		{
			return null;
		}
		List<string> list = (from x in httpContext.User.FindAll(claimId)
			select x.Value).ToList();
		if (list == null)
		{
			return null;
		}
		return list;
	}

	public static List<long>? GetDecodedClaimValues<T>(this HttpContext httpContext, string claimId)
	{
		List<string> claimValues = httpContext.GetClaimValues(claimId);
		if (claimValues == null)
		{
			return null;
		}
		IHashIdService hashIdService = httpContext.RequestServices.GetRequiredService<IHashIdService>();
		return claimValues.Select((string x) => hashIdService.Decode<T>(x)).ToList();
	}

	public static string? GetHashedRegionID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/RegionId")?.FirstOrDefault();
	}

	public static long? GetRegionID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<RegionDTO>("ShiftSoftware/ShiftEntity/Claims/RegionId")?.FirstOrDefault();
	}

	public static string? GetHashedCountryID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/CountryId")?.FirstOrDefault();
	}

	public static long? GetCountryID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<CountryDTO>("ShiftSoftware/ShiftEntity/Claims/CountryId")?.FirstOrDefault();
	}

	public static string? GetHashedCompanyID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/CompanyId")?.FirstOrDefault();
	}

	public static long? GetCompanyID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<CompanyDTO>("ShiftSoftware/ShiftEntity/Claims/CompanyId")?.FirstOrDefault();
	}

	public static string? GetHashedCityID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/CityId")?.FirstOrDefault();
	}

	public static long? GetCityID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<CityDTO>("ShiftSoftware/ShiftEntity/Claims/CityId")?.FirstOrDefault();
	}

	public static string? GetHashedCompanyBranchID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/CompanyBranchId")?.FirstOrDefault();
	}

	public static long? GetCompanyBranchID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<CompanyBranchDTO>("ShiftSoftware/ShiftEntity/Claims/CompanyBranchId")?.FirstOrDefault();
	}

	public static long? GetUserID(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<UserDTO>("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.FirstOrDefault();
	}

	public static string? GetUserEmail(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.FirstOrDefault();
	}

	public static string? GetHashedUserID(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.FirstOrDefault();
	}

	public static List<string>? GetHashedTeamIDs(this HttpContext httpContext)
	{
		return httpContext.GetClaimValues("ShiftSoftware/ShiftEntity/Claims/TeamIds");
	}

	public static List<long>? GetTeamIDs(this HttpContext httpContext)
	{
		return httpContext.GetDecodedClaimValues<TeamDTO>("ShiftSoftware/ShiftEntity/Claims/TeamIds");
	}
}
