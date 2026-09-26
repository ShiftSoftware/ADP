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
        var deployment = new BrandingDto { PrimaryColor = "#0f766e" };
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
            PrimaryColor = "#0f766e",
            SecondaryColor = "#222222",
            LogoUrl = "https://deployment.example/logo.png",
            FaviconUrl = "https://deployment.example/favicon.ico",
        };
        var survey = new BrandingDto
        {
            PrimaryColor = "#0055a4",
            LogoUrl = "https://survey.example/logo.png",
        };

        var merged = BrandingDto.Merge(deployment, survey)!;

        Assert.Equal("#0055a4", merged.PrimaryColor);                          // survey wins
        Assert.Equal("https://survey.example/logo.png", merged.LogoUrl);        // survey wins
        Assert.Equal("#222222", merged.SecondaryColor);                        // deployment fills
        Assert.Equal("https://deployment.example/favicon.ico", merged.FaviconUrl); // deployment fills
    }

    [Fact]
    public void Merge_DoesNotMutateInputs()
    {
        var deployment = new BrandingDto { PrimaryColor = "#0f766e", LogoUrl = "https://deployment.example/logo.png" };
        var survey = new BrandingDto { PrimaryColor = "#0055a4" };

        var merged = BrandingDto.Merge(deployment, survey)!;

        Assert.NotSame(deployment, merged);
        Assert.NotSame(survey, merged);
        Assert.Equal("#0f766e", deployment.PrimaryColor);
        Assert.Null(survey.LogoUrl);
    }
}
