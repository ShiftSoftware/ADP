using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

/// <summary>
/// Loads, for the request, the cities a caller without the reveal permission may see
/// (<see cref="DarlasticApiOptions.CityMinimumShare"/>), so the record and golden views the controller
/// builds can show or mask a city without a query of their own. Does nothing while the option is off.
///
/// <para>The set is every golden city at least that many golden customers carry, read once per resolve
/// run (<see cref="RegistryRunCache"/>). Records are matched to it without regard to case: a record's
/// city slot is the normalized form of the text the golden city is the display form of.</para>
///
/// <para>Every Darlastic controller carries it (<see cref="CityDisclosureConvention"/>), so a surface added
/// later cannot forget it: a view built without it would show every city.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
internal sealed class CityDisclosureAttribute : Attribute, IAsyncResourceFilter
{
    private static readonly object ItemKey = new();

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        int share = services.GetRequiredService<IOptions<DarlasticApiOptions>>().Value.CityMinimumShare;
        if (share > 0)
        {
            var db = services.GetRequiredService<ShiftDbContext>();
            context.HttpContext.Items[ItemKey] = await services.GetRequiredService<RegistryRunCache>().GetAsync(
                db, $"shown-cities:{share}", (d, ct) => SharedCitiesAsync(d, share, ct), context.HttpContext.RequestAborted);
        }
        await next();
    }

    /// <summary>The cities a caller without the reveal permission may see on this request, or null when
    /// every city may be shown (the option is off).</summary>
    public static IReadOnlySet<string>? ShownCities(HttpContext? http) =>
        http?.Items.TryGetValue(ItemKey, out var set) == true ? (IReadOnlySet<string>?)set : null;

    private static async Task<IReadOnlySet<string>> SharedCitiesAsync(ShiftDbContext db, int share, CancellationToken ct)
    {
        var cities = await db.Set<GoldenCustomer>().AsNoTracking()
            .Where(g => g.City != null && g.City != "")
            .GroupBy(g => g.City!)
            .Where(g => g.Count() >= share)
            .Select(g => g.Key)
            .ToListAsync(ct);
        return cities.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>Puts <see cref="CityDisclosureAttribute"/> on every controller of this module.</summary>
internal sealed class CityDisclosureConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var module = typeof(CityDisclosureConvention).Assembly;
        foreach (var controller in application.Controllers.Where(c => c.ControllerType.Assembly == module))
            controller.Filters.Add(new CityDisclosureAttribute());
    }
}
