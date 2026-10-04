using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.Milestones;
using ShiftSoftware.ADP.Models.Service;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

public partial class VehicleServiceItemStepDefinitions
{
    private ServiceItemTraceCollector? _historicalTrace;

    [Given("the free service start is unknown")]
    public void GivenUnknownStart() => _freeServiceStartDate = null;

    [Then("service item {string} has historical window {string} through {string}")]
    public void ThenHistoricalWindow(string id, string start, string end)
    {
        var item = RequireItem(id);
        if (end == "-") { Assert.Null(item.ExpiresAt); return; }
        Assert.Equal(DateTime.Parse(start), item.ActivatedAt);
        Assert.Equal(DateTime.Parse(start), item.UnlockedOn);
        Assert.Equal(DateTime.Parse(end), item.ExpiresAt);
    }

    [Then("service item {string} retains claim {string} on {string}")]
    public void ThenClaim(string id, string invoice, string date)
    {
        var item = RequireItem(id);
        Assert.Equal(invoice, item.InvoiceNumber);
        Assert.Equal(DateTime.Parse(date), item.ClaimDate?.UtcDateTime);
    }

    [Then("service item {string} retains claimed cost {int}")]
    public void ThenClaimedCost(string id, int cost) => Assert.Equal((decimal)cost, RequireItem(id).Cost);

    [Given("reward {string} uses historical visits before {string} with {string}")]
    public void GivenHistoricalVisits(string item, string cutoff, string mode) =>
        _context.Options.ServiceRewardTolerance = new()
        {
            ServiceItemIDs = [item], StartsBefore = DateTime.Parse(cutoff), TerminalPrograms = ["ALT"],
            HighPackageBehavior = Enum.Parse<RewardHighPackageBehavior>(mode),
            IsAdditionalQualifyingWork = line => line.LaborCode == "OIL-CHANGE",
        };

    [Given(@"historical jobs from sources {long}\/{long} and {long}\/{long} are confirmed mirrors")]
    public void GivenMirroredSources(long companyA, long branchA, long companyB, long branchB)
    {
        // Synthetic host convention: confirmation requires the full invoice/work multiset.
        // This exercises ADP's duplicate hook, independently of any private host implementation.
        _context.Options.ServiceRewardTolerance!.AreDuplicateJobs = (left, right) =>
        {
            var a = left[0];
            var b = right[0];
            if (!((a.CompanyID == companyA && a.BranchID == branchA && b.CompanyID == companyB && b.BranchID == branchB) ||
                  (b.CompanyID == companyA && b.BranchID == branchA && a.CompanyID == companyB && a.BranchID == branchB)) ||
                a.VIN != b.VIN || a.OrderDocumentNumber != b.OrderDocumentNumber || left.Count != right.Count)
                return false;
            static object Key(OrderLaborLineModel l) => (l.InvoiceNumber, l.InvoiceDate, l.PackageCode, l.LaborCode, l.ServiceCode);
            var counts = right.GroupBy(Key).ToDictionary(g => g.Key, g => g.Count());
            return left.GroupBy(Key).All(g => counts.TryGetValue(g.Key, out var n) && n == g.Count());
        };
    }

    [Then("service item {string} has reward outcome {string}")]
    public void ThenRewardOutcome(string id, string expected)
    {
        var item = RequireItem(id);
        if (expected is "Locked" or "Missed")
        {
            Assert.Equal(expected, item.Lock?.State.ToString());
            Assert.False(item.Claimable);
            Assert.Null(item.ExpiresAt);
        }
        else
        {
            Assert.Null(item.Lock);
            Assert.Equal(expected switch { "Available" => "pending", "Expired" => "expired", "Processed" => "processed", _ => throw new ArgumentException(expected) }, item.Status);
            Assert.Equal(expected == "Available", item.Claimable);
        }
    }

    [Then("service item {string} retains historical prerequisites:")]
    public void ThenHistoricalPrerequisites(string id, DataTable table)
    {
        var prerequisites = RequireItem(id).Prerequisites;
        Assert.NotNull(prerequisites);
        Assert.Equal(table.Rows.Count, prerequisites.Count);
        foreach (var row in table.Rows)
        {
            var actual = Assert.Single(prerequisites, p => p.Label == row["Label"]);
            Assert.Equal(long.Parse(row["Mileage"]), actual.Mileage);
            var satisfied = row["SatisfiedOn"] != "-";
            Assert.Equal(satisfied, actual.Satisfied);
            Assert.Equal(satisfied ? DateTime.Parse(row["SatisfiedOn"]) : (DateTime?)null, actual.SatisfiedOn);
            if (!satisfied) { Assert.Null(actual.Evidence); continue; }
            Assert.NotNull(actual.Evidence);
            Assert.Equal(DateTime.Parse(row["EvidenceDate"]), actual.Evidence.InvoiceDate);
            Assert.Equal(row["PackageCode"] == "-" ? null : row["PackageCode"], actual.Evidence.PackageCode);
            Assert.Equal(row["LaborCode"], actual.Evidence.LaborCode);
            Assert.Equal(row["JobNumber"], actual.Evidence.JobNumber);
            Assert.Equal(row["InvoiceNumber"], actual.Evidence.InvoiceNumber);
        }
    }

    [Then("historical evaluation reports {string}")]
    public void ThenHistoricalDiagnostic(string text) =>
        Assert.Contains(_historicalTrace!.Build().Notes, note => note.Contains(text, StringComparison.Ordinal));
}
