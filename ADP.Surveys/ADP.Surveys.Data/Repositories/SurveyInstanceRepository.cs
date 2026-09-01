using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

/// <summary>
/// List-only repository backing the dashboard's Responses grid.
/// <c>SurveyInstanceController</c> exposes just the OData Get — instances are
/// created by trigger ingest / the test-run action and mutated by the public
/// submit + scheduler paths, never through admin CRUD.
///
/// <para>
/// <b>The SurveyInstance / SurveyInstanceAdminDTO pair is NOT dead code, despite every CRUD route
/// returning 405.</b> <c>ShiftEntityMapperValidation</c> checks EVERY triple at startup, so this
/// one must resolve a mapper or the application does not boot. The old profile carried a bare
/// <c>CreateMap(...).ReverseMap()</c> for the same reason; the convention now supplies it, which is
/// why nothing is configured for that direction below.
/// </para>
///
/// <para>
/// <b>Its write mapper is live but HTTP-unreachable</b> — driven from the public submit and
/// trigger-ingest paths rather than PUT/POST. The harness alone therefore covers it not at all,
/// which is why it is recorded <c>httpWriteReachable: false</c> and backed by a mapper-level
/// golden test instead.
/// </para>
/// </summary>
public class SurveyInstanceRepository : ShiftRepository<ShiftDbContext, SurveyInstance, SurveyInstanceListDTO, SurveyInstanceAdminDTO>
{
    public SurveyInstanceRepository(ShiftDbContext db) : base(db, x => x.UseGeneratedMapper(map => map

        // ── LIST ──────────────────────────────────────────────────────────────────────────
        // Every member here is spliced into ONE SQL projection, so all five must stay
        // EF-translatable: a constant comparison, an enum cast, a navigation hop, and two
        // subqueries. Unlike the two JSON-derived members on BankQuestion and ScreenTemplate,
        // nothing here is a method call - and nothing here may become one.

        .ForList(d => d.IsTest, e => e.TriggeredBy == SurveysConstants.DashboardTestTriggerSource)

        // Status needs NOTHING: the convention already emits `Status = (int)e.Status` into
        // __shiftListProjection, verified in the emitted code. The old profile restated it because
        // AutoMapper had no such convention. Deleted rather than restated (conventions.md section 3)
        // - a redundant ForList is indistinguishable, to the next reader, from one that is doing
        // real work.

        .ForList(d => d.SchemaVersion, e => e.SurveyVersion.Version)

        // TRAP 1. The `!IsDeleted` predicate is the entire point of these two lines.
        //
        // Drop it and the count silently includes soft-deleted responses: the endpoint still
        // returns 200, the body still has the right shape, and the number is still plausible. No
        // diagnostic fires, no test fails on shape. The ONLY thing that catches it is a value diff
        // against a baseline whose seed contains a soft-deleted response - which the Surveys parity
        // seed does, on instance 5200001, precisely so this line is guarded.
        .ForList(d => d.ResponseCount, e => e.Responses.Count(r => !r.IsDeleted))

        // TRAP 1, same shape and the same reasoning: without the filter a soft-deleted response
        // could supply the Max and the instance would report a completion time that no live
        // response accounts for.
        .ForList(d => d.CompletedAt, e => e.Responses.Where(r => !r.IsDeleted).Max(r => r.CompletedAt))))
    {
    }
}
