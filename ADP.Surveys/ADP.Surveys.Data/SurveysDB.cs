using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Data.Extensions;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data;

public class SurveysDB : ShiftDbContext, ISurveysDbContext
{
    public SurveysDB(DbContextOptions option) : base(option) { }

    public DbSet<Survey> Surveys { get; set; } = default!;
    public DbSet<SurveyVersion> SurveyVersions { get; set; } = default!;
    public DbSet<BankQuestion> BankQuestions { get; set; } = default!;
    public DbSet<ScreenTemplate> ScreenTemplates { get; set; } = default!;
    public DbSet<SurveyInstance> SurveyInstances { get; set; } = default!;
    public DbSet<SurveyResponse> SurveyResponses { get; set; } = default!;
    public DbSet<SurveyAnswer> SurveyAnswers { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureSurveyEntities();
    }
}
