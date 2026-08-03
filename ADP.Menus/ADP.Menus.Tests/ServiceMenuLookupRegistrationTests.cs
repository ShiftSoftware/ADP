using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.Enums;
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
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12" });

        Assert.Equal(2.5m, config.TransferRate);
        Assert.True(config.UsePrimaryLabourRate);
    }

    /// <summary>
    /// Everything registers with TryAdd, so a host that calls this twice — or alongside the general lookup
    /// registration, which calls it internally — gets one registration, not a duplicate set.
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
    /// PHASE 6 — the general lookup registration now brings service menus with it, because the menu is part
    /// of the vehicle lookup result. This inverts what Phase 5 pinned, deliberately.
    ///
    /// <para>Registering is safe for a deployment that never provisioned the menu containers: nothing here
    /// touches Cosmos, and the section is only read when a request opts in with
    /// <c>ServiceMenuOptions.Include</c> — which reports a provisioning fault as a status rather than
    /// raising it.</para>
    /// </summary>
    [Fact]
    public void AddLookupService_RegistersTheMenuLookup()
    {
        var services = Services();
        services.AddLookupService(new LookupOptions { VehicleLookupStorageSource = StorageSources.CosmosDB });

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ServiceMenuLookupService>());

        // And the vehicle lookup can actually reach it. VehicleLookupService takes it as an OPTIONAL
        // constructor parameter, so a registration the container cannot satisfy does not fail — it silently
        // passes null, and every menu section would report NotRegistered forever with nothing to show why.
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<VehicleLookupService>());

        Assert.NotNull(
            typeof(VehicleLookupService)
                .GetConstructors()
                .Single()
                .GetParameters()
                .SingleOrDefault(parameter => parameter.ParameterType == typeof(ServiceMenuLookupService)));
    }

    /// <summary>
    /// PHASE 6 — the menu options are reachable from the ONE call every host already makes.
    ///
    /// <para>This matters more than convenience. <c>AddLookupService</c> registers the menu lookup, so
    /// without a way to configure it here the defaults apply silently: country 0 and no
    /// <c>CountrySettingsResolver</c>, which charges a single-country deployment the wrong labour rate
    /// (open item O6). The dangerous configuration must not be the one a host gets for doing nothing
    /// special.</para>
    /// </summary>
    [Fact]
    public async Task ServiceMenuOptions_AreConfigurableFromTheGeneralRegistration()
    {
        var services = Services();

        services.AddLookupService(new LookupOptions
        {
            VehicleLookupStorageSource = StorageSources.CosmosDB,
            ConfigureServiceMenu = menu =>
            {
                menu.DefaultCountryID = 7;
                menu.CountrySettingsResolver = _ => new ValueTask<ServiceMenuCountrySettings>(
                    new ServiceMenuCountrySettings { TransferRate = 2.5m, UsePrimaryLabourRate = true });
            },
        });

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<ServiceMenuGenerationEvaluator>()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12" });

        Assert.Equal(7, config.CountryID);
        Assert.Equal(2.5m, config.TransferRate);
        Assert.True(config.UsePrimaryLabourRate);
    }

    /// <summary>
    /// A suffix set through the general registration's menu configuration beats the one inherited from
    /// <see cref="LookupOptions.CosmosDatabaseNameSuffix"/> — the inherited value is a fallback for hosts
    /// that said nothing, not an override of hosts that did.
    /// </summary>
    [Fact]
    public void ConfigureServiceMenu_BeatsTheInheritedCosmosSuffix()
    {
        var services = Services();

        services.AddLookupService(new LookupOptions
        {
            CosmosDatabaseNameSuffix = "-general",
            ConfigureServiceMenu = menu => menu.CosmosDatabaseNameSuffix = "-menus",
        });

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(
            "-menus",
            provider.GetRequiredService<IOptions<ServiceMenuLookupOptions>>().Value.CosmosDatabaseNameSuffix);
    }

    /// <summary>
    /// Calling both registrations still yields ONE menu lookup, in either order, with the host's own menu
    /// options intact. This is the shape a host that wants menus WITHOUT the vehicle lookup ends up with,
    /// and it has to keep composing with the general registration.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddLookupService_ComposesWithAnExplicitMenuRegistration(bool menusFirst)
    {
        var services = Services();

        if (menusFirst)
        {
            services.AddServiceMenuLookup(options => options.DefaultCountryID = 7);
            services.AddLookupService(new LookupOptions());
        }
        else
        {
            services.AddLookupService(new LookupOptions());
            services.AddServiceMenuLookup(options => options.DefaultCountryID = 7);
        }

        Assert.Single(services, x => x.ServiceType == typeof(ServiceMenuLookupService));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var config = await scope.ServiceProvider
            .GetRequiredService<ServiceMenuGenerationEvaluator>()
            .ResolveConfigAsync(new ServiceMenuLookupRequest { BasicModelCode = "ABC12" });

        Assert.Equal(7, config.CountryID);
    }

    /// <summary>
    /// A dev pointing the whole lookup at suffixed databases means the menu containers too — the two option
    /// classes are separate, so the general registration seeds the menu one. An explicit menu suffix still
    /// wins, whichever order the two registrations ran in: the seed coalesces, it does not assign.
    /// </summary>
    [Theory]
    [InlineData(true, "-alt")]
    [InlineData(false, "-alt")]
    public void CosmosDatabaseNameSuffix_FlowsToTheMenuOptions_UnlessTheHostSetsItsOwn(bool menusFirst, string menuSuffix)
    {
        var services = Services();

        void AddMenus() => services.AddServiceMenuLookup(options => options.CosmosDatabaseNameSuffix = menuSuffix);
        void AddLookup() => services.AddLookupService(new LookupOptions { CosmosDatabaseNameSuffix = "-general" });

        if (menusFirst) { AddMenus(); AddLookup(); } else { AddLookup(); AddMenus(); }

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(
            menuSuffix,
            provider.GetRequiredService<IOptions<ServiceMenuLookupOptions>>().Value.CosmosDatabaseNameSuffix);
    }

    /// <summary>Without an explicit menu suffix, the general one is inherited rather than silently lost.</summary>
    [Fact]
    public void CosmosDatabaseNameSuffix_IsInheritedWhenTheMenuOptionsDoNotSetOne()
    {
        var services = Services();
        services.AddLookupService(new LookupOptions { CosmosDatabaseNameSuffix = "-general" });

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(
            "-general",
            provider.GetRequiredService<IOptions<ServiceMenuLookupOptions>>().Value.CosmosDatabaseNameSuffix);
    }

    private sealed record CountryCatalogue(decimal TransferRate, bool SingleCountry);
}
