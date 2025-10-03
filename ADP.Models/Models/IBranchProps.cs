namespace ShiftSoftware.ADP.Models;

internal interface IBranchProps: ICompanyProps
{
    public long? BranchID { get; set; }
    public string BranchHashID { get; set; }
}