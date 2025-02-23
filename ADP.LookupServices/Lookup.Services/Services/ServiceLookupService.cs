using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Service;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

public class ServiceLookupService
{
    private readonly ServiceCosmosService serviceCosmosService;

    public ServiceLookupService(ServiceCosmosService serviceCosmosService)
    {
        this.serviceCosmosService = serviceCosmosService;
    }

    public async Task<IEnumerable<FlatRateDTO>> FlatRateLookupAsync(string vds, string? wmi)
    {
        var items = await serviceCosmosService.GetFlatRatesAsycn(vds, wmi);
        var mappedItems = items.Select(x => new FlatRateDTO
        {
            id = x.id,
            LaborCode = x.LaborCode,
            VDS = x.VDS,
            Times = x.Times,
            WMI = x.WMI,
            Brand = x.Brand
        });

        if (items.Any(x => x.WMI == wmi?.ToUpper()))
            return mappedItems.Where(x => x.WMI == wmi?.ToUpper());

        return mappedItems;
    }
}
