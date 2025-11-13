using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

internal class PartDeadStockEvaluator
{
    private readonly PartAggregateCosmosModel partDataAggregate;
    private readonly LookupOptions options;
    private readonly IServiceProvider services;

    public PartDeadStockEvaluator(PartAggregateCosmosModel partDataAggregate, LookupOptions options, IServiceProvider services)
    {
        this.partDataAggregate = partDataAggregate;
        this.options = options;
        this.services = services;
    }

    public async Task<IEnumerable<DeadStockDTO>> Evaluate(string language = "en")
    {
        List<DeadStockDTO> deadStockParts = new();

        foreach (var item in partDataAggregate.CompanyDeadStockParts.GroupBy(x => x.CompanyID))
        {
            var deadStock = new DeadStockDTO
            {
                CompanyHashID = item.FirstOrDefault()?.CompanyHashID,
            };

            if (options?.CompanyNameResolver is not null)
                deadStock.CompanyName = await options.CompanyNameResolver(new LookupOptionResolverModel<long?>(item.Key, language, services));

            foreach (var i in item)
            {
                var branchDeadStock = new BranchDeadStockDTO
                {
                    CompanyBranchHashID = i.BranchHashID,
                    Quantity = i.AvailableQuantity,
                };

                if (options?.CompanyBranchNameResolver is not null)
                    branchDeadStock.CompanyBranchName = await options.CompanyBranchNameResolver(new LookupOptionResolverModel<long?>(i.BranchID, language, services));

                deadStock.BranchDeadStock = deadStock.BranchDeadStock.Append(branchDeadStock);
            }

            deadStockParts.Add(deadStock);
        }

        return deadStockParts;
    }
}
