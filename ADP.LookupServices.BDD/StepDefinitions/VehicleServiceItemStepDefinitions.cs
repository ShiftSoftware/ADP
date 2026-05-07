using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehicleServiceItemStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<VehicleServiceItemDTO>? _result;
    private bool _activationRequired;
    private DateTime? _freeServiceStartDate;

    public VehicleServiceItemStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the free service start date is {string}")]
    public void GivenTheFreeServiceStartDateIs(string date)
    {
        _freeServiceStartDate = DateTime.Parse(date);
    }

    [When("evaluating service items for {string} with language {string}")]
    public async Task WhenEvaluatingServiceItemsFor(string vin, string language)
    {
        _context.Aggregate.VIN = vin;

        var vehicle = new VehicleEntryEvaluator(_context.Aggregate).Evaluate();
        _context.CurrentVehicle = vehicle;

        var evaluator = new VehicleServiceItemEvaluator(
            _context.StorageService, _context.Aggregate, _context.Options, _context.ServiceProvider);

        var (serviceItems, activationRequired) = await evaluator.Evaluate(vehicle!, _freeServiceStartDate, language);
        _result = serviceItems;
        _activationRequired = activationRequired;
    }

    [Then("service item {string} has status {string}")]
    public void ThenServiceItemHasStatus(string serviceItemId, string expectedStatus)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expectedStatus, item.Status);
    }

    [Then("service item {string} has type {string}")]
    public void ThenServiceItemHasType(string serviceItemId, string expectedType)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expectedType, item.Type);
    }

    [Then("there are {int} service items")]
    public void ThenThereAreServiceItems(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("there are {int} service items with ID {string}")]
    public void ThenThereAreServiceItemsWithID(int count, string serviceItemId)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count(i => i.ServiceItemID == serviceItemId));
    }

    [Then("activation is required for the result")]
    public void ThenActivationIsRequiredForTheResult()
    {
        Assert.True(_activationRequired, "Expected activationRequired=true but got false");
    }

    [Then("activation is not required for the result")]
    public void ThenActivationIsNotRequiredForTheResult()
    {
        Assert.False(_activationRequired, "Expected activationRequired=false but got true");
    }

    [Then("service item {string} has company name {string}")]
    public void ThenServiceItemHasCompanyName(string serviceItemId, string expected)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expected, item.CompanyName);
    }

    [Then("service item {string} has print url {string}")]
    public void ThenServiceItemHasPrintUrl(string serviceItemId, string expected)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expected, item.PrintUrl);
    }

    [Then("service item {string} has a signature")]
    public void ThenServiceItemHasASignature(string serviceItemId)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.False(string.IsNullOrWhiteSpace(item.Signature), "Expected a non-empty signature");
    }

    [Then("service item {string} has signature expiry within {int} minutes of now")]
    public void ThenServiceItemSignatureExpiryWithinMinutesOfNow(string serviceItemId, int minutes)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        var delta = (item.SignatureExpiry - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(delta, minutes - 1, minutes + 1);
    }

    [Then("service item {string} has job number {string}")]
    public void ThenServiceItemHasJobNumber(string serviceItemId, string expected)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expected, item.JobNumber);
    }

    [Then("service item {string} has invoice number {string}")]
    public void ThenServiceItemHasInvoiceNumber(string serviceItemId, string expected)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expected, item.InvoiceNumber);
    }

    [Then("service item {string} has package code {string}")]
    public void ThenServiceItemHasPackageCode(string serviceItemId, string expected)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(expected, item.PackageCode);
    }

    [Then("the service items in order are:")]
    public void ThenTheServiceItemsInOrderAre(DataTable dataTable)
    {
        Assert.NotNull(_result);
        var expected = dataTable.Rows.Select(r => r["ServiceItemID"]).ToList();
        var actual = _result.Select(i => i.ServiceItemID).ToList();
        Assert.Equal(expected, actual);
    }

    [Then("service item {string} for inspection {string} has status {string}")]
    public void ThenServiceItemForInspectionHasStatus(string serviceItemId, string inspectionId, string expectedStatus)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId && i.VehicleInspectionID == inspectionId);
        Assert.NotNull(item);
        Assert.Equal(expectedStatus, item.Status);
    }

    [Then("service item {string} for campaign VIN entry {string} has status {string}")]
    public void ThenServiceItemForCampaignVinEntryHasStatus(string serviceItemId, string entryId, string expectedStatus)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId && i.CampaignVinEntryID == entryId);
        Assert.NotNull(item);
        Assert.Equal(expectedStatus, item.Status);
    }

    [Then("service item {string} is claimable")]
    public void ThenServiceItemIsClaimable(string serviceItemId)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.True(item.Claimable, $"Expected service item '{serviceItemId}' to be claimable");
    }

    [Then("service item {string} is not claimable")]
    public void ThenServiceItemIsNotClaimable(string serviceItemId)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.False(item.Claimable, $"Expected service item '{serviceItemId}' to not be claimable");
    }

    [Then("service item {string} is in the result")]
    public void ThenServiceItemIsInTheResult(string serviceItemId)
    {
        Assert.NotNull(_result);
        Assert.Contains(_result, i => i.ServiceItemID == serviceItemId);
    }

    [Then("service item {string} is not in the result")]
    public void ThenServiceItemIsNotInTheResult(string serviceItemId)
    {
        Assert.NotNull(_result);
        Assert.DoesNotContain(_result, i => i.ServiceItemID == serviceItemId);
    }

    [Then("service item {string} has expiration {string}")]
    public void ThenServiceItemHasExpiration(string serviceItemId, string expectedDate)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.NotNull(item.ExpiresAt);
        Assert.Equal(DateTime.Parse(expectedDate), item.ExpiresAt.Value);
    }

    [Then("service item {string} has activation {string}")]
    public void ThenServiceItemHasActivation(string serviceItemId, string expectedDate)
    {
        Assert.NotNull(_result);
        var item = _result.FirstOrDefault(i => i.ServiceItemID == serviceItemId);
        Assert.NotNull(item);
        Assert.Equal(DateTime.Parse(expectedDate), item.ActivatedAt);
    }
}
