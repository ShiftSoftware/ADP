using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

[Binding]
public class VehiclePaintThicknessStepDefinitions
{
    private readonly Support.TestContext _context;
    private IEnumerable<PaintThicknessInspectionDTO>? _result;
    private bool _evaluated;

    public VehiclePaintThicknessStepDefinitions(Support.TestContext context)
    {
        _context = context;
    }

    [When("evaluating paint thickness with language {string}")]
    public async Task WhenEvaluatingPaintThicknessWithLanguage(string language)
    {
        _result = await new VehiclePaintThicknessEvaluator(
            _context.Aggregate, _context.Options, _context.ServiceProvider)
            .Evaluate(language);
        _evaluated = true;
    }

    [Then("there is {int} paint thickness inspection")]
    [Then("there are {int} paint thickness inspections")]
    public void ThenThereArePaintThicknessInspections(int count)
    {
        Assert.True(_evaluated);
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Count());
    }

    [Then("there are no paint thickness inspections")]
    public void ThenThereAreNoPaintThicknessInspections()
    {
        Assert.True(_evaluated);
        Assert.Null(_result);
    }

    [Then("the inspection on {string} has {int} panels")]
    public void ThenTheInspectionHasPanels(string date, int panelCount)
    {
        Assert.NotNull(_result);
        var inspectionDate = DateTime.Parse(date);
        var inspection = _result.FirstOrDefault(i => i.InspectionDate == inspectionDate);
        Assert.NotNull(inspection);
        Assert.NotNull(inspection.Panels);
        Assert.Equal(panelCount, inspection.Panels.Count());
    }
}
