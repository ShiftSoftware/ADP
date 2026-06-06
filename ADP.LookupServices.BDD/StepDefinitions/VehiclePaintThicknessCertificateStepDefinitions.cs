using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehiclePaintThicknessCertificateStepDefinitions
{
    private readonly Support.TestContext _context;
    private PaintThicknessCertificateModel? _result;
    private bool _evaluated;
    private bool? _availability;

    public VehiclePaintThicknessCertificateStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [Given("the distributor company id is {long}")]
    public void GivenTheDistributorCompanyIdIs(long companyId)
    {
        _context.Options.DistributorCompanyID = companyId;
    }

    [When("evaluating the paint thickness certificate with language {string}")]
    public async Task WhenEvaluatingTheCertificate(string language)
    {
        _result = await new PaintThicknessCertificateEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .Evaluate(language);
        _evaluated = true;
    }

    [Given("a paint thickness certificate serial number resolver that returns {string}")]
    public void GivenASerialNumberResolver(string serial)
    {
        _context.Options.PaintThicknessCertificateSerialNumberResolver = _ => new ValueTask<string?>(serial);
    }

    [Then("the certificate serial number is {string}")]
    public void ThenTheSerialNumberIs(string serial)
    {
        Assert.NotNull(_result);
        Assert.Equal(serial, _result!.SerialNumber);
    }

    [Then("the certificate has no serial number")]
    public void ThenTheCertificateHasNoSerialNumber()
    {
        Assert.NotNull(_result);
        Assert.Null(_result!.SerialNumber);
    }

    [When("checking paint thickness certificate availability")]
    public void WhenCheckingAvailability()
    {
        _availability = new PaintThicknessCertificateEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .EvaluateAvailability();
    }

    [Then("the paint thickness certificate is reported as available")]
    public void ThenReportedAsAvailable()
    {
        Assert.True(_availability);
    }

    [Then("the paint thickness certificate is reported as unavailable")]
    public void ThenReportedAsUnavailable()
    {
        Assert.False(_availability);
    }

    [Then("a paint thickness certificate is produced")]
    public void ThenACertificateIsProduced()
    {
        Assert.True(_evaluated);
        Assert.NotNull(_result);
    }

    [Then("no paint thickness certificate is produced")]
    public void ThenNoCertificateIsProduced()
    {
        Assert.True(_evaluated);
        Assert.Null(_result);
    }

    [Then("the certificate is based on the inspection on {string}")]
    public void ThenTheCertificateIsBasedOnInspectionOn(string date)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(date), _result!.InspectionDate);
    }

    [Then("the certificate invoice date is {string}")]
    public void ThenTheCertificateInvoiceDateIs(string date)
    {
        Assert.NotNull(_result);
        Assert.Equal(DateTime.Parse(date), _result!.InvoiceDate);
    }

    [Then("the certificate has {int} readings")]
    [Then("the certificate has {int} reading")]
    public void ThenTheCertificateHasReadings(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result!.Readings.Count);
    }

    [Then("the certificate has a reading {string} with thickness {decimal}")]
    public void ThenTheCertificateHasReadingWithThickness(string label, decimal thickness)
    {
        Assert.NotNull(_result);
        var reading = _result!.Readings.FirstOrDefault(r => r.PanelLabel == label);
        Assert.NotNull(reading);
        Assert.Equal(thickness, reading!.MeasuredThickness);
    }

    [Then("the certificate gallery has {int} images")]
    [Then("the certificate gallery has {int} image")]
    public void ThenTheGalleryHasImages(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result!.PanelImages.Count);
    }
}
