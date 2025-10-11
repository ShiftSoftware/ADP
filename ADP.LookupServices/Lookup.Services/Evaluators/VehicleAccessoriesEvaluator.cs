using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleAccessoriesEvaluator
{
    private readonly CompanyDataAggregateCosmosModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;

    public VehicleAccessoriesEvaluator(CompanyDataAggregateCosmosModel companyDataAggregate, LookupOptions options, IServiceProvider ServiceProvider)
    {
        this.CompanyDataAggregate = companyDataAggregate;
        this.Options = options;
        this.ServiceProvider = ServiceProvider;
    }

    public async Task<IEnumerable<AccessoryDTO>> Evaluate(string languageCode)
    {
        var tasks = CompanyDataAggregate.Accessories.Select(async accessory =>
        {
            var image = await GetAccessoryImageFullUrl(accessory.Image, languageCode);

            return new AccessoryDTO
            {
                PartNumber = accessory.PartNumber,
                Description = accessory.PartDescription,
                Image = image
            };
        });

        return await Task.WhenAll(tasks);
    }

    private async Task<string> GetAccessoryImageFullUrl(string image, string languageCode)
    {
        if (Options?.AccessoryImageUrlResolver is not null)
            return await Options.AccessoryImageUrlResolver(new(image, languageCode, ServiceProvider));

        return image;
    }
}