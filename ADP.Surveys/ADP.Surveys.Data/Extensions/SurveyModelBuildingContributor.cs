using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Extensions;

/// <summary>
/// Registered by the API package's <c>AddSurveysApiServices</c> so consumer DbContexts
/// pick up Surveys' entity configuration without having to call
/// <see cref="SurveyModelBuilderExtensions.ConfigureSurveyEntities"/> themselves.
/// Mirrors the <c>MenuModelBuildingContributor</c> pattern.
/// </summary>
public class SurveyModelBuildingContributor : IModelBuildingContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureSurveyEntities();
    }
}
