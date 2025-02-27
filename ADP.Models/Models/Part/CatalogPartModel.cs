using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CatalogPartModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string PartNumber { get; set; } = default!;
    public string PartName { get; set; } = default!;
    public string ProductGroup { get; set; } = default!;
    public string ProductGroupDescription { get; set; }
    public string BinType { get; set; }
    public decimal? DistributorPurchasePrice { get; set; }
    public string ProductCode { get; set; }
    public string PNC { get; set; }
    public decimal? Dimension1 { get; set; }
    public decimal? Dimension2 { get; set; }
    public decimal? Dimension3 { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? CubicMeasure { get; set; }
    public decimal? GrossWeight { get; set; }
    public string Origin { get; set; }
    public IEnumerable<PartSupersession> SupersededTo { get; set; }
    public string LocalDescription { get; set; }
    public string HSCode { get; set; }
    public IEnumerable<CountryDataModel> CountryData { get; set; }
    public string ItemType => ModelTypes.CatalogPart;
}

public class PartSupersession
{
    public string PartNumber { get; set; } = default!;
    public int? SupersessionCode { get; set; } = default!;
}