namespace ShiftSoftware.ADP.Models;

internal interface IBranchProps: ICompanyProps
{
    public string BranchID { get; set; }
    public string BranchHashID { get; set; }
}