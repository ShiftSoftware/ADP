using NSubstitute;
using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehiclePaintThicknessCertificateStepDefinitions
{
    private readonly Support.TestContext _context;
    private PaintThicknessCertificateModel? _result;
    private bool _evaluated;
    private bool? _availability;
    private VehicleLookupDTO? _lookupResult;

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

    [Given("a paint thickness certificate url resolver that returns {string} for languages {string}")]
    public void GivenACertificateUrlResolver(string urlTemplate, string languages)
    {
        var languageList = languages
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        _context.Options.PaintThicknessCertificateUrlsResolver =
            h => new ValueTask<List<PaintThicknessCertificateUrlDTO>?>(
                languageList.Select(language => new PaintThicknessCertificateUrlDTO
                {
                    Language = language,
                    Name = $"{language}-name",
                    Url = $"{urlTemplate.Replace("{vin}", h.Value)}?lang={language}",
                }).ToList());
    }

    [When("looking up the vehicle {string} with certificate url generation requested")]
    public Task WhenLookingUpWithUrlGeneration(string vin) => LookupAsync(vin, generateCertificateUrl: true);

    [When("looking up the vehicle {string} without certificate url generation")]
    public Task WhenLookingUpWithoutUrlGeneration(string vin) => LookupAsync(vin, generateCertificateUrl: false);

    // Exercises the FULL lookup (not the evaluator directly) — the certificate url is
    // produced by VehicleLookupService.LookupFromAggregateAsync, gated on availability,
    // the request option and the resolver.
    private async Task LookupAsync(string vin, bool generateCertificateUrl)
    {
        _context.StorageService.GetAggregatedCompanyData(vin).Returns(_context.Aggregate);

        var service = new VehicleLookupService(_context.StorageService, _context.ServiceProvider, null, _context.Options);

        _lookupResult = await service.LookupAsync(vin, new VehicleLookupRequestOptions
        {
            GeneratePaintThicknessCertificateUrls = generateCertificateUrl,
        });
    }

    [Then("the lookup reports the paint thickness certificate as available")]
    public void ThenLookupReportsAvailable()
    {
        Assert.NotNull(_lookupResult);
        Assert.True(_lookupResult!.PaintThicknessCertificateAvailable);
    }

    [Then("the lookup reports the paint thickness certificate as unavailable")]
    public void ThenLookupReportsUnavailable()
    {
        Assert.NotNull(_lookupResult);
        Assert.False(_lookupResult!.PaintThicknessCertificateAvailable);
    }

    [Then("the lookup has {int} certificate urls")]
    public void ThenTheLookupHasCertificateUrls(int count)
    {
        Assert.NotNull(_lookupResult);
        Assert.NotNull(_lookupResult!.PaintThicknessCertificateUrls);
        Assert.Equal(count, _lookupResult!.PaintThicknessCertificateUrls.Count);
    }

    [Then("the lookup certificate url for {string} is {string}")]
    public void ThenTheLookupCertificateUrlForLanguageIs(string language, string url)
    {
        Assert.NotNull(_lookupResult);
        Assert.NotNull(_lookupResult!.PaintThicknessCertificateUrls);
        var entry = _lookupResult!.PaintThicknessCertificateUrls.FirstOrDefault(x => x.Language == language);
        Assert.NotNull(entry);
        Assert.Equal(url, entry!.Url);

        // The display name rides the entry untouched — UIs render it with no language
        // knowledge of their own.
        Assert.Equal($"{language}-name", entry!.Name);
    }

    [Then("the lookup has no certificate urls")]
    public void ThenTheLookupHasNoCertificateUrls()
    {
        Assert.NotNull(_lookupResult);
        Assert.Null(_lookupResult!.PaintThicknessCertificateUrls);
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
