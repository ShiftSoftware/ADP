using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;
using DuckDBConnection = global::DuckDB.NET.Data.DuckDBConnection;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// The reference data the evaluators ask the storage for while evaluating ONE vehicle — service
/// items, vehicle models, colours, broker stock, the customers the entries name — loaded once and
/// answered from memory, so no evaluation worker ever touches a DuckDB connection (D8). The
/// aggregate itself is not served here: the stream supplies it, and asking for one is a bug.
///
/// <para>Every lookup keeps the exact matching rule of <c>DuckDBVehicleLookupStorageService</c>
/// (trimmed colour codes, exact customer id and company, broker stock by normalized VIN then
/// brand) so the two paths answer alike.</para>
/// </summary>
public sealed class PreloadedReferenceStorage : IVehicleLookupStorageService
{
    private readonly List<ServiceItemModel> serviceItems;
    private readonly List<VehicleModelModel> vehicleModels;
    private readonly Dictionary<(string Variant, long? Brand), VehicleModelModel> modelByVariantAndBrand;
    private readonly Dictionary<(string Code, long? Brand), ColorModel> exteriorColors;
    private readonly Dictionary<(string Code, long? Brand), ColorModel> interiorColors;
    private readonly Dictionary<string, List<TBP_StockModel>> brokerStockByVin;
    private readonly Dictionary<(string CustomerId, long? CompanyId), CustomerModel> customers;

    public sealed class Options
    {
        /// <summary>Load broker stock. Off for a client without brokers, whose source has no such table.</summary>
        public bool LoadBrokerStock { get; set; } = true;
        /// <summary>Load the customers the vehicle entries name (never the whole customer table).</summary>
        public bool LoadCustomers { get; set; } = true;

        // The SELECT per reference table. The defaults are a read snapshot's bare tables, as the
        // per-VIN storage names them; a source binding (BulkLookupSource) points them at its own
        // relations — a serving table under a schema, the published parquet of one — with its
        // live-row predicate. The matching rules below never change with the source.
        public string ServiceItemsSql { get; set; } = "SELECT * FROM ServiceItem";
        public string VehicleModelsSql { get; set; } = "SELECT * FROM VehicleModel";
        public string ExteriorColorsSql { get; set; } = "SELECT * FROM ExteriorColor";
        public string InteriorColorsSql { get; set; } = "SELECT * FROM InteriorColor";
        public string BrokerStockSql { get; set; } = "SELECT * FROM TBP_BrokerStock";
        public string CustomersSql { get; set; } = "SELECT c.* FROM Customer c WHERE c.CustomerID IN (SELECT DISTINCT CustomerID FROM VehicleEntry WHERE CustomerID IS NOT NULL)";
    }

    public sealed class LoadReport
    {
        public int ServiceItems { get; set; }
        public int VehicleModels { get; set; }
        public int ExteriorColors { get; set; }
        public int InteriorColors { get; set; }
        public int BrokerStockRows { get; set; }
        public int Customers { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    private PreloadedReferenceStorage(
        List<ServiceItemModel> serviceItems,
        List<VehicleModelModel> vehicleModels,
        Dictionary<(string, long?), ColorModel> exteriorColors,
        Dictionary<(string, long?), ColorModel> interiorColors,
        Dictionary<string, List<TBP_StockModel>> brokerStockByVin,
        Dictionary<(string, long?), CustomerModel> customers)
    {
        this.serviceItems = serviceItems;
        this.vehicleModels = vehicleModels;
        this.exteriorColors = exteriorColors;
        this.interiorColors = interiorColors;
        this.brokerStockByVin = brokerStockByVin;
        this.customers = customers;
        modelByVariantAndBrand = new Dictionary<(string, long?), VehicleModelModel>();
        foreach (var model in vehicleModels)
        {
            var key = (model.VariantCode, model.BrandID);
            if (model.VariantCode is not null && !modelByVariantAndBrand.ContainsKey(key))
                modelByVariantAndBrand[key] = model;
        }
    }

    public LoadReport Report { get; private set; }

    public static PreloadedReferenceStorage Load(string connectionString, Options options = null)
    {
        options ??= new Options();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        using var connection = new DuckDBConnection(connectionString);
        connection.Open();

        var serviceItems = Read<ServiceItemModel>(connection, options.ServiceItemsSql);
        var models = Read<VehicleModelModel>(connection, options.VehicleModelsSql);
        var exterior = Read<ColorModel>(connection, options.ExteriorColorsSql);
        var interior = Read<ColorModel>(connection, options.InteriorColorsSql);
        var stock = options.LoadBrokerStock ? Read<TBP_StockModel>(connection, options.BrokerStockSql) : new List<TBP_StockModel>();
        var customers = options.LoadCustomers ? Read<CustomerModel>(connection, options.CustomersSql) : new List<CustomerModel>();

        var exteriorByKey = new Dictionary<(string, long?), ColorModel>();
        foreach (var color in exterior)
        {
            var key = (color.Code?.Trim(), color.BrandID);
            if (key.Item1 is not null && !exteriorByKey.ContainsKey(key)) exteriorByKey[key] = color;
        }
        var interiorByKey = new Dictionary<(string, long?), ColorModel>();
        foreach (var color in interior)
        {
            var key = (color.Code?.Trim(), color.BrandID);
            if (key.Item1 is not null && !interiorByKey.ContainsKey(key)) interiorByKey[key] = color;
        }
        // The per-VIN storage selects stock by the row's VIN as stored against the normalized request
        // VIN, so only a row already in canonical form ever matches a vehicle. The same rule here.
        var stockByVin = stock
            .Where(row => !string.IsNullOrWhiteSpace(row.VIN) && string.Equals(row.VIN, VinOrderedFamilyReader.Normalize(row.VIN), StringComparison.Ordinal))
            .GroupBy(row => row.VIN, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var customerByKey = new Dictionary<(string, long?), CustomerModel>();
        foreach (var customer in customers)
        {
            var key = (customer.CustomerID, customer.CompanyID);
            if (customer.CustomerID is not null && !customerByKey.ContainsKey(key)) customerByKey[key] = customer;
        }

        clock.Stop();
        return new PreloadedReferenceStorage(serviceItems, models, exteriorByKey, interiorByKey, stockByVin, customerByKey)
        {
            Report = new LoadReport
            {
                ServiceItems = serviceItems.Count,
                VehicleModels = models.Count,
                ExteriorColors = exterior.Count,
                InteriorColors = interior.Count,
                BrokerStockRows = stock.Count,
                Customers = customers.Count,
                Elapsed = clock.Elapsed,
            },
        };
    }

    /// <summary>
    /// A view for one evaluation worker: the read-only indexes are shared, the service-item list is
    /// the worker's own copy, because that is the one reference collection an evaluator holds across
    /// a whole vehicle and the one whose objects a rule could conceivably touch.
    /// </summary>
    public PreloadedReferenceStorage ForWorker()
    {
        var copy = JsonSerializer.Deserialize<List<ServiceItemModel>>(JsonSerializer.Serialize(serviceItems)) ?? new List<ServiceItemModel>();
        return new PreloadedReferenceStorage(copy, vehicleModels, exteriorColors, interiorColors, brokerStockByVin, customers) { Report = Report };
    }

    private static List<T> Read<T>(DuckDBConnection connection, string sql) where T : new()
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var mapper = DuckDBModelMapper<T>.For(reader);
        var rows = new List<T>();
        while (reader.Read())
            rows.Add(mapper.Read(reader));
        return rows;
    }

    // ---- reference lookups -----------------------------------------------------------------

    public Task<IEnumerable<ServiceItemModel>> GetServiceItemsAsync(bool useCache = true) =>
        Task.FromResult<IEnumerable<ServiceItemModel>>(serviceItems);

    public Task<VehicleModelModel> GetVehicleModelsAsync(string variant, long? brand)
    {
        if (string.IsNullOrWhiteSpace(variant))
            return Task.FromResult<VehicleModelModel>(null);
        modelByVariantAndBrand.TryGetValue((variant, brand), out var model);
        return Task.FromResult(model);
    }

    public Task<IEnumerable<VehicleModelModel>> GetAllVehicleModelsAsync() =>
        Task.FromResult<IEnumerable<VehicleModelModel>>(vehicleModels);

    public Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByKatashikiAsync(string katashiki) =>
        Task.FromResult<IEnumerable<VehicleModelModel>>(vehicleModels.Where(m => string.Equals(m.Katashiki, katashiki, StringComparison.Ordinal)).ToList());

    public Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVariantAsync(string variant) =>
        Task.FromResult<IEnumerable<VehicleModelModel>>(vehicleModels.Where(m => string.Equals(m.VariantCode, variant, StringComparison.Ordinal)).ToList());

    public Task<ColorModel> GetExteriorColorsAsync(string colorCode, long? brand)
    {
        if (string.IsNullOrWhiteSpace(colorCode))
            return Task.FromResult<ColorModel>(null);
        exteriorColors.TryGetValue((colorCode.Trim(), brand), out var color);
        return Task.FromResult(color);
    }

    public Task<ColorModel> GetInteriorColorsAsync(string trimCode, long? brand)
    {
        if (string.IsNullOrWhiteSpace(trimCode))
            return Task.FromResult<ColorModel>(null);
        interiorColors.TryGetValue((trimCode.Trim(), brand), out var color);
        return Task.FromResult(color);
    }

    public Task<IEnumerable<TBP_StockModel>> GetBrokerStockAsync(long? brandId, string vin)
    {
        var key = VinOrderedFamilyReader.Normalize(vin);
        if (string.IsNullOrEmpty(key) || !brokerStockByVin.TryGetValue(key, out var rows))
            return Task.FromResult(Enumerable.Empty<TBP_StockModel>());
        IEnumerable<TBP_StockModel> result = brandId is null ? rows : rows.Where(row => row.BrandID == brandId).ToList();
        return Task.FromResult(result);
    }

    public Task<CustomerModel> GetCustomerAsync(string customerID, long? companyID)
    {
        if (customerID is null)
            return Task.FromResult<CustomerModel>(null);
        customers.TryGetValue((customerID, companyID), out var customer);
        return Task.FromResult(customer);
    }

    // The per-VIN path reads brokers from a table no snapshot to date has carried; the evaluators that
    // would ask are commented out. Answering null is what the storage has always effectively done.
    public Task<BrokerModel> GetBrokerAsync(string accountNumber, long? companyID) => Task.FromResult<BrokerModel>(null);
    public Task<BrokerModel> GetBrokerAsync(long id) => Task.FromResult<BrokerModel>(null);

    // ---- not served here: the stream supplies aggregates -----------------------------------

    public Task<CompanyDataAggregateModel> GetAggregatedCompanyData(string vin) => throw AggregatesAreStreamed();
    public Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyData(IEnumerable<string> vins, IEnumerable<string> itemTypes) => throw AggregatesAreStreamed();
    public Task<IEnumerable<CompanyDataAggregateModel>> GetAggregatedCompanyDataForBulkLookupAsync(IEnumerable<string> vins) => throw AggregatesAreStreamed();
    public Task<IEnumerable<VehicleModelModel>> GetVehicleModelsByVinAsync(string vin) => throw AggregatesAreStreamed();

    private static NotSupportedException AggregatesAreStreamed() =>
        new NotSupportedException("PreloadedReferenceStorage serves reference data only; aggregates come from VinOrderedAggregateStream. Evaluate through VehicleLookupService.LookupAsync(aggregate, ...).");
}
