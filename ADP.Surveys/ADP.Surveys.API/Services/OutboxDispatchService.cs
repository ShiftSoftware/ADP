using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Surveys.API.Extensions;
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
/// is a future polish.
///
/// <para><b>Retry.</b> A failed dispatch is usually a subscriber blip — a Service Bus
/// timeout, a webhook restart — not a poisoned payload, so failures are retried with
/// exponential backoff rather than parked on first error. The event stays
/// <see cref="SurveyOutboxEventStatus.Failed"/> between attempts (that status has always
/// meant "last attempt failed") and carries a <c>NextAttemptAt</c>. Once the attempt budget
/// is spent it flips to <see cref="SurveyOutboxEventStatus.DeadLettered"/> and is never
/// picked up again — that is the status to alert on, and requeueing is a matter of setting
/// it back to Pending.</para>
/// </summary>
public class OutboxDispatchService
{
    private readonly ShiftDbContext db;
    private readonly OutboxSubscriberRegistry subscribers;
    private readonly SurveyApiOptions options;
    private readonly ILogger<OutboxDispatchService> logger;

    public OutboxDispatchService(
        ShiftDbContext db,
        OutboxSubscriberRegistry subscribers,
        SurveyApiOptions options,
        ILogger<OutboxDispatchService> logger)
    {
        this.db = db;
        this.subscribers = subscribers;
        this.options = options;
        this.logger = logger;
    }

    public async Task<OutboxDispatchResult> PollOnceAsync(int batchSize = 100, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var maxAttempts = Math.Max(1, options.OutboxMaxAttempts);

        // Pending (never tried) plus Failed rows whose backoff has elapsed and that still
        // have attempts left. DeadLettered is excluded by construction.
        var pending = await db.Set<SurveyOutboxEvent>()
            .Where(e => !e.IsDeleted)
            .Where(e => e.Status == SurveyOutboxEventStatus.Pending
                || (e.Status == SurveyOutboxEventStatus.Failed
                    && e.Attempts < maxAttempts
                    && (e.NextAttemptAt == null || e.NextAttemptAt <= now)))
            .OrderBy(e => e.CreateDate)
            .Take(batchSize)
            .ToListAsync(ct);

        int dispatched = 0, failed = 0, deadLettered = 0;
        foreach (var evt in pending)
        {
            try
            {
                await DispatchOneAsync(evt, ct);
            }
            catch (Exception ex)
            {
                // Dispatch itself blew up rather than a subscriber returning failure —
                // same budget, same backoff, so a systemic fault can't spin forever.
                logger.LogError(ex, "Outbox dispatch failed unexpectedly for event {EventId}", evt.ID);
                evt.Attempts++;
                ApplyFailure(evt, ex.Message, DateTimeOffset.UtcNow);
                await db.SaveChangesAsync(ct);
            }

            if (evt.Status == SurveyOutboxEventStatus.Dispatched) dispatched++;
            else if (evt.Status == SurveyOutboxEventStatus.Failed) failed++;
            else if (evt.Status == SurveyOutboxEventStatus.DeadLettered) deadLettered++;
        }

        return new OutboxDispatchResult(dispatched, failed, deadLettered);
    }

    /// <summary>
    /// Records a failed attempt: either schedule the next one, or give up permanently once
    /// the budget is spent. Attempts must already have been incremented by the caller.
    /// </summary>
    private void ApplyFailure(SurveyOutboxEvent evt, string? error, DateTimeOffset now)
    {
        evt.LastError = error;

        if (evt.Attempts >= Math.Max(1, options.OutboxMaxAttempts))
        {
            evt.Status = SurveyOutboxEventStatus.DeadLettered;
            evt.NextAttemptAt = null;
            logger.LogError(
                "Outbox event {EventId} dead-lettered after {Attempts} attempts. Last error: {Error}",
                evt.ID, evt.Attempts, error);
            return;
        }

        evt.Status = SurveyOutboxEventStatus.Failed;
        evt.NextAttemptAt = now + BackoffFor(evt.Attempts);
    }

    /// <summary>
    /// Exponential backoff: base × 2^(attempts-1), clamped so a long-lived outage doesn't
    /// push the next attempt weeks out.
    /// </summary>
    private TimeSpan BackoffFor(int attempts)
    {
        var baseDelay = options.OutboxRetryBackoff;
        if (baseDelay <= TimeSpan.Zero) return TimeSpan.Zero;

        // Cap the exponent before multiplying — 2^n on a TimeSpan overflows fast.
        var exponent = Math.Min(Math.Max(attempts - 1, 0), 16);
        var scaled = baseDelay * Math.Pow(2, exponent);
        return scaled > options.OutboxRetryBackoffCap ? options.OutboxRetryBackoffCap : scaled;
    }

    private async Task DispatchOneAsync(SurveyOutboxEvent evt, CancellationToken ct)
    {
        evt.Attempts++;

        // A payload that won't deserialize is a poison message, not a blip — retrying it
        // cannot succeed, so skip the budget and dead-letter immediately.
        SurveyOutboxPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<SurveyOutboxPayload>(evt.PayloadJson, SurveySchemaSerializer.Options);
        }
        catch (Exception ex)
        {
            DeadLetter(evt, $"Payload deserialize: {ex.Message}");
            await db.SaveChangesAsync(ct);
            return;
        }
        if (payload is null)
        {
            DeadLetter(evt, "Payload deserialized to null.");
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
            ApplyFailure(evt, log.LastOrDefault(e => !e.Success)?.Error, DateTimeOffset.UtcNow);
        }
        else
        {
            evt.Status = SurveyOutboxEventStatus.Dispatched;
            evt.DispatchedAt = DateTimeOffset.UtcNow;
            evt.NextAttemptAt = null;
            evt.LastError = null;
        }

        await db.SaveChangesAsync(ct);
    }

    private void DeadLetter(SurveyOutboxEvent evt, string error)
    {
        evt.Status = SurveyOutboxEventStatus.DeadLettered;
        evt.NextAttemptAt = null;
        evt.LastError = error;
        logger.LogError("Outbox event {EventId} dead-lettered: {Error}", evt.ID, error);
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

/// <param name="Dispatched">Events all subscribers accepted this tick.</param>
/// <param name="Failed">Events that failed but will be retried.</param>
/// <param name="DeadLettered">Events that gave up this tick — the number to alert on.</param>
public record OutboxDispatchResult(int Dispatched, int Failed, int DeadLettered = 0);
