using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;

namespace ShiftSoftware.ADP.Surveys.Data;

/// <summary>
/// Contract surface of the Surveys DbContext. Consumer apps can depend on the interface
/// and <c>SurveysDB</c> registers itself for DI; mirrors the <c>IMenuDbContext</c> pattern.
/// </summary>
public interface ISurveysDbContext
{
    DbSet<Survey> Surveys { get; set; }
    DbSet<SurveyVersion> SurveyVersions { get; set; }
    DbSet<BankQuestion> BankQuestions { get; set; }
    DbSet<ScreenTemplate> ScreenTemplates { get; set; }
    DbSet<SurveyInstance> SurveyInstances { get; set; }
    DbSet<SurveyResponse> SurveyResponses { get; set; }
    DbSet<SurveyAnswer> SurveyAnswers { get; set; }
}
