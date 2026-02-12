using ShiftSoftware.ADP.Models.Customer;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;
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