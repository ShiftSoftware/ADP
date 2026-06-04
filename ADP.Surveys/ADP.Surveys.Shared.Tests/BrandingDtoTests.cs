using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class BrandingDtoTests
{
    [Fact]
    public void Merge_BothNull_ReturnsNull()
    {
        Assert.Null(BrandingDto.Merge(null, null));
    }

    [Fact]
    public void Merge_OnlyDeployment_ReturnsDeployment()
    {
        var deployment = new BrandingDto { PrimaryColor = "#eb0a1e" };
        Assert.Same(deployment, BrandingDto.Merge(deployment, null));
    }

    [Fact]
    public void Merge_OnlySurvey_ReturnsSurvey()
    {
        var survey = new BrandingDto { PrimaryColor = "#0055a4" };
        Assert.Same(survey, BrandingDto.Merge(null, survey));
    }

    [Fact]
    public void Merge_SurveyFieldWins_DeploymentFillsGaps()
    {
        var deployment = new BrandingDto
        {
            PrimaryColor = "#eb0a1e",
            SecondaryColor = "#222222",
            LogoUrl = "https://toyota.example/logo.png",
            FaviconUrl = "https://toyota.example/favicon.ico",
        };
        var survey = new BrandingDto
        {
            PrimaryColor = "#0055a4",
            LogoUrl = "https://lexus.example/logo.png",
        };

        var merged = BrandingDto.Merge(deployment, survey)!;

        Assert.Equal("#0055a4", merged.PrimaryColor);                          // survey wins
        Assert.Equal("https://lexus.example/logo.png", merged.LogoUrl);        // survey wins
        Assert.Equal("#222222", merged.SecondaryColor);                        // deployment fills
        Assert.Equal("https://toyota.example/favicon.ico", merged.FaviconUrl); // deployment fills
    }

    [Fact]
    public void Merge_DoesNotMutateInputs()
    {
        var deployment = new BrandingDto { PrimaryColor = "#eb0a1e", LogoUrl = "https://t.example/l.png" };
        var survey = new BrandingDto { PrimaryColor = "#0055a4" };

        var merged = BrandingDto.Merge(deployment, survey)!;

        Assert.NotSame(deployment, merged);
        Assert.NotSame(survey, merged);
        Assert.Equal("#eb0a1e", deployment.PrimaryColor);
        Assert.Null(survey.LogoUrl);
    }
}
