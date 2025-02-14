using System.Collections.Generic;
using ShiftSoftware.ADP.Models.DealerData.CosmosModels;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartAggregateCosmosModel
{
    public IEnumerable<PartStockCosmosModel> StockParts { get; set; }
    public IEnumerable<CatalogPartCosmosModel> PartCatalog { get; set; }
}
