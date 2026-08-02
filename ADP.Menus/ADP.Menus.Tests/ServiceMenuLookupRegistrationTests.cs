using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Extensions;
using ShiftSoftware.ADP.Lookup.Services.Services;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 5 — the service-menu lookup's registration.
///
/// Offline: a <see cref="CosmosClient"/> connects lazily, so a container can be built and every service
/// resolved without an endpoint. That is the point — this checks the WIRING, not Cosmos.
///
/// Worth its own file because the wiring is the part a compiler cannot check. A missing registration, a
/// constructor the container cannot satisfy, or options that never reach the evaluator all surface at
/// the first request rather than at build time.
/// </summary>
public class ServiceMenuLookupRegistrationTests
{
    /// <summary>Well-known local emulator endpoint/key. Never connected to here — construction is lazy.</summary>
    private const string ConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CosmosClient(ConnectionString));
        return services;
    }

    [Fact]
    public void AddServiceMenuLookup_ResolvesTheWholeGraph()
    {
        using var provider = Services().AddServiceMenuLookup().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuLookupService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuGenerationEvaluator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuCosmosService>());
    }

    /// <summary>Registering with no configuration at all must still resolve — every option is optional.</summary>
    [Fact]
    public async Task Registration_WithoutConfiguration_UsesTheDefaults()
    {
        using var provider = Services().AddServiceMenuLookup().BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<ServiceMenuGenerationEvaluator>()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12" });

        Assert.Equal(0, config.CountryID);
        Assert.Equal(1m, config.TransferRate);
        Assert.False(config.UsePrimaryLabourRate);
        Assert.False(config.IncludePartCost);
    }

    [Fact]
    public async Task ConfiguredOptions_ReachTheEvaluator()
    {
        using var provider = Services()
            .AddServiceMenuLookup(options => options.DefaultCountryID = 7)
            .BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<ServiceMenuGenerationEvaluator>()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12" });

        Assert.Equal(7, config.CountryID);
    }

    /// <summary>
    /// The reason no resolver takes an <see cref="IServiceProvider"/>: a host that needs its own
    /// services inside one configures the option WITH them, which is what the options pattern is for.
    /// Pinned because it is the migration path for anyone who reaches for a service provider parameter.
    /// </summary>
    [Fact]
    public async Task OptionsCanBeConfiguredFromTheHostsOwnServices_WithoutAServiceProviderParameter()
    {
        var services = Services();
        services.AddSingleton(new CountryCatalogue(TransferRate: 2.5m, SingleCountry: true));
        services.AddServiceMenuLookup();

        services.AddOptions<ServiceMenuLookupOptions>()
            .Configure<CountryCatalogue>((options, countries) =>
                options.CountrySettingsResolver = _ => new ValueTask<ServiceMenuCountrySettings>(
                    new ServiceMenuCountrySettings
                    {
                        TransferRate = countries.TransferRate,
                        UsePrimaryLabourRate = countries.SingleCountry,
                    }));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<ServiceMenuGenerationEvaluator>()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12", TransferRate = 9m });

        Assert.Equal(2.5m, config.TransferRate);
        Assert.True(config.UsePrimaryLabourRate);
    }

    /// <summary>
    /// Everything registers with TryAdd, so a host that calls this twice — or alongside a future general
    /// lookup registration that calls it internally — gets one registration, not a duplicate set.
    /// </summary>
    [Fact]
    public void Registration_IsIdempotent()
    {
        var services = Services()
            .AddServiceMenuLookup(options => options.DefaultCountryID = 1)
            .AddServiceMenuLookup();

        Assert.Single(services, x => x.ServiceType == typeof(ServiceMenuLookupService));
        Assert.Single(services, x => x.ServiceType == typeof(ServiceMenuCosmosService));
        Assert.Single(services, x => x.ServiceType == typeof(ServiceMenuGenerationEvaluator));
    }

    /// <summary>
    /// The general lookup registration does NOT bring service menus along yet — a host opts in
    /// explicitly. Pinned so the day that changes is a deliberate edit to this test rather than a
    /// surprise for hosts that never provisioned the menu containers.
    /// </summary>
    [Fact]
    public void AddLookupService_DoesNotRegisterTheMenuLookup()
    {
        var services = Services();
        services.AddLookupService(new LookupOptions());

        Assert.DoesNotContain(services, x => x.ServiceType == typeof(ServiceMenuLookupService));
    }

    private sealed record CountryCatalogue(decimal TransferRate, bool SingleCountry);
}
