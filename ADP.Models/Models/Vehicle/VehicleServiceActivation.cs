namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleServiceActivation : VehicleBase, IPartitionedItem, IBrandProps, ICompanyProps, IBranchProps, IInvoiceProps
{
    public string CustomerName { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public string CustomerEmail { get; set; }
    public bool IsDeleted { get; set; }
    public string ItemType => ModelTypes.VehicleServiceActivation;
}