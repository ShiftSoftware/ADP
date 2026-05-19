using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;

public static class SampleSurveySeeder
{
    /// <summary>
    /// Idempotent — each recipe carries an IntegrationId / Key that we check against
    /// existing rows so re-running the app does not duplicate data. Drafts only; no
    /// publish is attempted. Open a seeded survey in the builder to inspect or publish.
    /// </summary>
    public static async Task SeedSampleSurveysAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        foreach (var recipe in SampleSurveys.All)
        {
            foreach (var bank in recipe.Banks)
                await EnsureBankAsync(db, bank);

            foreach (var template in recipe.Templates)
                await EnsureTemplateAsync(db, template);

            if (await db.Set<Survey>().AnyAsync(s => s.IntegrationId == recipe.IntegrationId))
                continue;

            db.Set<Survey>().Add(new Survey
            {
                Name = recipe.Name,
                IntegrationId = recipe.IntegrationId,
                DraftJson = JsonSerializer.Serialize(recipe.Draft, SurveySchemaSerializer.Options),
            });

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureBankAsync(DB db, BankRecipe recipe)
    {
        if (await db.Set<BankQuestion>().AnyAsync(b => b.Key == recipe.Key))
            return;

        db.Set<BankQuestion>().Add(new BankQuestion
        {
            Key = recipe.Key,
            // Serialize against the abstract base so the type discriminator ("type": "nps", …) is emitted.
            QuestionJson = JsonSerializer.Serialize(recipe.Question, typeof(QuestionDto), SurveySchemaSerializer.Options),
            BiColumn = recipe.BiColumn,
            Tags = recipe.Tags,
        });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureTemplateAsync(DB db, TemplateRecipe recipe)
    {
        if (await db.Set<ScreenTemplate>().AnyAsync(t => t.Key == recipe.Key))
            return;

        db.Set<ScreenTemplate>().Add(new ScreenTemplate
        {
            Key = recipe.Key,
            TemplateJson = JsonSerializer.Serialize(recipe.Template, SurveySchemaSerializer.Options),
            Tags = recipe.Tags,
        });

        await db.SaveChangesAsync();
    }

}
