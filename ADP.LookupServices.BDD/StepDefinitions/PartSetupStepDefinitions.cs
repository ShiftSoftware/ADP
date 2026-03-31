using LookupServices.BDD.Support;
using Reqnroll;
using ShiftSoftware.ADP.Models.Part;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class PartSetupStepDefinitions
{
    private readonly TestContext _context;

    public PartSetupStepDefinitions(TestContext context)
    {
        _context = context;
    }

    private static string? GetOptionalString(DataTableRow row, string column)
    {
        if (!row.ContainsKey(column))
            return null;

        var value = row[column];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static long? GetOptionalLong(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : long.Parse(value);
    }

    private static decimal? GetOptionalDecimal(DataTableRow row, string column)
    {
        var value = GetOptionalString(row, column);
        return value is null ? null : decimal.Parse(value);
    }

    [Given("catalog data for part {string}:")]
    public void GivenCatalogDataForPart(string partNumber, DataTable dataTable)
    {
        var catalogPart = new CatalogPartModel
        {
            PartNumber = partNumber,
            CountryData = new List<PartCountryDataModel>()
        };

        foreach (var row in dataTable.Rows)
        {
            var field = row["Field"];
            var value = row["Value"];

            switch (field)
            {
                case "PartName":
                    catalogPart.PartName = value;
                    break;
                case "BrandID":
                    // CatalogPartModel doesn't have BrandID — skip
                    break;
                case "DistributorPurchasePrice":
                    catalogPart.DistributorPurchasePrice = decimal.Parse(value);
                    break;
                case "ProductGroup":
                    catalogPart.ProductGroup = value;
                    break;
            }
        }

        _context.PartAggregate.CatalogParts =
            (_context.PartAggregate.CatalogParts ?? Enumerable.Empty<CatalogPartModel>())
            .Append(catalogPart);
    }

    [Given("catalog part {string} with distributor price {decimal}")]
    public void GivenCatalogPartWithDistributorPrice(string partNumber, decimal price)
    {
        _context.PartAggregate.CatalogParts =
            (_context.PartAggregate.CatalogParts ?? Enumerable.Empty<CatalogPartModel>())
            .Append(new CatalogPartModel
            {
                PartNumber = partNumber,
                DistributorPurchasePrice = price,
                CountryData = new List<PartCountryDataModel>()
            });
    }

    [Given("catalog part {string} has country {long} with region prices:")]
    public void GivenCatalogPartHasCountryWithRegionPrices(string partNumber, long countryId, DataTable dataTable)
    {
        var catalogPart = _context.PartAggregate.CatalogParts?.FirstOrDefault(p => p.PartNumber == partNumber);
        if (catalogPart is null)
            throw new InvalidOperationException($"Catalog part '{partNumber}' not found. Add it with 'Given catalog part' first.");

        var regionPrices = dataTable.Rows.Select(row => new RegionPriceModel
        {
            RegionID = GetOptionalLong(row, "RegionID"),
            RetailPrice = GetOptionalDecimal(row, "RetailPrice"),
            PurchasePrice = GetOptionalDecimal(row, "PurchasePrice"),
            WarrantyPrice = GetOptionalDecimal(row, "WarrantyPrice"),
        }).ToList();

        var countryData = new PartCountryDataModel
        {
            CountryID = countryId,
            RegionPrices = regionPrices
        };

        catalogPart.CountryData = catalogPart.CountryData.Append(countryData);
    }

    [Given("stock for part {string}:")]
    public void GivenStockForPart(string partNumber, DataTable dataTable)
    {
        var stockParts = dataTable.Rows.Select(row => new StockPartModel
        {
            PartNumber = partNumber,
            Location = GetOptionalString(row, "Location") ?? "",
            AvailableQuantity = GetOptionalDecimal(row, "AvailableQuantity"),
            CompanyID = GetOptionalLong(row, "CompanyID"),
            CompanyHashID = GetOptionalString(row, "CompanyHashID"),
        });

        _context.PartAggregate.StockParts =
            (_context.PartAggregate.StockParts ?? Enumerable.Empty<StockPartModel>())
            .Concat(stockParts);
    }

    [Given("dead stock for part {string}:")]
    public void GivenDeadStockForPart(string partNumber, DataTable dataTable)
    {
        var deadStockParts = dataTable.Rows.Select(row => new CompanyDeadStockPartModel
        {
            PartNumber = partNumber,
            CompanyID = GetOptionalLong(row, "CompanyID"),
            CompanyHashID = GetOptionalString(row, "CompanyHashID"),
            BranchID = GetOptionalLong(row, "BranchID"),
            BranchHashID = GetOptionalString(row, "BranchHashID"),
            AvailableQuantity = decimal.Parse(row["AvailableQuantity"]),
        });

        _context.PartAggregate.CompanyDeadStockParts =
            (_context.PartAggregate.CompanyDeadStockParts ?? Enumerable.Empty<CompanyDeadStockPartModel>())
            .Concat(deadStockParts);
    }
}
