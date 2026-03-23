using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Models.TBP;

namespace LookupServices.BDD.Support;

public class TestEnvironment
{
    public LookupOptions LookupOptions { get; set; } = new();
    public List<TestCompany> Companies { get; set; } = new();
    public Dictionary<string, CompanyDataAggregateModel> Vehicles { get; set; } = new();
    public List<BrokerInitialVehicleModel> BrokerInitialVehicles { get; set; } = new();
    public List<BrokerInvoiceModel> BrokerInvoices { get; set; } = new();
}

public class TestCompany
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public List<TestBranch> Branches { get; set; } = new();
}

public class TestBranch
{
    public long BranchId { get; set; }
    public string BranchName { get; set; } = "";
}
