using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Web.Endpoints;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Maps the attribute-driven CRUD endpoints for entities decorated with
/// <c>[ShiftEntityEndpoint&lt;…&gt;]</c> / <c>[ShiftEntitySecureEndpoint&lt;…&gt;]</c>. The DI side is wired
/// automatically by <c>RegisterShiftRepositories(...)</c>; this just maps the routes. When no
/// <paramref name="assemblies" /> are passed it uses the data assemblies registered via
/// <c>AddShiftEntityWeb(x =&gt; x.AddDataAssembly(...))</c>.
/// </summary>
public static class ShiftEntityEndpointsEndpointRouteBuilderExtensions
{
	public static IEndpointRouteBuilder MapShiftEntityEndpoints<DB>(this IEndpointRouteBuilder endpoints, params Assembly[] assemblies) where DB : ShiftDbContext
	{
		if (assemblies == null || assemblies.Length == 0)
		{
			assemblies = endpoints.ServiceProvider.GetService<ShiftEntityOptions>()?.DataAssemblies.ToArray() ?? Array.Empty<Assembly>();
		}
		IReadOnlyList<ShiftEntityEndpointSpec> specs = ShiftEntityEndpointDiscovery.Discover((IEnumerable<Assembly>)assemblies);
		ShiftEntityGeneratedEndpoints.Generate(endpoints, specs, typeof(DB));
		return endpoints;
	}
}
