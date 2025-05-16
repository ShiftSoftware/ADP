using AutoMapper;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Extensions;

public static class ISyncServiceExtensions
{
    public static ISyncService<TSource, TDestination> UseAutoMapper<TSource, TDestination>(
        this ISyncService<TSource, TDestination> syncService,
        IMapper mapper)
        where TSource : class, new()
        where TDestination : class, new()
    {
        syncService.SetupMapping(x =>new ValueTask<IEnumerable<TDestination?>?>(mapper.Map<IEnumerable<TDestination>>(x)));

        return syncService;
    }
}
