using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The module's maps, resolved the way a host resolves them: <c>AddWarrantyClaimsApiServices</c> hands the Data
/// assembly to <c>RegisterShiftRepositories</c>, which registers that assembly's generated ShiftMapper mapper - the
/// repositories' automatic maps and <c>Mappers/WarrantyClaimsMapper.cs</c> - and a repository maps through it behind
/// <see cref="ShiftMapperEntityMapper{EntityType, ListDTO, ViewAndUpsertDTO}"/>.
/// </summary>
internal static class ModuleMapper
{
    private static readonly Lazy<ServiceProvider> provider = new(() => Services().BuildServiceProvider());

    public static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.RegisterShiftRepositories(typeof(Marker).Assembly);
        return services;
    }

    /// <summary>A mapper as a request gets one: a fresh scope each time.</summary>
    public static IMapper Mapper() =>
        provider.Value.CreateScope().ServiceProvider.GetRequiredService<IMapper>();

    /// <summary>The mapper a repository closing this triple maps through.</summary>
    public static IShiftEntityMapper<TEntity, TListDTO, TViewDTO> For<TEntity, TListDTO, TViewDTO>()
        where TEntity : ShiftEntity<TEntity> =>
        new ShiftMapperEntityMapper<TEntity, TListDTO, TViewDTO>(Mapper(), context: null);
}
