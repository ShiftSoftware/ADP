using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// The case browser's scale and disclosure options are off unless a host turns them on, and off they leave the
/// surfaces as they were before the options existed: every city shown, comparison groups named by slug, nothing read
/// to build an index. Values are synthetic.
/// </summary>
public class OptionDefaultsTests
{
    static readonly PiiOptions Pii = new();
    static readonly IPiiMasker Masker = new DefaultPiiMasker(Options.Create(Pii));

    [Fact]
    public void Every_scale_and_disclosure_option_is_off_by_default()
    {
        var o = new DarlasticApiOptions();
        Assert.False(o.GoldenRowIndex);
        Assert.False(o.RecordSearchIndex);
        Assert.Equal(0, o.CityMinimumShare);
        Assert.False(o.LabelSourcesInBlockKeys);
        Assert.Empty(o.SourceLabels);
    }

    /// <summary>Runs the city filter for a request; the request's services hold the options and nothing else, so
    /// reading the registry (which an off option must not do) would throw.</summary>
    static async Task<HttpContext> CityFilterAsync(DarlasticApiOptions options)
    {
        var services = new ServiceCollection().AddSingleton(Options.Create(options)).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        bool ran = false;
        await new CityDisclosureAttribute().OnResourceExecutionAsync(
            new ResourceExecutingContext(action, filters, new List<IValueProviderFactory>()),
            () => { ran = true; return Task.FromResult(new ResourceExecutedContext(action, filters)); });
        Assert.True(ran, "the city filter did not run the action");
        return http;
    }

    [Fact]
    public async Task Off_the_city_rule_reads_nothing_and_shows_every_city_to_a_masked_caller()
    {
        var http = await CityFilterAsync(new DarlasticApiOptions());
        Assert.Null(CityDisclosureAttribute.ShownCities(http));

        var view = new RecordPiiView(Masker, Pii, reveal: false, partialSearch: false, CityDisclosureAttribute.ShownCities(http));
        Assert.Equal("Rare Synth Town", view.ShowCity("Rare Synth Town"));
        Assert.Equal("Rare Synth Town", view.Quoter!(QuotedValue.City, "Rare Synth Town"));
    }

    [Fact]
    public void On_the_city_rule_masks_a_city_too_few_customers_share()
    {
        var view = new RecordPiiView(Masker, Pii, reveal: false, partialSearch: false,
            new HashSet<string>(["Shared Synth City"], StringComparer.OrdinalIgnoreCase));
        Assert.Equal("shared synth city", view.ShowCity("shared synth city"));
        Assert.Equal(Masker.Mask(PiiKind.Address, "Rare Synth Town"), view.ShowCity("Rare Synth Town"));
        Assert.NotEqual("Rare Synth Town", view.ShowCity("Rare Synth Town"));

        var reveal = new RecordPiiView(Masker, Pii, reveal: true, partialSearch: false,
            new HashSet<string>(["Shared Synth City"], StringComparer.OrdinalIgnoreCase));
        Assert.Equal("Rare Synth Town", reveal.ShowCity("Rare Synth Town"));
    }

    [Fact]
    public void Off_comparison_groups_name_their_source_by_slug_even_when_the_host_gives_labels()
    {
        var options = new DarlasticApiOptions { SourceLabels = new(StringComparer.OrdinalIgnoreCase) { ["src-a"] = "Source A" } };
        Assert.Null(RecordPiiView.BlockKeyLabels(options));

        var reveal = new RecordPiiView(Masker, Pii, reveal: true, partialSearch: false, sourceLabels: RecordPiiView.BlockKeyLabels(options));
        Assert.Equal("R:src-a|42", reveal.BlockKey("R:src-a|42"));
        Assert.Equal("P:7000000001", reveal.BlockKey("P:7000000001"));

        options.LabelSourcesInBlockKeys = true;
        var labelled = new RecordPiiView(Masker, Pii, reveal: true, partialSearch: false, sourceLabels: RecordPiiView.BlockKeyLabels(options));
        Assert.Equal("R:Source A|42", labelled.BlockKey("R:src-a|42"));
        Assert.Equal("R:src-b|42", labelled.BlockKey("R:src-b|42"));

        var masked = new RecordPiiView(Masker, Pii, reveal: false, partialSearch: false, sourceLabels: RecordPiiView.BlockKeyLabels(options));
        Assert.Equal("R:" + Pii.HiddenText, masked.BlockKey("R:src-a|42"));
    }
}
