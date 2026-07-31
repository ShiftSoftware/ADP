using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using ShiftSoftware.ADP.Models.TBP;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Models.Constants;

public class NoSQLConstants
{
    public class Databases
    {
        public const string CompanyData = "CompanyData";
        public const string Logs = "Logs";
        public const string Services = "Services";
        public const string TBP = "TBP";
        public const string Customers = "Customers";
    }

    public class Containers
    {
        public const string Brokers = "Brokers";
        public const string Vehicles = "Vehicles";
        public const string Parts = "Parts";
        //public const string Stock = "Stock";
        public const string ClaimableItemCampaigns = "ClaimableItemCampaigns";
        public const string ServiceItems = "ServiceItems";
        public const string ExteriorColors = "ExteriorColors";
        public const string InteriorColors = "InteriorColors";
        public const string VehicleModels = "VehicleModels";

        /// <summary>
        /// Service menus, replicated per row from the menus database. Hierarchical partition key: the
        /// basic model code, then the item type — so one single-partition query returns a whole model's
        /// menu graph, fully denormalized. See ADP.Menus/COSMOS_REPLICATION_PLAN.md §16.
        /// </summary>
        public const string ServiceMenus = "ServiceMenus";

        // The menu catalog's master entities. Each gets its own container partitioned by its own id,
        // rather than being forced into the ServiceMenus partition scheme with a key it does not have.
        // Their fields are denormalized into the ServiceMenus documents, so the lookup reads these only
        // for maintenance/backfill — never on the read path.
        public const string ServiceIntervals = "ServiceIntervals";
        public const string ServiceIntervalGroups = "ServiceIntervalGroups";
        public const string ReplacementItems = "ReplacementItems";
        public const string StandaloneReplacementItemGroups = "StandaloneReplacementItemGroups";
        public const string LabourRateMappings = "LabourRateMappings";
        public const string BrandMappings = "BrandMappings";

        public const string PartLookupLogs = "PartLookup";
        public const string ManufacturerPartLookupLogs = "ManufacturerPartLookup";
        public const string SSCLogs = "SSC";
        public const string CSVUpload = "CSVUpload";

        public const string FlatRate = "FlatRate";

        public const string TBP_BrokerStock = "BrokerStock";

        public const string Customers_Customers = "Customers";
    }

    public class PartitionKeys
    {
        public class Vehicles
        {
            public const string Level1 = "/" + nameof(VehicleEntryModel.VIN);
            public const string Level2 = "/" + nameof(VehicleEntryModel.ItemType);
            public const string Level3 = "/" + nameof(VehicleEntryModel.CompanyID);
        }

        public class Parts
        {
            public const string Level1 = "/" + nameof(CatalogPartModel.PartNumber);
            public const string Level2 = "/" + nameof(CatalogPartModel.ItemType);
            public const string Level3 = "/" + nameof(CatalogPartModel.Location);
        }

        public class ExteriorColors
        {
            public const string Level1 = "/" + nameof(ColorModel.Code);
            public const string Level2 = "/" + nameof(ColorModel.BrandID);
        }

        public class InteriorColors
        {
            public const string Level1 = "/" + nameof(ColorModel.Code);
            public const string Level2 = "/" + nameof(ColorModel.BrandID);
        }

        public class VehicleModels
        {
            public const string Level1 = "/" + nameof(VehicleModelModel.VariantCode);
            public const string Level2 = "/" + nameof(VehicleModelModel.BrandID);
        }

        //public class Stock
        //{
        //    public const string Level1 = "/" + nameof(StockPartModel.PartNumber);
        //    public const string Level2 = "/" + nameof(StockPartModel.Location);
        //}

        /// <summary>
        /// Partition-key paths for <see cref="Containers.ServiceMenus"/>. Every document in it — the
        /// root variant and its sibling link documents — carries a real basic model code.
        /// </summary>
        public class ServiceMenus
        {
            public const string Level1 = "/" + nameof(MenuVariantCosmosModel.BasicModelCode);
            public const string Level2 = "/" + nameof(MenuVariantCosmosModel.ItemType);
        }

        // The menu catalog's master containers. One document type each, partitioned by its own id —
        // the same shape the Services database already uses for ServiceItems and ClaimableItemCampaigns.

        public class ServiceIntervals
        {
            public const string Level1 = "/" + nameof(ServiceIntervalCosmosModel.id);
        }

        public class ServiceIntervalGroups
        {
            public const string Level1 = "/" + nameof(ServiceIntervalGroupCosmosModel.id);
        }

        public class ReplacementItems
        {
            public const string Level1 = "/" + nameof(ReplacementItemCosmosModel.id);
        }

        public class StandaloneReplacementItemGroups
        {
            public const string Level1 = "/" + nameof(StandaloneReplacementItemGroupCosmosModel.id);
        }

        public class LabourRateMappings
        {
            public const string Level1 = "/" + nameof(LabourRateMappingCosmosModel.id);
        }

        public class BrandMappings
        {
            public const string Level1 = "/" + nameof(BrandMappingCosmosModel.id);
        }

        public class PartLookupLogs
        {
            public const string Level1 = "/" + nameof(CatalogPartModel.PartNumber);
        }

        public class ManufacturerPartLookupLogs
        {
            public const string Level1 = "/" + nameof(CatalogPartModel.PartNumber);
        }

        public class SSCLogs
        {
            public const string Level1 = "/" + nameof(VehicleEntryModel.VIN);
        }

        public class FlatRate
        {
            public const string Level1 = "/" + nameof(FlatRateModel.VDS);
            public const string Level2 = "/" + nameof(FlatRateModel.WMI);
        }

        public class Customers
        {
            public const string Level1 = "/" + nameof(CustomerModel.CompanyID);
            public const string Level2 = "/" + nameof(CustomerModel.CustomerID);
            public const string Level3 = "/" + nameof(CustomerModel.ItemType);
        }

        public class TBPBrokerStock
        {
            public const string Level1 = "/" + nameof(TBP_StockModel.BrandID);
            public const string Level2 = "/" + nameof(TBP_StockModel.BrokerID);
            public const string Level3 = "/" + nameof(TBP_StockModel.VIN);
        }
    }
}