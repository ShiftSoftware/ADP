using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Menus.Shared.ActionTrees;

[ActionTree("Menu", "Menu Module Permissions")]
public class MenuActionTree
{
    public readonly static ReadWriteDeleteAction Menus = new("Menus");
    public readonly static ReadWriteDeleteAction MenuVariants = new("Menu Variants");
    public readonly static ReadWriteDeleteAction MenuVersions = new("Menu Versions");
    public readonly static ReadWriteDeleteAction VehicleModels = new("Vehicle Models");
    public readonly static ReadWriteDeleteAction ReplacementItems = new("Replacement Items");
    public readonly static ReadWriteDeleteAction ServiceIntervals = new("Service Intervals");
    public readonly static ReadWriteDeleteAction ServiceIntervalGroups = new("Service Interval Groups");
    public readonly static ReadWriteDeleteAction BrandMappings = new("Brand Mappings");
    public readonly static ReadWriteDeleteAction LabourRateMappings = new("Labour Rate Mappings");
    public readonly static ReadWriteDeleteAction StandaloneReplacementItemGroups = new("Standalone Replacement Item Groups");

    [ActionTree("Menu Operations", "Special Menu Operations")]
    public class Operations
    {
        public readonly static BooleanAction CreateMenuVersion = new("Create Menu Version");
        public readonly static BooleanAction UpdatePartsPrice = new("Update Parts Price For All Menus");
    }

    [ActionTree("Menu Reports", "Menu Report Exports")]
    public class Reports
    {
        public readonly static BooleanAction ExportMenusToExcel = new("Export Menus To Excel");
        public readonly static BooleanAction ExportRTSCodesToExcel = new("Export RTS Codes To Excel");
        public readonly static BooleanAction ExportMenuDetailReport = new("Export Menu Detail Report");
    }
}
