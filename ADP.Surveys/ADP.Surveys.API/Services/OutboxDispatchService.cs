using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Surveys.API.Subscribers;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Drains the <see cref="SurveyOutboxEvent"/> table — one tick = one batch of pending
/// events, each fanned out to every registered <see cref="ISurveyResponseSubscriber"/>.
///
/// Slice 7: every subscriber receives every event. Filtering (per-survey or per-subscriber)
/// is a future polish. Retry semantics: if any subscriber throws or returns failure, the
/// event is marked <see cref="SurveyOutboxEventStatus.Failed"/> and stays put — no automatic
/// retry yet. Operations team alerts on <c>Status = Failed</c> rows.
/// </summary>
public class OutboxDispatchService
{
    private readonly ShiftDbContext db;
    private readonly OutboxSubscriberRegistry subscribers;
    private readonly ILogger<OutboxDispatchService> logger;

    public OutboxDispatchService(
        ShiftDbContext db,
        OutboxSubscriberRegistry subscribers,
        ILogger<OutboxDispatchService> logger)
    {
        this.db = db;
        this.subscribers = subscribers;
        this.logger = logger;
    }

    public async Task<OutboxDispatchResult> PollOnceAsync(int batchSize = 100, CancellationToken ct = default)
    {
        var pending = await db.Set<SurveyOutboxEvent>()
            .Where(e => !e.IsDeleted)
            .Where(e => e.Status == SurveyOutboxEventStatus.Pending)
            .OrderBy(e => e.CreateDate)
            .Take(batchSize)
            .ToListAsync(ct);

        int dispatched = 0, failed = 0;
        foreach (var evt in pending)
        {
            try
            {
                await DispatchOneAsync(evt, ct);
                if (evt.Status == SurveyOutboxEventStatus.Dispatched) dispatched++;
                else if (evt.Status == SurveyOutboxEventStatus.Failed) failed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch failed unexpectedly for event {EventId}", evt.ID);
                evt.Status = SurveyOutboxEventStatus.Failed;
                evt.LastError = ex.Message;
                evt.Attempts++;
                await db.SaveChangesAsync(ct);
                failed++;
            }
        }

        return new OutboxDispatchResult(dispatched, failed);
    }

    private async Task DispatchOneAsync(SurveyOutboxEvent evt, CancellationToken ct)
    {
        evt.Attempts++;

        SurveyOutboxPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SurveyOutboxPayload>(evt.PayloadJson, SurveySchemaSerializer.Options);
        }
        catch (Exception ex)
        {
            evt.Status = SurveyOutboxEventStatus.Failed;
            evt.LastError = $"Payload deserialize: {ex.Message}";
            await db.SaveChangesAsync(ct);
            return;
        }
        if (payload is null)
        {
            evt.Status = SurveyOutboxEventStatus.Failed;
            evt.LastError = "Payload deserialized to null.";
            await db.SaveChangesAsync(ct);
            return;
        }

        var log = LoadLog(evt);
        bool anyFailed = false;

        foreach (var subscriber in subscribers.All)
        {
            var entry = new DispatchLogEntry { Key = subscriber.Key, DispatchedAt = DateTimeOffset.UtcNow };
            try
            {
                var result = await subscriber.DispatchAsync(payload, ct);
                entry.Success = result.Success;
                entry.Error = result.Error;
                if (!result.Success) anyFailed = true;
            }
            catch (Exception ex)
            {
                entry.Success = false;
                entry.Error = ex.Message;
                anyFailed = true;
            }
            log.Add(entry);
        }

        evt.DispatchLogJson = JsonSerializer.Serialize(log, SurveySchemaSerializer.Options);

        if (anyFailed)
        {
            evt.Status = SurveyOutboxEventStatus.Failed;
            evt.LastError = log.LastOrDefault(e => !e.Success)?.Error;
        }
        else
        {
            evt.Status = SurveyOutboxEventStatus.Dispatched;
            evt.DispatchedAt = DateTimeOffset.UtcNow;
            evt.LastError = null;
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<DispatchLogEntry> LoadLog(SurveyOutboxEvent evt)
    {
        if (string.IsNullOrEmpty(evt.DispatchLogJson)) return new();
        try { return JsonSerializer.Deserialize<List<DispatchLogEntry>>(evt.DispatchLogJson) ?? new(); }
        catch { return new(); }
    }

    private class DispatchLogEntry
    {
        public string Key { get; set; } = "";
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTimeOffset DispatchedAt { get; set; }
    }
}

public record OutboxDispatchResult(int Dispatched, int Failed);
