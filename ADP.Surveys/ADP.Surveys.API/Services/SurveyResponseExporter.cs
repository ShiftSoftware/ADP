using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Builds the wide, BI-shaped export of a survey's responses: one row per submitted
/// response, one column per distinct answer key, plus instance metadata up front.
///
/// <para><b>Columns are anchored on the bank key, not the key captured at submission.</b>
/// That is Decision #11's retroactive-rename rule doing its job: <c>BankEntryID</c> is the
/// stable anchor, so correcting a typo'd bank key later re-labels every historical column
/// instead of splitting one question across two.
/// <c>KeyAtSubmission</c> is the fallback for inline (non-banked) questions, which have no
/// anchor and therefore genuinely can split if an author renames them.</para>
///
/// <para>CSV rather than xlsx: this module is a NuGet package consumed by other people's
/// apps, and the Menus precedent keeps spreadsheet libraries out of the module by pushing
/// them onto the consumer. CSV needs no dependency, opens in Excel, and is what the BI
/// side ingests anyway.</para>
/// </summary>
public class SurveyResponseExporter
{
    private readonly ShiftDbContext db;

    public SurveyResponseExporter(ShiftDbContext db)
    {
        this.db = db;
    }

    /// <summary>Fixed leading columns, in order. Named to survive a naive spreadsheet import.</summary>
    private static readonly string[] MetaColumns =
    [
        "PublicId", "Status", "SchemaVersion", "TriggeredAt", "TriggeredBy",
        "IsTest", "CustomerRef", "RecipientAddress", "RecipientLocale",
        "StartedAt", "CompletedAt", "AgentId",
    ];

    public async Task<string> BuildCsvAsync(long surveyId, bool includeTestInstances, CancellationToken ct = default)
    {
        var responses = await db.Set<SurveyResponse>()
            .Where(r => !r.IsDeleted)
            .Where(r => r.SurveyInstance.SurveyID == surveyId && !r.SurveyInstance.IsDeleted)
            .Include(r => r.SurveyInstance).ThenInclude(i => i.SurveyVersion)
            .Include(r => r.Answers.Where(a => !a.IsDeleted)).ThenInclude(a => a.BankQuestion)
            .OrderBy(r => r.SurveyInstance.TriggeredAt)
            .ThenBy(r => r.ID)
            .ToListAsync(ct);

        if (!includeTestInstances)
        {
            responses = responses
                .Where(r => r.SurveyInstance.TriggeredBy != SurveysConstants.DashboardTestTriggerSource)
                .ToList();
        }

        // Column set is the union across every response — surveys branch, so no single
        // respondent sees every question, and a column driven off the first row would
        // silently drop the others' answers.
        var answerColumns = responses
            .SelectMany(r => r.Answers)
            .Select(ColumnKeyFor)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        var csv = new StringBuilder();
        AppendRow(csv, MetaColumns.Concat(answerColumns));

        foreach (var response in responses)
        {
            // Last write wins on a duplicate key: menu-loop surveys legitimately revisit a
            // screen, and the final answer is the one the respondent stood behind.
            var byColumn = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var answer in response.Answers.OrderBy(a => a.Order).ThenBy(a => a.ID))
                byColumn[ColumnKeyFor(answer)] = RenderValue(answer.ValueJson);

            var instance = response.SurveyInstance;
            var cells = new List<string>
            {
                instance.PublicID.ToString(),
                response.Status.ToString(),
                instance.SurveyVersion?.Version.ToString(CultureInfo.InvariantCulture) ?? "",
                Iso(instance.TriggeredAt),
                instance.TriggeredBy ?? "",
                instance.TriggeredBy == SurveysConstants.DashboardTestTriggerSource ? "true" : "false",
                instance.CustomerRef ?? "",
                instance.RecipientAddress ?? "",
                instance.RecipientLocale ?? "",
                response.StartedAt is { } s ? Iso(s) : "",
                response.CompletedAt is { } c ? Iso(c) : "",
                response.AgentId ?? "",
            };
            cells.AddRange(answerColumns.Select(col => byColumn.GetValueOrDefault(col, "")));

            AppendRow(csv, cells);
        }

        return csv.ToString();
    }

    private static string ColumnKeyFor(SurveyAnswer answer) =>
        answer.BankQuestion?.Key is { Length: > 0 } bankKey ? bankKey : answer.KeyAtSubmission;

    private static string Iso(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    private static string RenderValue(string valueJson) => SurveyCsv.RenderValue(valueJson);

    private static void AppendRow(StringBuilder csv, IEnumerable<string> cells)
    {
        csv.AppendJoin(',', cells.Select(SurveyCsv.EscapeCell));
        // CRLF: Excel is the primary consumer and it is the safer line ending for it.
        csv.Append("\r\n");
    }
}
