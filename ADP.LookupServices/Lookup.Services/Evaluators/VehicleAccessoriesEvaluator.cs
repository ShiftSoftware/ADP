using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

public class VehicleAccessoriesEvaluator
{
    private readonly CompanyDataAggregateModel CompanyDataAggregate;
    private readonly LookupOptions Options;
    private readonly IServiceProvider ServiceProvider;

    public VehicleAccessoriesEvaluator(CompanyDataAggregateModel companyDataAggregate, LookupOptions options, IServiceProvider ServiceProvider)
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
                Image = image,
                // 0 is the source's "no value".
                JobNumber = accessory.JobNumber == 0 ? null : accessory.JobNumber,
                InvoiceNumber = accessory.InvoiceNumber == 0 ? null : accessory.InvoiceNumber
            };
        });

        var dtos = await Task.WhenAll(tasks);

        // Host resolvers are not thread-safe (scoped DbContext), so resolve one company at a time, once each.
        if (Options?.CompanyNameResolver is not null)
        {
            var accessories = CompanyDataAggregate.Accessories.ToList();
            var names = new Dictionary<long, string>();
            string nullCompanyName = null;
            var nullResolved = false;

            for (var i = 0; i < dtos.Length; i++)
            {
                var companyId = accessories[i].CompanyID;

                if (companyId is null)
                {
                    if (!nullResolved)
                    {
                        nullCompanyName = await Options.CompanyNameResolver(new(companyId, languageCode, ServiceProvider));
                        nullResolved = true;
                    }
                    dtos[i].CompanyName = nullCompanyName;
                    continue;
                }

                if (!names.TryGetValue(companyId.Value, out var name))
                {
                    name = await Options.CompanyNameResolver(new(companyId, languageCode, ServiceProvider));
                    names[companyId.Value] = name;
                }
                dtos[i].CompanyName = name;
            }
        }

        return dtos;
    }

    private async Task<string> GetAccessoryImageFullUrl(string image, string languageCode)
    {
        if (Options?.AccessoryImageUrlResolver is not null)
            return await Options.AccessoryImageUrlResolver(new(image, languageCode, ServiceProvider));

        return image;
    }
}