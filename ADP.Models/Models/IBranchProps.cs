namespace ShiftSoftware.ADP.Models;

internal interface IBranchProps: ICompanyProps
{
    public string BranchIntegrationID { get; set; }
}