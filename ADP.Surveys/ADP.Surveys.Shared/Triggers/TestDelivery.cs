using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Responses;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;

namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// The operator's input for a test run sent to a recipient: which values to ask for, and the
/// event payload those values become. <c>recipient.*</c> is not asked for as a field because it
/// comes from the recipient the operator enters, and <c>answers.*</c> from the respondent.
/// </summary>
public static class TestDelivery
{
    private const string CandidatePrefix = "candidate.";

    /// <summary>
    /// Every <c>candidate.*</c> value to ask for, declared variables first: a variable can be
    /// declared just to be asked for, when something other than the copy needs it, such as a
    /// subscriber. Then the tokens the copy uses, then what the host always needs.
    /// </summary>
    public static List<TestDeliveryFieldDTO> CollectFields(SurveyDto survey, IEnumerable<string>? hostFields = null)
    {
        var fields = new List<TestDeliveryFieldDTO>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? rawName, SurveyVariableDto? variable = null)
        {
            var name = rawName?.Trim();
            if (name is null || !IsCandidateField(name) || !seen.Add(name)) return;

            fields.Add(new TestDeliveryFieldDTO
            {
                Name = name,
                Description = variable?.Description,
                Example = variable?.Example is { } example ? new Dictionary<string, string>(example) : new(),
            });
        }

        foreach (var variable in survey.Variables ?? [])
            Add(variable.Name, variable);

        foreach (var token in PersonalizationTokens.CollectTokenNames(survey).Order(StringComparer.Ordinal))
            Add(token);

        foreach (var field in hostFields ?? [])
            Add(field);

        return fields;
    }

    /// <summary>
    /// The candidate payload a real event would have frozen on the instance. Blank values are left
    /// out, so a field left empty behaves like one the event is missing.
    /// </summary>
    public static string BuildCandidatePayload(IReadOnlyDictionary<string, string>? variables)
    {
        var payload = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (rawName, value) in variables ?? new Dictionary<string, string>())
        {
            var name = rawName.Trim();
            if (IsCandidateField(name) && !string.IsNullOrWhiteSpace(value))
                payload[name[CandidatePrefix.Length..]] = value.Trim();
        }

        return JsonSerializer.Serialize(payload);
    }

    private static bool IsCandidateField(string name) =>
        name.StartsWith(CandidatePrefix, StringComparison.Ordinal) && name.Length > CandidatePrefix.Length;
}
