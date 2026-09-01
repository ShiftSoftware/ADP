using System.Collections;
using System.Reflection;
using ShiftSoftware.ShiftEntity.Core;
using Xunit;
using Entities = ShiftSoftware.ADP.Surveys.Data.Entities;
using Dtos = ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;

namespace ShiftSoftware.ADP.Surveys.Data.Tests;

/// <summary>
/// The mapper-level write golden for the <c>SurveyInstance</c> triple.
///
/// <para>
/// <b>Why this test exists at all.</b> <c>SurveyInstanceController</c> overrides <c>GetSingle</c>,
/// <c>Post</c>, <c>Put</c>, <c>Delete</c>, <c>GetRevisions</c>, <c>Print</c> and <c>PrintToken</c> to
/// return 405, so no HTTP request can ever reach this triple's write mapper - yet the mapper is
/// live, driven from the public submit and trigger-ingest paths. An endpoint-level test suite
/// therefore produces a handful of 405 transcripts, reports "no server errors", and covers this
/// write mapper <b>not at all</b>. A member silently becoming writable here would be structurally
/// invisible to it. This is the substitute, and it is why <c>SurveyInstance</c> is recorded
/// <c>writeUnreachable</c> rather than simply passing.
/// </para>
///
/// <para>
/// <b>What it pins.</b> <c>SurveyInstanceAdminDTO</c> carries exactly one member of its own -
/// <c>ID</c> - and exists only to satisfy the framework's <c>ViewAndUpsert</c> generic. So the
/// correct written member set is just the inherited audit and soft-delete members, and every one of
/// the sixteen domain members - the scheduler's <c>NextSendAt</c> / <c>RemindersRemaining</c>, the
/// delivery log, the recipient address, the lifecycle <c>Status</c> - must be left untouched.
/// Writing any of them would blank live scheduler state with a default, because the DTO has no
/// value to supply.
/// </para>
///
/// <para>
/// <b>Why it diffs reflectively instead of listing names.</b> A hand-written expected list only
/// covers the members someone remembered on the day. Adding a property to <c>SurveyInstance</c>, or
/// to <c>SurveyInstanceAdminDTO</c>, changes what the convention writes - and the risk is precisely
/// the member nobody thought to add to a list. Diffing every scalar property before and after the
/// map means a new member is covered the moment it is declared, and the test fails naming it.
/// </para>
/// </summary>
public class SurveyInstanceWriteMapperGoldenTests
{
    /// <summary>
    /// The complete set of members the write mapper is permitted to touch: the audit and
    /// soft-delete members every ShiftEntity carries. Checked against the pre-migration reverse
    /// map, which was a bare <c>CreateMap(SurveyInstance, SurveyInstanceAdminDTO).ReverseMap()</c>
    /// over a DTO with no domain members - i.e. exactly these and nothing else.
    /// </summary>
    private static readonly string[] PermittedWrites =
    {
        "CreateDate", "LastSaveDate", "CreatedByUserID", "LastSavedByUserID", "IsDeleted",
    };

    [Fact]
    public void The_write_mapper_touches_only_the_audit_members_and_no_domain_member()
    {
        var mapper = ResolveMapper();

        // Every domain member gets a distinctive, NON-DEFAULT value. That is load-bearing: the DTO
        // has nothing to supply for them, so if the convention ever started writing one it could
        // only write a default - which shows up as a change precisely because the seed is not one.
        var entity = new Entities.SurveyInstance
        {
            PublicID           = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            SurveyID           = 4242,
            SurveyVersionID    = 8484,
            TriggeredAt        = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.Zero),
            TriggeredBy        = "golden-trigger-source",
            Status             = Entities.SurveyInstanceStatus.Completed,
            CustomerRef        = "golden-customer-ref",
            MetaDataJson       = "{\"golden\":\"metadata\"}",
            TriggerId          = "golden-trigger-id",
            Channel            = "golden-channel",
            RecipientAddress   = "golden@example.invalid",
            RecipientLocale    = "ku-IQ",
            NextSendAt         = new DateTimeOffset(2024, 4, 5, 6, 7, 8, TimeSpan.Zero),
            LastSentAt         = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero),
            RemindersRemaining = 7,
            DeliveryLogJson    = "[{\"golden\":\"log\"}]",
        };

        // The audit members are seeded DIFFERENTLY from what the DTO carries, so that "was written"
        // is observable. Without this the permitted writes would be indistinguishable from no-ops
        // and the test would pass just as happily against a mapper that wrote nothing at all.
        entity.CreateDate        = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        entity.LastSaveDate      = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        entity.CreatedByUserID   = 1;
        entity.LastSavedByUserID = 1;
        entity.IsDeleted         = false;

        var before = Snapshot(entity);

        var dto = new Dtos.SurveyInstanceAdminDTO
        {
            CreateDate        = new DateTimeOffset(2026, 9, 9, 9, 9, 9, TimeSpan.Zero),
            LastSaveDate      = new DateTimeOffset(2026, 9, 9, 9, 9, 9, TimeSpan.Zero),
            CreatedByUserID   = "99",
            LastSavedByUserID = "99",
            IsDeleted         = true,
        };

        mapper.MapToEntity(dto, entity);

        var after = Snapshot(entity);
        var written = before.Where(kv => !Equals(kv.Value, after[kv.Key]))
                            .Select(kv => kv.Key)
                            .OrderBy(x => x, StringComparer.Ordinal)
                            .ToArray();

        Assert.Equal(
            PermittedWrites.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            written);
    }

    [Fact]
    public void Every_domain_member_is_actually_covered_by_the_diff()
    {
        // A guard on the guard. The test above proves "nothing outside the audit set changed", which
        // is trivially true if the snapshot silently skipped the domain members - a filter bug, a
        // property that stops being readable. This asserts the snapshot really does watch them, so
        // the golden cannot quietly degrade into asserting nothing.
        var watched = Snapshot(new Entities.SurveyInstance()).Keys;

        foreach (var member in new[]
                 {
                     "PublicID", "SurveyID", "SurveyVersionID", "TriggeredAt", "TriggeredBy",
                     "Status", "CustomerRef", "MetaDataJson", "TriggerId", "Channel",
                     "RecipientAddress", "RecipientLocale", "NextSendAt", "LastSentAt",
                     "RemindersRemaining", "DeliveryLogJson",
                 })
            Assert.Contains(member, watched);
    }

    /// <summary>
    /// Resolves the generated mapper out of the Data assembly by its closed interface rather than by
    /// name. The generated type carries a content hash in its name
    /// (<c>Generated_SurveyInstance_..._1a7b2898</c>) which changes whenever the mapper
    /// configuration changes, so matching on the name would turn every legitimate edit into a
    /// broken test.
    /// </summary>
    private static IShiftEntityMapper<Entities.SurveyInstance, Dtos.SurveyInstanceListDTO, Dtos.SurveyInstanceAdminDTO> ResolveMapper()
    {
        var closed = typeof(IShiftEntityMapper<Entities.SurveyInstance, Dtos.SurveyInstanceListDTO, Dtos.SurveyInstanceAdminDTO>);

        // Two types in the assembly satisfy the closed interface: the generated mapper and
        // SurveyInstanceRepository itself, since ShiftRepository implements IShiftEntityMapper by
        // delegating to it. The repository is the wrong target here - constructing it needs a
        // DbContext, and it would only forward to the very type this golden is about - so narrow to
        // the generated-mappers namespace, which is stable even though the type name is not.
        var type = Assert.Single(
            typeof(Entities.SurveyInstance).Assembly.GetTypes(),
            t => t is { IsAbstract: false, IsInterface: false }
                 && t.Namespace == "ShiftSoftware.ShiftEntity.GeneratedMappers"
                 && closed.IsAssignableFrom(t));

        return (IShiftEntityMapper<Entities.SurveyInstance, Dtos.SurveyInstanceListDTO, Dtos.SurveyInstanceAdminDTO>)
            Activator.CreateInstance(type, nonPublic: true)!;
    }

    /// <summary>
    /// Value snapshot of every readable scalar property. Navigation properties and collections are
    /// excluded: they are reference-compared, so an untouched navigation would read as unchanged
    /// whatever happened to it, and including them would only add noise this golden cannot act on.
    /// </summary>
    private static Dictionary<string, object?> Snapshot(Entities.SurveyInstance entity) =>
        typeof(Entities.SurveyInstance)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Where(p => !IsNavigation(p.PropertyType))
            .ToDictionary(p => p.Name, p => p.GetValue(entity));

    /// <summary>
    /// A navigation is a collection, or a reference to another entity in the entity namespace.
    /// <b>Enums are explicitly not navigations</b> even though they share that namespace -
    /// <c>SurveyInstanceStatus</c> is declared alongside the entities, and treating it as one would
    /// silently drop <c>Status</c>, the single most consequential domain member here, out of the
    /// diff. The companion test asserts it is watched precisely because this is easy to get wrong.
    /// </summary>
    private static bool IsNavigation(Type t) =>
        t != typeof(string)
        && !t.IsEnum
        && (typeof(IEnumerable).IsAssignableFrom(t)
            || t.Namespace == typeof(Entities.SurveyInstance).Namespace);
}
