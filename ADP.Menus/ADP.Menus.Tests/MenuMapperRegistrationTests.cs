using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Sync.Extensions;
using ShiftSoftware.ADP.Menus.Sync.Replication;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// The menu maps as a host gets them: <c>AddMenuApiServices</c> hands <c>ADP.Menus.Data</c> to
/// <c>RegisterShiftRepositories</c>, which registers that assembly's generated ShiftMapper mapper. Every repository
/// triple must be covered in all four directions, or the host does not start (ShiftEntityMapperValidation). The
/// same registration carries the Cosmos replication's maps, so no host registers those itself.
/// </summary>
public class MenuMapperRegistrationTests
{
    private static readonly System.Reflection.Assembly DataAssembly = typeof(Data.Marker).Assembly;

    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.RegisterShiftRepositories(DataAssembly);

        // This project's own registration. It also has a BUILD-time job: referencing ShiftIdentity.Data and
        // ADP.Menus.Data puts their mapper classes' maps into this assembly's generated mapper, and a pack the
        // framework shares (the ShiftEntitySelectDTO convention among it) reaches maps generated in a project only
        // through a registration there. Without one, ShiftIdentity's maps fail this project's build with SM0011.
        services.AddShiftMapper();
        return services;
    }

    private static IEnumerable<(Type Entity, Type List, Type View)> RepositoryTriples() =>
        DataAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.BaseType)
            .Where(b => b is { IsGenericType: true } && b.GetGenericTypeDefinition() == typeof(ShiftRepository<,,,>))
            .Select(b => b!.GetGenericArguments())
            .Select(a => (a[1], a[2], a[3]));

    [Fact]
    public void Every_repository_triple_is_mapped_in_all_four_directions()
    {
        using var provider = Services().BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var triples = RepositoryTriples().ToList();
        Assert.Equal(10, triples.Count);

        foreach (var (entity, list, view) in triples)
        {
            Assert.True(mapper.CanMap(entity, view), $"{entity.Name} -> {view.Name}");
            Assert.True(mapper.CanMap(view, entity), $"{view.Name} -> {entity.Name}");
            Assert.True(mapper.CanMap(entity, list), $"{entity.Name} -> {list.Name}");
            Assert.True(mapper.CanMap(entity, entity), $"{entity.Name} -> {entity.Name}");
        }
    }

    [Fact]
    public void Startup_mapping_validation_passes() =>
        ShiftEntityMapperValidation.Validate(Services(), [DataAssembly]);

    private static readonly (Type Entity, Type Document)[] ReplicatedDocuments =
    [
        (typeof(ServiceInterval), typeof(ServiceIntervalCosmosModel)),
        (typeof(ServiceIntervalGroup), typeof(ServiceIntervalGroupCosmosModel)),
        (typeof(ReplacementItem), typeof(ReplacementItemCosmosModel)),
        (typeof(StandaloneReplacementItemGroup), typeof(StandaloneReplacementItemGroupCosmosModel)),
        (typeof(LabourRateMapping), typeof(LabourRateMappingCosmosModel)),
        (typeof(BrandMapping), typeof(BrandMappingCosmosModel)),
        (typeof(MenuVariant), typeof(MenuVariantCosmosModel)),
        (typeof(MenuPeriodicAvailability), typeof(MenuPeriodCosmosModel)),
        (typeof(MenuLabourDetails), typeof(MenuLabourCosmosModel)),
        (typeof(MenuItem), typeof(MenuItemCosmosModel)),
    ];

    /// <summary>
    /// A host replicating the menu catalog writes no mapping line: the registration it already makes for the menu API
    /// carries every document the replication writes, and the replication maps through that very mapper.
    /// </summary>
    [Fact]
    public void The_menu_registration_carries_the_replication_maps()
    {
        var services = new ServiceCollection();
        services.RegisterShiftRepositories(DataAssembly);   // what AddMenuApiServices does

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = MenuCosmosDocuments.ResolveMapper(scope.ServiceProvider);

        Assert.Same(scope.ServiceProvider.GetRequiredService<IMapper>(), mapper);
        foreach (var (entity, document) in ReplicatedDocuments)
            Assert.True(mapper.CanMap(entity, document), $"{entity.Name} -> {document.Name}");
    }

    /// <summary>
    /// A host that registered no mapper at all — a catch-up-only Functions app — still replicates, through
    /// ADP.Menus.Data's own generated mapper.
    /// </summary>
    [Fact]
    public void A_host_with_no_mapper_registered_still_replicates()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        var mapper = MenuCosmosDocuments.ResolveMapper(provider);

        foreach (var (entity, document) in ReplicatedDocuments)
            Assert.True(mapper.CanMap(entity, document), $"{entity.Name} -> {document.Name}");

        var interval = new ServiceInterval(501) { Code = "S1", FullName = "Interval 1", Description = "d", ServiceIntervalGroupID = 61 };
        var document501 = MenuCosmosDocuments.Map(mapper, interval);
        Assert.Equal("501", document501.id);
        Assert.Equal(501, document501.ServiceIntervalID);
    }
}
