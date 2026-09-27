using ShiftSoftware.ADP.Surveys.Data.Entities;
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
/// <c>CreateMap(...).ReverseMap()</c> for the same reason; the repository's automatic maps now
/// supply it, which is why <c>Mappers/SurveysMapper.cs</c> declares only this triple's list map.
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
    // The maps - the list's test flag, schema version and soft-delete-aware response members - are in
    // Mappers/SurveysMapper.cs.
    public SurveyInstanceRepository(ShiftDbContext db) : base(db)
    {
    }
}
