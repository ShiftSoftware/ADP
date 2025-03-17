using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleEntryModel : VehicleBase, IPartitionedItem, IBrandProps, ICompanyProps, IRegionProps, IBranchProps, IInvoiceLineProps, IInvoiceProps
{
    public string LineStatus { get; set; }
    public string LineID { get; set; }
    public DateTime? LoadDate { get; set; }
    public DateTime? PostDate { get; set; }
    public string ItemType => ModelTypes.VehicleEntry;
}