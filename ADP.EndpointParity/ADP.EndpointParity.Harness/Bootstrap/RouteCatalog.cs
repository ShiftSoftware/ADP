using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here and cannot: this file asks the
// running application for its own action descriptors. Reviewed by reading its diff.
// ============================================================================================

/// <summary>
/// One route as the application itself reports it.
/// </summary>
public sealed record CatalogueRoute(
    string Method,
    string Template,
    string Controller,
    string Action,
    string Parameters)
{
    /// <summary>
    /// The stable key a ParityCase points at for the coverage gate. Method + template is
    /// enough to identify a route and is stable across a rename of the action method.
    /// </summary>
    public string Key => Method + " " + Template;
}

/// <summary>
/// Emits the route catalogue from <see cref="IActionDescriptorCollectionProvider"/>, and drives
/// the case list from it.
///
/// <para>
/// Route enumeration is GENERATED, never written by hand. That buys three things a URL-driven
/// harness cannot have: inherited ShiftEntityControllerAsync routes (list, detail, post, put,
/// delete, revisions, print) appear without anyone listing them; a route that DISAPPEARS in the
/// upgrade shows up as a catalogue diff; and route-prefix conventions are exercised as
/// configured rather than as assumed. A hand-maintained URL list is precisely the list that
/// omits the route that broke.
/// </para>
///
/// <para>
/// <b>Enumerating a route is not exercising it.</b> The catalogue is itself a golden AND the
/// source of the coverage gate: every entry must resolve to at least one case, or appear in
/// parity.psd1's excludedRoutes with a written reason. The templated CRUD list reaches the
/// inherited surface and nothing else, while the groups carry a large hand-written surface -
/// Darlastic 31 hand-written actions, WarrantyClaims 14, Menus 13, Surveys 12, ClaimableItems 3,
/// including the whole anonymous renderer surface in the one group this plan calls "full HTTP
/// parity". Those exist in the coverage report only because this rule forces someone to write
/// down why they are not covered.
/// </para>
/// </summary>
public static class RouteCatalog
{
    /// <summary>
    /// Reads every route the booted application exposes.
    ///
    /// <para>
    /// <b>Source is EndpointDataSource, not IActionDescriptorCollectionProvider.</b>
    /// verification.md section 5 names the latter, and it is the right idea, but it sees only
    /// CONTROLLER actions. These hosts also map minimal APIs - MapShiftIdentityDashboard() for
    /// the whole auth/identity surface and MapShiftEntityEndpoints&lt;DB&gt;() for the
    /// attribute-driven CRUD entities - and those are invisible to the action-descriptor
    /// provider. Enumerating from the endpoint data source is a superset: it still reports every
    /// controller action (each carries a ControllerActionDescriptor in its endpoint metadata),
    /// and it additionally catches the minimal-API routes that a controller-only catalogue would
    /// silently omit. Omitting them would defeat the one thing the catalogue exists for - noticing
    /// that a route DISAPPEARED.
    /// </para>
    /// </summary>
    public static IReadOnlyList<CatalogueRoute> Enumerate(IServiceProvider services)
    {
        var sources = services.GetRequiredService<EndpointDataSource>();
        var routes = new List<CatalogueRoute>();

        foreach (var endpoint in sources.Endpoints.OfType<RouteEndpoint>())
        {
            var template = endpoint.RoutePattern.RawText;
            if (template is null) continue;

            var descriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var httpMethods = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IHttpMethodMetadata>();

            var controller = descriptor?.ControllerName ?? "";
            var action = descriptor?.ActionName ?? endpoint.DisplayName ?? "";

            var parameters = descriptor is null
                ? string.Join(", ", endpoint.RoutePattern.Parameters.Select(p => p.Name))
                : string.Join(", ", descriptor.Parameters.Select(p => p.ParameterType.Name + " " + p.Name));

            // No explicit method constraint means the endpoint answers any verb. Recorded as ANY
            // rather than silently expanded, so the catalogue says what the app actually declared.
            var methods = httpMethods is null || httpMethods.HttpMethods.Count == 0
                ? new[] { "ANY" }
                : httpMethods.HttpMethods.OrderBy(m => m, StringComparer.Ordinal).ToArray();

            foreach (var method in methods)
                routes.Add(new CatalogueRoute(method, "/" + template.TrimStart('/'), controller, action, parameters));
        }

        return routes
            .OrderBy(r => r.Template, StringComparer.Ordinal)
            .ThenBy(r => r.Method, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Renders the catalogue as its golden file.</summary>
    public static string ToGolden(IReadOnlyList<CatalogueRoute> routes)
    {
        var arr = new JsonArray();
        foreach (var r in routes)
        {
            arr.Add(new JsonObject
            {
                ["method"] = r.Method,
                ["template"] = r.Template,
                ["controller"] = r.Controller,
                ["action"] = r.Action,
                ["parameters"] = r.Parameters,
            });
        }

        return Canonical.Write(new JsonObject { ["routes"] = arr });
    }
}
