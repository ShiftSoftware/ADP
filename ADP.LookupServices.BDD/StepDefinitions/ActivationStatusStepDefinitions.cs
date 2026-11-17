using System.Globalization;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class ActivationStatusStepDefinitions
{
    private readonly Support.TestContext _context;
    private bool _activationDue;
    private long? _requestingCompanyID;
    private WarrantyActivationStatus _status;
    private VehicleLookupDTO? _lookup;

    public ActivationStatusStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("activation is due")]
    public void GivenActivationIsDue() => _activationDue = true;

    [Given("activation is not due")]
    public void GivenActivationIsNotDue() => _activationDue = false;

    [Given("LookupOptions has require-allocation-for-activation enabled")]
    public void GivenRequireAllocationForActivationEnabled() => _context.Options.RequireAllocationForActivation = true;

    [Given("the requesting company is {string}")]
    public void GivenTheRequestingCompanyIs(string companyID) => _requestingCompanyID = long.Parse(companyID);

    [When("resolving the warranty activation status")]
    public void WhenResolvingTheWarrantyActivationStatus()
    {
        _status = new ActivationStatusEvaluator(_context.Aggregate, _context.Options)
            .Evaluate(_activationDue, _requestingCompanyID);
    }

    [Then("the warranty activation status is {string}")]
    public void ThenTheWarrantyActivationStatusIs(string expected)
    {
        Assert.Equal(expected, _status.ToString());
    }

    [Given("the lookup signing key is {string}")]
    public void GivenTheLookupSigningKeyIs(string key) => _context.Options.SigningSecretKey = key;

    [When("looking up {string} end-to-end")]
    public async Task WhenLookingUpEndToEnd(string vin)
    {
        _lookup = await _context.LookupAsync(vin, new VehicleLookupRequestOptions
        {
            LanguageCode = "en",
            RequestingCompanyID = _requestingCompanyID,
        });

        _status = _lookup.Warranty.ActivationStatus;
    }

    [Then("no warranty activation token is issued")]
    public void ThenNoWarrantyActivationTokenIsIssued() => Assert.Null(_lookup!.Warranty.ActivationToken);

    [Then("the warranty activation token is valid for {string} and company {string} at {string}")]
    public void ThenTheWarrantyActivationTokenIsValid(string vin, string companyID, string at) =>
        Assert.True(Validate(_lookup!.Warranty.ActivationToken, vin, companyID, at));

    [Then("the warranty activation token is not valid for {string} and company {string} at {string}")]
    public void ThenTheWarrantyActivationTokenIsNotValid(string vin, string companyID, string at) =>
        Assert.False(Validate(_lookup!.Warranty.ActivationToken, vin, companyID, at));

    [Then("the warranty activation token with a later expiry written into it is not valid for {string} and company {string} at {string}")]
    public void ThenTheTokenWithALaterExpiryIsNotValid(string vin, string companyID, string at)
    {
        var token = _lookup!.Warranty.ActivationToken!;
        var separator = token.IndexOf('.');
        var laterExpiry = long.Parse(token[..separator], CultureInfo.InvariantCulture) + 3600;

        Assert.False(Validate($"{laterExpiry}{token[separator..]}", vin, companyID, at));
    }

    private bool Validate(string? token, string vin, string companyID, string at) =>
        WarrantyActivationToken.Validate(
            token,
            vin,
            long.Parse(companyID, CultureInfo.InvariantCulture),
            _context.Options.SigningSecretKey,
            DateTime.SpecifyKind(DateTime.Parse(at, CultureInfo.InvariantCulture), DateTimeKind.Utc));
}
