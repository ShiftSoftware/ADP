using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Data.Repositories;
using ShiftSoftware.ADP.Surveys.Shared;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Data.Tests;

/// <summary>
/// Exercises the compiled Survey mappers without accessing a database.
/// </summary>
public class SurveyMappingRegressionTests : IDisposable
{
    private readonly ServiceProvider host;
    private readonly IServiceScope scope;

    public SurveyMappingRegressionTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(Mock.Of<ITypeAuthService>());
        services.AddControllers().AddShiftEntityWeb(options =>
        {
            options.AddDataAssembly(typeof(Survey).Assembly);
            options.AddDataAssembly(typeof(SurveyDto).Assembly);
        });
        services.AddDbContext<SurveysDB>(options => options.UseSqlServer(
            "Server=localhost;Database=SurveyMappingTests;Trusted_Connection=True;TrustServerCertificate=True"));
        services.AddScoped<ShiftDbContext>(provider => provider.GetRequiredService<SurveysDB>());
        services.RegisterShiftRepositories(typeof(Survey).Assembly);
        host = services.BuildServiceProvider();
        scope = host.CreateScope();
    }

    [Fact]
    public void SurveyDraftRoundTripsPersonalizationWithoutChangingThePublishedVersion()
    {
        var mapper = scope.ServiceProvider.GetRequiredService<SurveyRepository>();
        var entity = new Survey { ID = 42, Name = "Survey", PublishedVersionNumber = 3 };
        mapper.MapToEntity(new SurveyAdminDTO
        {
            Name = "Updated survey",
            PublishedVersionNumber = 99,
            Draft = new SurveyDto
            {
                Variables =
                [
                    new SurveyVariableDto
                    {
                        Name = "candidate.customerName",
                        Example = new() { ["en"] = "Sample customer" },
                        Fallback = new() { ["en"] = "Customer" }
                    }
                ]
            }
        }, entity);
        var restored = mapper.MapToView(entity);

        Assert.Equal("Updated survey", entity.Name);
        Assert.Equal(3, entity.PublishedVersionNumber);
        Assert.Equal("42", restored.Draft!.SurveyId);
        var variable = Assert.Single(restored.Draft.Variables);
        Assert.Equal("candidate.customerName", variable.Name);
        Assert.Equal("Sample customer", variable.Example["en"]);
        Assert.Equal("Customer", variable.Fallback["en"]);
    }

    [Fact]
    public void BankQuestionMappingPreservesIdentityLockAndQuestionJson()
    {
        var mapper = scope.ServiceProvider.GetRequiredService<BankQuestionRepository>();
        var entity = new BankQuestion { Locked = true };
        var originalId = entity.BankEntryID;
        mapper.MapToEntity(new BankQuestionAdminDTO
        {
            Key = "feedback",
            BankEntryID = Guid.Empty,
            Locked = false,
            Question = new TextQuestionDto { MaxLength = 120 }
        }, entity);
        var restored = mapper.MapToView(entity);

        Assert.NotEqual(Guid.Empty, originalId);
        Assert.Equal(originalId, entity.BankEntryID);
        Assert.True(entity.Locked);
        Assert.Equal(120, Assert.IsType<TextQuestionDto>(restored.Question).MaxLength);
        Assert.NotNull(restored.Tags);
        Assert.Empty(restored.Tags);
    }

    [Fact]
    public void ScreenTemplateMappingPreservesJsonAndNormalizesEmptyTags()
    {
        var mapper = scope.ServiceProvider.GetRequiredService<ScreenTemplateRepository>();
        var entity = new ScreenTemplate();
        mapper.MapToEntity(new ScreenTemplateAdminDTO
        {
            Key = "feedback-screen",
            Template = new ScreenTemplateDto(),
            Tags = []
        }, entity);
        var restored = mapper.MapToView(entity);

        Assert.Equal("feedback-screen", restored.Key);
        Assert.NotNull(restored.Template);
        Assert.Null(entity.Tags);
        Assert.NotNull(restored.Tags);
        Assert.Empty(restored.Tags);
    }

    [Fact]
    public void SurveyResponseProjectionExcludesDeletedResponses()
    {
        var mapper = scope.ServiceProvider.GetRequiredService<SurveyInstanceRepository>();
        var completedAt = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var instance = new SurveyInstance
        {
            SurveyVersion = new SurveyVersion { Version = 2 },
            TriggeredBy = SurveysConstants.DashboardTestTriggerSource,
            Responses =
            [
                new SurveyResponse { CompletedAt = completedAt },
                new SurveyResponse { IsDeleted = true, CompletedAt = completedAt.AddDays(1) }
            ]
        };
        var result = mapper.MapToList(new[] { instance }.AsQueryable()).Single();

        Assert.True(result.IsTest);
        Assert.Equal(2, result.SchemaVersion);
        Assert.Equal(1, result.ResponseCount);
        Assert.Equal(completedAt, result.CompletedAt);
    }

    public void Dispose()
    {
        scope.Dispose();
        host.Dispose();
    }
}
