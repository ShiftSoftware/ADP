using System.Collections.Generic;
using ShiftSoftware.ADP.Models.Part;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class PartAggregateCosmosModel
{
    public IEnumerable<StockPartModel> StockParts { get; set; }
    public IEnumerable<CatalogPartModel> CatalogParts { get; set; }
}