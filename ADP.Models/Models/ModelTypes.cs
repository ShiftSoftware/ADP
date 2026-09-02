namespace ShiftSoftware.ADP.Models;

public static class ModelTypes
{
    public static readonly PartitionedItemType CatalogPart = new("CatalogPart");
    public static readonly PartitionedItemType StockPart = new("StockPart");
    public static readonly PartitionedItemType StockPartFirstReceiveDate = new("StockPartFirstReceiveDate");
    public static readonly PartitionedItemType InvoicePartLine = new("OrderPartLine");
    public static readonly PartitionedItemType CompanyDeadStockPart = new("CompanyDeadStockPart");


    public static readonly PartitionedItemType InvoiceLaborLine = new("OrderLaborLine");
    public static readonly PartitionedItemType Invoice = new("Invoice");
    public static readonly PartitionedItemType BrokerInitialVehicle = new("BrokerInitialVehicle");
    public static readonly PartitionedItemType BrokerInvoice = new("BrokerInvoice");
    public static readonly PartitionedItemType BrokerVehicleTransfer = new("BrokerVehicleTransfer");
    public static readonly PartitionedItemType VehicleAccessory = new("VehicleAccessory");
    public static readonly PartitionedItemType InitialOfficialVIN = new("InitialOfficialVIN");
    public static readonly PartitionedItemType PaidServiceInvoice = new("PaidServiceInvoice");
    public static readonly PartitionedItemType PaintThicknessInspection = new("PaintThicknessInspection");
    public static readonly PartitionedItemType ItemClaim = new("ItemClaim");
    public static readonly PartitionedItemType SSCAffectedVIN = new("SSCAffectedVIN");
    public static readonly PartitionedItemType VehicleEntry = new("VehicleEntry");
    public static readonly PartitionedItemType VehicleServiceActivation = new("VehicleServiceActivation");
    public static readonly PartitionedItemType VehicleInspection = new("VehicleInspection");
    public static readonly PartitionedItemType CampaignVinEntry = new("CampaignVinEntry");
    public static readonly PartitionedItemType WarrantyClaim = new("WarrantyClaim");

    public static readonly PartitionedItemType FreeServiceItemDateShift = new("FreeServiceItemDateShift");
    public static readonly PartitionedItemType WarrantyDateShift = new("WarrantyDateShift");
    public static readonly PartitionedItemType FreeServiceItemExcludedVIN = new("FreeServiceItemExcludedVIN");
    public static readonly PartitionedItemType FreeServiceItemValidityOverride = new("FreeServiceItemValidityOverride");

    public static readonly PartitionedItemType DealerCustomer = new("DealerCustomer");
    public static readonly PartitionedItemType GoldenCustomer = new("GoldenCustomer");
    public static readonly PartitionedItemType GoldenCustomerVehicleLinks = new("GoldenCustomerVehicleLinks");
    public static readonly PartitionedItemType VehicleGoldenOwnership = new("VehicleGoldenOwnership");

    public static readonly PartitionedItemType ExtendedWarranty = new("ExtendedWarranty");

    // Service menus. These discriminate the documents of the ServiceMenus container ONLY — the root
    // variant and the three link documents that share its basic-model-code partition. The master
    // entities (service intervals, interval groups, replacement items, standalone replacement-item
    // groups, labour-rate and brand mappings) each have their own container partitioned by their own
    // id, so they need no discriminator. See ADP.Menus/COSMOS_REPLICATION_PLAN.md §16.
    public static readonly PartitionedItemType MenuVariant = new("MenuVariant");
    public static readonly PartitionedItemType MenuPeriod = new("MenuPeriod");
    public static readonly PartitionedItemType MenuLabour = new("MenuLabour");
    public static readonly PartitionedItemType MenuItem = new("MenuItem");
}