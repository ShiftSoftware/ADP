namespace ShiftSoftware.ADP.Models;

public static class ModelTypes
{
    public static readonly PartitionedItemType CatalogPart = new("CatalogPart");
    public static readonly PartitionedItemType StockPart = new("StockPart");
    public static readonly PartitionedItemType InvoicePartLine = new("InvoicePartLine");
    public static readonly PartitionedItemType InvoiceLaborLine = new("InvoiceLaborLine");
    public static readonly PartitionedItemType Invoice = new("Invoice");
    public static readonly PartitionedItemType BrokerInitialVehicle = new("BrokerInitialVehicle");
    public static readonly PartitionedItemType BrokerInvoice = new("BrokerInvoice");
    public static readonly PartitionedItemType VehicleAccessory = new("VehicleAccessory");
    public static readonly PartitionedItemType InitialOfficialVIN = new("InitialOfficialVIN");
    public static readonly PartitionedItemType PaidServiceInvoice = new("PaidServiceInvoice");
    public static readonly PartitionedItemType PaintThicknessInspection = new("PaintThicknessInspection");
    public static readonly PartitionedItemType ServiceItemClaimLine = new("ServiceItemClaimLine");
    public static readonly PartitionedItemType SSCAffectedVIN = new("SSCAffectedVIN");
    public static readonly PartitionedItemType VehicleEntry = new("VehicleEntry");
    public static readonly PartitionedItemType WarrantyClaim = new("WarrantyClaim");

    public static readonly PartitionedItemType FreeServiceItemDateShift = new("FreeServiceItemDateShift");
    public static readonly PartitionedItemType WarrantyDateShift = new("WarrantyDateShift");
    public static readonly PartitionedItemType FreeServiceItemExcludedVIN = new("FreeServiceItemExcludedVIN");
}