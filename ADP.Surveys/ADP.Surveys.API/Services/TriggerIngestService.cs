using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;
using ShiftSoftware.ADP.Surveys.Shared.Evaluation;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Slice 2 ingest pipeline. For each candidate event in a batch, finds published
/// surveys whose triggers carry the matching <c>eventKind</c>, evaluates the
/// trigger's filter against the candidate context, materialises a
/// <see cref="SurveyInstance"/> with snapshot fields (channel, recipient,
/// next-send-at), and stamps the unique-hash shadow column.
///
/// Per-item save loop today; slice 3 collapses this into <c>BulkInsertOrUpdateAsync</c>
/// with a real created/skipped/failed split. The unique index on
/// <c>SurveyInstance.UniqueHash</c> already enforces dedup at the DB layer —
/// slice 3 just exposes the skipped path cleanly.
/// </summary>
public class TriggerIngestService
{
    private readonly ShiftDbContext db;

    public TriggerIngestService(ShiftDbContext db)
    {
        this.db = db;
    }

    public async Task<TriggerIngestResult> IngestAsync(TriggerIngestRequest request, CancellationToken ct = default)
    {
        var result = new TriggerIngestResult();
        var matches = await LoadPublishedTriggersForEventKindAsync(request.EventKind, ct);

        foreach (var item in request.Items)
        {
            var itemResult = new TriggerIngestItemResult();
            bool anyCreated = false, anySkipped = false;

            try
            {
                foreach (var match in matches)
                {
                    if (!match.Trigger.Enabled) continue;

                    if (match.Trigger.Filter is not null)
                    {
                        var ctx = BuildEvaluationContext(match.Survey.ID, item.Recipient, item.Payload);
                        if (!LogicEvaluator.EvaluateCondition(match.Trigger.Filter, ctx))
                            continue;
                    }

                    var instance = MaterializeInstance(match.Survey, match.Version, match.Trigger, item);
                    var hashBytes = TriggerHasher.BuildHashBytes(
                        match.Trigger.DedupRecipe, match.Survey.ID, item.Recipient, item.Payload);

                    db.Set<SurveyInstance>().Add(instance);
                    db.Entry(instance).Property(IEntityHasUniqueHash.UniqueHash).CurrentValue = hashBytes;

                    try
                    {
                        await db.SaveChangesAsync(ct);
                        itemResult.Instances.Add(new TriggerIngestInstanceRef
                        {
                            SurveyId = match.Survey.ID,
                            TriggerId = match.Trigger.Id,
                            PublicId = instance.PublicID,
                        });
                        anyCreated = true;
                    }
                    catch (DbUpdateException dbex) when (IsUniqueHashViolation(dbex))
                    {
                        // Detach so subsequent saves in this loop aren't poisoned by a tracked-but-failed entity.
                        db.Entry(instance).State = EntityState.Detached;

                        // Surface the existing instance's PublicID so callers can correlate
                        // "this candidate already produced an instance Y for survey X via trigger Z."
                        var existing = await db.Set<SurveyInstance>()
                            .AsNoTracking()
                            .Where(i => !i.IsDeleted)
                            .Select(i => new { Hash = EF.Property<byte[]>(i, IEntityHasUniqueHash.UniqueHash), i.PublicID })
                            .FirstOrDefaultAsync(x => x.Hash == hashBytes, ct);

                        itemResult.Instances.Add(new TriggerIngestInstanceRef
                        {
                            SurveyId = match.Survey.ID,
                            TriggerId = match.Trigger.Id,
                            PublicId = existing?.PublicID ?? Guid.Empty,
                        });
                        anySkipped = true;
                    }
                }

                if (anyCreated)
                {
                    itemResult.Outcome = TriggerIngestOutcome.Created;
                    result.Created++;
                }
                else if (anySkipped)
                {
                    itemResult.Outcome = TriggerIngestOutcome.Skipped;
                    result.Skipped++;
                }
                else
                {
                    itemResult.Outcome = TriggerIngestOutcome.NoMatch;
                }
            }
            catch (Exception ex)
            {
                itemResult.Outcome = TriggerIngestOutcome.Failed;
                itemResult.Error = ex.Message;
                result.Failed++;
            }

            result.Items.Add(itemResult);
        }

        return result;
    }

    private async Task<List<TriggerMatch>> LoadPublishedTriggersForEventKindAsync(string eventKind, CancellationToken ct)
    {
        // Slice 2 simplistic load: pull all published surveys + their pinned versions in one
        // round-trip, then parse triggers in memory. Per legacy-data-analysis.md the live-template
        // count is small (~50/company), so the cost is bounded. Slice 4+ caches parsed schemas.
        var surveys = await db.Set<Survey>()
            .Include(s => s.Versions)
            .Where(s => !s.IsDeleted && s.PublishedVersionNumber != null)
            .ToListAsync(ct);

        var matches = new List<TriggerMatch>();
        foreach (var survey in surveys)
        {
            var version = survey.Versions.FirstOrDefault(v => v.Version == survey.PublishedVersionNumber);
            if (version is null) continue;

            SurveyDto? schema;
            try
            {
                schema = JsonSerializer.Deserialize<SurveyDto>(version.ResolvedJson, SurveySchemaSerializer.Options);
            }
            catch
            {
                continue;
            }
            if (schema is null) continue;

            foreach (var trigger in schema.Triggers.Where(t => t.EventKind == eventKind))
                matches.Add(new TriggerMatch(survey, version, trigger));
        }

        return matches;
    }

    private static SurveyInstance MaterializeInstance(
        Survey survey, SurveyVersion version, TriggerDto trigger, TriggerCandidate item)
    {
        var now = DateTimeOffset.UtcNow;
        TriggerDuration.TryParse(trigger.Schedule.InitialDelay, out var initialDelay);

        return new SurveyInstance
        {
            SurveyID = survey.ID,
            SurveyVersionID = version.ID,
            TriggeredAt = now,
            TriggeredBy = $"event:{trigger.EventKind}",
            Status = SurveyInstanceStatus.Pending,
            CustomerRef = item.Recipient.CustomerRef,
            MetaDataJson = item.Payload.ValueKind != JsonValueKind.Undefined
                ? item.Payload.GetRawText()
                : null,
            TriggerId = trigger.Id,
            Channel = trigger.Channel,
            RecipientAddress = item.Recipient.Address,
            RecipientLocale = item.Recipient.Locale,
            NextSendAt = now + initialDelay,
            RemindersRemaining = trigger.Schedule.Reminders.Count,
            LastSentAt = null,
            DeliveryLogJson = null,
        };
    }

    private static Dictionary<string, JsonElement> BuildEvaluationContext(
        long templateId, TriggerRecipient recipient, JsonElement candidatePayload)
    {
        var ctx = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["templateId"] = JsonSerializer.SerializeToElement(templateId),
            ["recipient.address"] = JsonSerializer.SerializeToElement(recipient.Address ?? ""),
        };
        if (recipient.Locale is not null)
            ctx["recipient.locale"] = JsonSerializer.SerializeToElement(recipient.Locale);
        if (recipient.CustomerRef is not null)
            ctx["recipient.customerRef"] = JsonSerializer.SerializeToElement(recipient.CustomerRef);

        if (candidatePayload.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in candidatePayload.EnumerateObject())
                ctx[$"candidate.{prop.Name}"] = prop.Value.Clone();
        }
        return ctx;
    }

    private static bool IsUniqueHashViolation(DbUpdateException ex)
        => ex.InnerException?.Message?.Contains(IEntityHasUniqueHash.UniqueHash, StringComparison.OrdinalIgnoreCase) ?? false;

    private record TriggerMatch(Survey Survey, SurveyVersion Version, TriggerDto Trigger);
}
