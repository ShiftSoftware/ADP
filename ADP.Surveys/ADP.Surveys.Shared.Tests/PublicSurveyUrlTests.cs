using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

/// <summary>
/// The link template is the one setting whose default is silently wrong in production:
/// it works perfectly for every developer and produces a dead link for every customer.
/// These facts pin the "is this deployable?" judgement that the startup check and the
/// send path both rely on.
/// </summary>
public class PublicSurveyUrlTests
{
    [Fact]
    public void DevDefault_IsNotDeployable()
    {
        Assert.False(PublicSurveyUrl.IsDeployable(PublicSurveyUrl.DevDefault));
    }

    [Theory]
    [InlineData("http://localhost:5190/s/{publicId}")]
    [InlineData("https://localhost/s/{publicId}")]
    [InlineData("http://127.0.0.1:8080/s/{publicId}")]
    [InlineData("http://[::1]/s/{publicId}")]
    [InlineData("https://[::1]/s/{publicId}")]
    [InlineData("HTTPS://LOCALHOST/s/{publicId}")]
    public void LoopbackHosts_AreNotDeployable(string template)
    {
        Assert.True(PublicSurveyUrl.PointsAtLoopback(template));
        Assert.False(PublicSurveyUrl.IsDeployable(template));
    }

    [Fact]
    public void HostMerelyContainingLocalhost_IsStillDeployable()
    {
        // Substring matching would reject this; the check is on the parsed host.
        const string template = "https://surveys.localhost-labs.example/s/{publicId}";
        Assert.False(PublicSurveyUrl.PointsAtLoopback(template));
        Assert.True(PublicSurveyUrl.IsDeployable(template));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingTemplate_IsNotDeployable(string? template)
    {
        Assert.False(PublicSurveyUrl.IsDeployable(template));
        Assert.Null(PublicSurveyUrl.Compose(template, Guid.NewGuid()));
    }

    [Fact]
    public void TemplateWithoutPlaceholder_IsNotDeployable()
    {
        // Every recipient would receive the same link — worse than no link, because it
        // looks like it worked.
        const string template = "https://surveys.example.com/s/";
        Assert.False(PublicSurveyUrl.IsDeployable(template));
        Assert.Contains("placeholder", PublicSurveyUrl.DescribeProblem(template));
    }

    [Fact]
    public void Compose_SubstitutesThePublicId()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var url = PublicSurveyUrl.Compose("https://surveys.example.com/s/{publicId}", id);
        Assert.Equal("https://surveys.example.com/s/11111111-2222-3333-4444-555555555555", url);
    }

    [Fact]
    public void DeployableTemplate_ReportsNoProblem()
    {
        const string template = "https://surveys.example.com/s/{publicId}";
        Assert.True(PublicSurveyUrl.IsDeployable(template));
        Assert.Equal("PublicSurveyUrlTemplate is valid.", PublicSurveyUrl.DescribeProblem(template));
    }

    [Theory]
    [InlineData("/s/{publicId}")]
    [InlineData("s/{publicId}")]
    [InlineData("../s/{publicId}")]
    public void NonAbsoluteTemplate_IsNotTreatedAsLoopback(string template)
    {
        // Hosts may serve the survey under their own origin. On Unix, Uri can parse a
        // root-relative route as a file URI; that must not make it a loopback web URL.
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.False(PublicSurveyUrl.PointsAtLoopback(template));
        Assert.True(PublicSurveyUrl.IsDeployable(template));
        Assert.Equal("PublicSurveyUrlTemplate is valid.", PublicSurveyUrl.DescribeProblem(template));
        Assert.Equal(template.Replace("{publicId}", id.ToString()), PublicSurveyUrl.Compose(template, id));
    }

    [Theory]
    [InlineData("file:///s/{publicId}")]
    [InlineData("file://localhost/s/{publicId}")]
    [InlineData("ftp://localhost/s/{publicId}")]
    public void NonHttpUri_IsNotTreatedAsLoopbackWebUrl(string template)
    {
        // This helper checks web hosts, not whether every possible URI scheme is deployable.
        Assert.False(PublicSurveyUrl.PointsAtLoopback(template));
    }
}
