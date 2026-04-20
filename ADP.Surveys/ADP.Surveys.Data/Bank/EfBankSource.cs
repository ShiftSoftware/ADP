using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Bank;

/// <summary>
/// EF Core-backed implementation of <see cref="IBankSource"/> for <c>SchemaResolver</c>
/// and <c>SurveyIntegrityValidator</c>. Loads rows on first hit, caches them for the
/// rest of the resolve pass — a single resolve typically touches few bank entries
/// and we want to avoid 1+N queries per rendered survey.
///
/// Depends on the resolved <see cref="ShiftDbContext"/> (aliased to the consumer's own
/// DbContext in DI by <c>AddSurveysApiServices</c>), not <see cref="ISurveysDbContext"/>,
/// because consumer DbContexts don't implement that interface — they pick up the
/// Surveys entities via the <c>IModelBuildingContributor</c>.
///
/// One instance per resolution (don't share across threads or publish cycles). The
/// cache is memoization for that single operation, not a long-lived DI singleton.
/// </summary>
public class EfBankSource : IBankSource
{
    private readonly ShiftDbContext db;
    private readonly Dictionary<string, BankQuestionDto?> questionCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ScreenTemplateDto?> templateCache = new(StringComparer.Ordinal);

    public EfBankSource(ShiftDbContext db) => this.db = db;

    /// <summary>
    /// Keys of every bank question hit during this resolve pass that actually existed.
    /// The publisher uses this to flip <c>Locked</c> true on the exact entries a new
    /// survey version now depends on — Decision #11's append-only guarantee.
    /// </summary>
    public IEnumerable<string> UsedQuestionIds => questionCache
        .Where(kv => kv.Value is not null)
        .Select(kv => kv.Key);

    /// <summary>Keys of every template hit during this resolve pass that existed.</summary>
    public IEnumerable<string> UsedTemplateIds => templateCache
        .Where(kv => kv.Value is not null)
        .Select(kv => kv.Key);

    public BankQuestionDto? GetQuestion(string id)
    {
        if (questionCache.TryGetValue(id, out var cached)) return cached;

        var entity = db.Set<BankQuestion>().AsNoTracking()
            .FirstOrDefault(b => b.Key == id && !b.IsDeleted);
        var dto = entity is null ? null : ToDto(entity);
        questionCache[id] = dto;
        return dto;
    }

    public ScreenTemplateDto? GetTemplate(string id)
    {
        if (templateCache.TryGetValue(id, out var cached)) return cached;

        var entity = db.Set<ScreenTemplate>().AsNoTracking()
            .FirstOrDefault(t => t.Key == id && !t.IsDeleted);
        var dto = entity is null ? null : ToDto(entity);
        templateCache[id] = dto;
        return dto;
    }

    /// <summary>
    /// Eagerly populate the caches from explicit id lists — useful when the caller
    /// already knows which refs a survey touches (e.g. from a prior parse pass) and
    /// wants to avoid per-ref round trips during resolution.
    /// </summary>
    public void Preload(IEnumerable<string> questionIds, IEnumerable<string> templateIds)
    {
        var missingQ = questionIds.Where(id => !questionCache.ContainsKey(id)).ToArray();
        if (missingQ.Length > 0)
        {
            var rows = db.Set<BankQuestion>().AsNoTracking()
                .Where(b => missingQ.Contains(b.Key) && !b.IsDeleted)
                .ToList();
            foreach (var e in rows) questionCache[e.Key] = ToDto(e);
            foreach (var id in missingQ) questionCache.TryAdd(id, null);
        }

        var missingT = templateIds.Where(id => !templateCache.ContainsKey(id)).ToArray();
        if (missingT.Length > 0)
        {
            var rows = db.Set<ScreenTemplate>().AsNoTracking()
                .Where(t => missingT.Contains(t.Key) && !t.IsDeleted)
                .ToList();
            foreach (var e in rows) templateCache[e.Key] = ToDto(e);
            foreach (var id in missingT) templateCache.TryAdd(id, null);
        }
    }

    private static BankQuestionDto ToDto(BankQuestion entity)
    {
        var question = JsonSerializer.Deserialize<QuestionDto>(entity.QuestionJson, SurveySchemaSerializer.Options)
            ?? throw new InvalidOperationException(
                $"BankQuestion '{entity.Key}' has a null or malformed QuestionJson column.");

        return new BankQuestionDto
        {
            Id = entity.Key,
            Question = question,
            BiColumn = entity.BiColumn,
            Locked = entity.Locked,
            Retired = entity.Retired,
            Tags = string.IsNullOrEmpty(entity.Tags)
                ? null
                : entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
        };
    }

    private static ScreenTemplateDto ToDto(ScreenTemplate entity)
    {
        var dto = JsonSerializer.Deserialize<ScreenTemplateDto>(entity.TemplateJson, SurveySchemaSerializer.Options)
            ?? throw new InvalidOperationException(
                $"ScreenTemplate '{entity.Key}' has a null or malformed TemplateJson column.");
        // Belt-and-suspenders: force the DTO's id to the entity's current Key, so typo
        // corrections to the DB key don't require re-saving the serialized JSON.
        dto.Id = entity.Key;
        return dto;
    }
}
