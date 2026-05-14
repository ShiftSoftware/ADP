using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Surveys.API.Channels;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Slice 4 polling scheduler. One tick = one batch of due-and-active instances pulled
/// from the DB, dispatched to their snapshotted channel, then advanced (NextSendAt
/// rolls forward to the next reminder slot, or nulls out when reminders are exhausted).
///
/// Production needs a periodic invoker (BackgroundService or Hangfire) calling
/// <see cref="PollOnceAsync"/> on an interval — that wiring is a slice 4 follow-up.
/// For testing and ad-hoc operator use, the admin endpoint
/// <c>POST /api/Surveys/Triggers/scheduler/tick</c> drives a single tick.
///
/// Slice 5 will hook into the response-submit path to clear NextSendAt on completion.
/// Slice 6 will add the "all reminders exhausted with no response → Expired" terminal.
/// </summary>
public class TriggerSchedulerService
{
    private readonly ShiftDbContext db;
    private readonly SurveyChannelRegistry channels;
    private readonly SurveyApiOptions options;
    private readonly ILogger<TriggerSchedulerService> logger;

    public TriggerSchedulerService(
        ShiftDbContext db,
        SurveyChannelRegistry channels,
        SurveyApiOptions options,
        ILogger<TriggerSchedulerService> logger)
    {
        this.db = db;
        this.channels = channels;
        this.options = options;
        this.logger = logger;
    }

    public async Task<SchedulerTickResult> PollOnceAsync(int batchSize = 100, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var processed = await SendDueAsync(now, batchSize, ct);
        var expired = await SweepExpiredAsync(now, batchSize, ct);
        return new SchedulerTickResult(processed, expired);
    }

    private async Task<int> SendDueAsync(DateTimeOffset now, int batchSize, CancellationToken ct)
    {
        var due = await db.Set<SurveyInstance>()
            .Include(i => i.SurveyVersion)
            .Where(i => !i.IsDeleted)
            .Where(i => i.Status != SurveyInstanceStatus.Completed && i.Status != SurveyInstanceStatus.Expired)
            .Where(i => i.NextSendAt != null && i.NextSendAt <= now)
            .OrderBy(i => i.NextSendAt)
            .Take(batchSize)
            .ToListAsync(ct);

        int processed = 0;
        foreach (var instance in due)
        {
            try
            {
                await ProcessOneAsync(instance, now, ct);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduler tick failed for SurveyInstance {PublicId}", instance.PublicID);
            }
        }
        return processed;
    }

    /// <summary>
    /// Slice 6: flips rows whose schedule has run out (<c>NextSendAt IS NULL</c>) and that
    /// have been quiet past <see cref="SurveyApiOptions.ExpiryGracePeriod"/> to
    /// <see cref="SurveyInstanceStatus.Expired"/>. Covers three lifecycles:
    /// (a) all reminders sent, no submission — measured from <c>LastSentAt</c>;
    /// (b) misconfigured channel never delivered — measured from <c>TriggeredAt</c> since
    /// <c>LastSentAt</c> is null in that case;
    /// (c) ingest happy path, scheduler hasn't run yet, but enough time has passed —
    /// same path as (b).
    /// </summary>
    private async Task<int> SweepExpiredAsync(DateTimeOffset now, int batchSize, CancellationToken ct)
    {
        var cutoff = now - options.ExpiryGracePeriod;

        var stale = await db.Set<SurveyInstance>()
            .Where(i => !i.IsDeleted)
            .Where(i => i.NextSendAt == null)
            .Where(i => i.Status == SurveyInstanceStatus.Pending
                     || i.Status == SurveyInstanceStatus.Sent
                     || i.Status == SurveyInstanceStatus.Opened)
            .Where(i => (i.LastSentAt != null && i.LastSentAt < cutoff)
                     || (i.LastSentAt == null && i.TriggeredAt < cutoff))
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var instance in stale)
            instance.Status = SurveyInstanceStatus.Expired;

        if (stale.Count > 0)
            await db.SaveChangesAsync(ct);

        return stale.Count;
    }

    private async Task ProcessOneAsync(SurveyInstance instance, DateTimeOffset now, CancellationToken ct)
    {
        var channel = channels.Resolve(instance.Channel);
        if (channel is null)
        {
            // No channel registered under the snapshotted key — give up on this instance.
            // Logging this loudly because it's a deployment misconfiguration: an authored
            // trigger references a channel that the runtime doesn't have. Slice 4 just
            // stops trying so we don't tight-loop; alerting / re-queueing is later.
            logger.LogWarning(
                "SurveyInstance {PublicId} references unregistered channel '{Channel}'; nulling NextSendAt to halt retries",
                instance.PublicID, instance.Channel);
            AppendDeliveryLog(instance, attempt: 0, status: "no-channel",
                error: $"Channel '{instance.Channel}' not registered. Registered keys: [{string.Join(",", channels.RegisteredKeys)}]");
            instance.NextSendAt = null;
            await db.SaveChangesAsync(ct);
            return;
        }

        var attemptNumber = ComputeAttemptNumber(instance);
        var url = options.PublicSurveyUrlTemplate.Replace("{publicId}", instance.PublicID.ToString());

        var result = await channel.SendAsync(new ChannelSendRequest
        {
            PublicId = instance.PublicID,
            Address = instance.RecipientAddress ?? "",
            Locale = instance.RecipientLocale,
            SurveyUrl = url,
            AttemptNumber = attemptNumber,
            CandidateMetadataJson = instance.MetaDataJson,
        }, ct);

        AppendDeliveryLog(
            instance,
            attempt: attemptNumber,
            status: result.Delivered ? "delivered" : "failed",
            messageId: result.ProviderMessageId,
            error: result.Error);

        if (result.Delivered)
        {
            instance.LastSentAt = now;
            if (instance.Status == SurveyInstanceStatus.Pending)
                instance.Status = SurveyInstanceStatus.Sent;

            AdvanceNextSendAt(instance, now);
        }
        // On failed: leave NextSendAt as-is so the next poll retries. Backoff/give-up
        // logic is a later slice — for now a transient channel hiccup just retries on
        // the next tick. (Production interval ≈ 60s per the legacy Hangfire pattern.)

        await db.SaveChangesAsync(ct);
    }

    private void AdvanceNextSendAt(SurveyInstance instance, DateTimeOffset sentAt)
    {
        TriggerDto? trigger = LookupTrigger(instance);
        if (trigger is null)
        {
            instance.NextSendAt = null;
            instance.RemindersRemaining = 0;
            return;
        }

        // RemindersRemaining counts down with each send. The reminder array index for
        // the next send is (totalReminders - RemindersRemaining). When that index goes
        // out-of-range, no more reminders to schedule.
        var totalReminders = trigger.Schedule.Reminders.Count;
        var nextReminderIndex = totalReminders - instance.RemindersRemaining;

        if (nextReminderIndex < totalReminders
            && TriggerDuration.TryParse(trigger.Schedule.Reminders[nextReminderIndex], out var delay))
        {
            instance.NextSendAt = sentAt + delay;
            instance.RemindersRemaining--;
        }
        else
        {
            instance.NextSendAt = null;
        }
    }

    private static TriggerDto? LookupTrigger(SurveyInstance instance)
    {
        if (string.IsNullOrEmpty(instance.TriggerId)) return null;
        try
        {
            var schema = JsonSerializer.Deserialize<SurveyDto>(
                instance.SurveyVersion.ResolvedJson, SurveySchemaSerializer.Options);
            return schema?.Triggers.FirstOrDefault(t => t.Id == instance.TriggerId);
        }
        catch
        {
            return null;
        }
    }

    private static int ComputeAttemptNumber(SurveyInstance instance)
    {
        if (string.IsNullOrEmpty(instance.DeliveryLogJson)) return 1;
        try
        {
            var entries = JsonSerializer.Deserialize<List<DeliveryLogEntry>>(instance.DeliveryLogJson);
            return (entries?.Count ?? 0) + 1;
        }
        catch
        {
            return 1;
        }
    }

    private static void AppendDeliveryLog(
        SurveyInstance instance, int attempt, string status, string? messageId = null, string? error = null)
    {
        List<DeliveryLogEntry> entries = new();
        if (!string.IsNullOrEmpty(instance.DeliveryLogJson))
        {
            try { entries = JsonSerializer.Deserialize<List<DeliveryLogEntry>>(instance.DeliveryLogJson) ?? new(); }
            catch { entries = new(); }
        }
        entries.Add(new DeliveryLogEntry
        {
            Attempt = attempt,
            SentAt = DateTimeOffset.UtcNow,
            Status = status,
            MessageId = messageId,
            Error = error,
        });
        instance.DeliveryLogJson = JsonSerializer.Serialize(entries, SurveySchemaSerializer.Options);
    }

    private class DeliveryLogEntry
    {
        public int Attempt { get; set; }
        public DateTimeOffset SentAt { get; set; }
        public string Status { get; set; } = "";
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }
}

public record SchedulerTickResult(int Processed, int Expired);
