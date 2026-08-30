using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class ServiceIntervalRepository : ShiftRepository<ShiftDbContext, ServiceInterval, ServiceIntervalListDTO, ServiceIntervalDTO>
{
    // The view's ServiceIntervalGroup selector needs nothing: the convention builds it through
    // MappingHelpers.ToSelectDTO from the foreign key. Only the flattened list column has no convention,
    // because it reaches through a navigation.
    public ServiceIntervalRepository(ShiftDbContext db) : base(db, x => x.UseGeneratedMapper(map => map
        .ForList(d => d.ServiceIntervalGroupName, e => e.ServiceIntervalGroup.Name)))
    {
    }
}
