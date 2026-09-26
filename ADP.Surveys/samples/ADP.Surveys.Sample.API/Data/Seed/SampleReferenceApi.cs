using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;

/// <summary>
/// Stand-in for a deployment's public reference API — the city and branch lists the
/// "Sample: External API options" survey fetches in the respondent's browser. A real
/// deployment points <c>optionsSource.url</c> at its own public, CORS-open endpoints; this
/// host serves canned lists in the same shape (a JSON array of <c>ID</c> / <c>Name</c>
/// items, the SDK's default value and label paths), so the sample works offline.
///
/// To demo against a real deployment, set <see cref="BaseUrlSetting"/> in user secrets or
/// on the command line, never in a committed file. It is read only when the sample is
/// first seeded into a database.
/// </summary>
public static class SampleReferenceApi
{
    public const string BaseUrlSetting = "SampleSurveys:ReferenceApiBaseUrl";

    /// <summary>This host's own endpoints, at the port in launchSettings.json.</summary>
    public const string DefaultBaseUrl = "http://localhost:5134/api/public";

    private static readonly ReferenceItem[] Cities =
    {
        new("riverside", "Riverside"),
        new("hillview", "Hillview"),
        new("lakeshore", "Lakeshore"),
    };

    private static readonly Branch[] Branches =
    {
        new("riverside-showroom", "Riverside Showroom", new[] { "new-vehicle-sale" }),
        new("hillview-showroom", "Hillview Showroom", new[] { "new-vehicle-sale", "auto-repair-and-maintenance" }),
        new("riverside-service", "Riverside Service Center", new[] { "auto-repair-and-maintenance", "body-and-paint" }),
        new("lakeshore-body-shop", "Lakeshore Body & Paint", new[] { "body-and-paint" }),
    };

    public static IEndpointRouteBuilder MapSampleReferenceApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public").AllowAnonymous();

        group.MapGet("/city", () => Cities);

        // ?services= narrows the list to the branches offering that service: the
        // per-screen variation the sample authors through queryParams / sourceParams.
        group.MapGet("/company-branch", (string? services) => Branches
            .Where(b => string.IsNullOrEmpty(services) || b.Services.Contains(services))
            .Select(b => new ReferenceItem(b.Id, b.Name)));

        return app;
    }

    // Named explicitly so the wire shape does not depend on the host's JSON naming policy.
    private sealed record ReferenceItem(
        [property: JsonPropertyName("ID")] string Id,
        [property: JsonPropertyName("Name")] string Name);

    private sealed record Branch(string Id, string Name, string[] Services);
}
