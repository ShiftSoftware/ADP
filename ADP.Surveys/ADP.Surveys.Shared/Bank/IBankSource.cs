using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;

namespace ShiftSoftware.ADP.Surveys.Shared.Bank;

/// <summary>
/// Lookup abstraction over the Question Bank + Screen Template catalog. Consumed by
/// the schema resolver and the integrity validator. The Data layer (Phase 1 Part C)
/// implements this against EF Core; tests use the in-memory implementation.
///
/// Intentionally synchronous and id-keyed — resolution happens against a snapshot
/// loaded by the service, not against a live query per-ref. The caller is expected
/// to pre-load everything a given survey references.
/// </summary>
public interface IBankSource
{
    /// <summary>Null when no bank entry with this id exists.</summary>
    BankQuestionDto? GetQuestion(string id);

    /// <summary>Null when no screen template with this id exists.</summary>
    ScreenTemplateDto? GetTemplate(string id);
}

/// <summary>
/// Test-friendly <see cref="IBankSource"/> built from in-memory collections.
/// Accepts either a dictionary or a list (ids are taken from entries).
/// </summary>
public class InMemoryBankSource : IBankSource
{
    private readonly Dictionary<string, BankQuestionDto> questions;
    private readonly Dictionary<string, ScreenTemplateDto> templates;

    public InMemoryBankSource(
        IEnumerable<BankQuestionDto>? questions = null,
        IEnumerable<ScreenTemplateDto>? templates = null)
    {
        this.questions = (questions ?? []).ToDictionary(q => q.Id);
        this.templates = (templates ?? []).ToDictionary(t => t.Id);
    }

    public BankQuestionDto? GetQuestion(string id) =>
        questions.TryGetValue(id, out var q) ? q : null;

    public ScreenTemplateDto? GetTemplate(string id) =>
        templates.TryGetValue(id, out var t) ? t : null;
}
