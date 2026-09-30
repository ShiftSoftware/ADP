using System.Globalization;
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

    /// <summary>
    /// Department and brand the "Book a visit" sample reads calendars for. The stand-in
    /// calendar ignores both; against a real deployment set them to its own values, the same
    /// way as <see cref="BaseUrlSetting"/>.
    /// </summary>
    public const string BookingDepartmentSetting = "SampleSurveys:BookingDepartmentId";
    public const string BookingBrandSetting = "SampleSurveys:BookingBrandId";
    public const string DefaultBookingDepartmentId = "service-center";
    public const string DefaultBookingBrandId = "BRAND";

    /// <summary>
    /// Full URL of the calendar the "Book a visit" sample reads. Unset, it is this host's
    /// stand-in, <c>calendar</c> under the base URL.
    /// </summary>
    public const string BookingCalendarSetting = "SampleSurveys:BookingCalendarUrl";

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

        // Shape of a public calendar endpoint: the next days with hourly slots, starting two
        // days out. One branch has no calendar, so the sample also shows the booking
        // question's empty state.
        group.MapGet("/calendar", (string? from, string? branchId) =>
        {
            if (string.IsNullOrEmpty(branchId) || branchId == "lakeshore-body-shop")
                return Array.Empty<CalendarDay>();

            var start = DateOnly.TryParseExact(from, "yyyy-MM-dd", out var parsed) ? parsed : DateOnly.FromDateTime(DateTime.Today);
            return Enumerable.Range(2, 14)
                .Select(start.AddDays)
                .Where(day => day.DayOfWeek != DayOfWeek.Friday)
                .Select(day => new CalendarDay(
                    day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    new[] { 8, 9, 10, 11, 13, 14, 15 }
                        .Select(hour => day.ToDateTime(new TimeOnly(hour, 0)).ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture))
                        .ToArray()))
                .ToArray();
        });

        return app;
    }

    private sealed record CalendarDay(
        [property: JsonPropertyName("Date")] string Date,
        [property: JsonPropertyName("Times")] string[] Times);

    // Named explicitly so the wire shape does not depend on the host's JSON naming policy.
    private sealed record ReferenceItem(
        [property: JsonPropertyName("ID")] string Id,
        [property: JsonPropertyName("Name")] string Name);

    private sealed record Branch(string Id, string Name, string[] Services);
}
