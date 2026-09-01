using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace ShiftSoftware.ShiftEntity.Web.Services;

public static class SwaggerService
{
	public static bool DocInclusionPredicate(string docName, ApiDescription apiDesc)
	{
		return true;
	}
}
