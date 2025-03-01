using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Vehicle;

namespace ShiftSoftware.ADP.Models.Constants;

public class NoSQLConstants
{
    public class Databases
    {
        public const string CompanyData = "CompanyData";
        public const string Logs = "Logs";
    }

    public class Containers
    {
        public const string Brokers = "Brokers";
        public const string Customers = "Customers";
        public const string Vehicles = "Vehicles";
        public const string Parts = "Parts";
        //public const string Stock = "Stock";
        public const string ServiceItems = "ServiceItems";
        public const string ExteriorColors = "ExteriorColors";
        public const string InteriorColors = "InteriorColors";
        public const string VehicleModels = "VehicleModels";

        public const string PartLookupLogs = "PartLookup";
    }

    public class PartitionKeys
    {
        public class Vehicles
        {
            public const string Level1 = "/" + nameof(VehicleEntryModel.VIN);
            public const string Level2 = "/" + nameof(VehicleEntryModel.ItemType);
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
            public const string Level2 = "/" + nameof(ColorModel.Brand);
        }

        public class InteriorColors
        {
            public const string Level1 = "/" + nameof(ColorModel.Code);
            public const string Level2 = "/" + nameof(ColorModel.Brand);
        }

        public class VehicleModels
        {
            public const string Level1 = "/" + nameof(VehicleModelModel.VariantCode);
            public const string Level2 = "/" + nameof(VehicleModelModel.Brand);
        }

        //public class Stock
        //{
        //    public const string Level1 = "/" + nameof(StockPartModel.PartNumber);
        //    public const string Level2 = "/" + nameof(StockPartModel.Location);
        //}
    }
}