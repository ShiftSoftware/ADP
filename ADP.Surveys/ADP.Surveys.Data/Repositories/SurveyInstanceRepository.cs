using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

/// <summary>
/// List-only repository backing the dashboard's Responses grid.
/// <c>SurveyInstanceController</c> exposes just the OData Get — instances are
/// created by trigger ingest / the test-run action and mutated by the public
/// submit + scheduler paths, never through admin CRUD.
/// </summary>
public class SurveyInstanceRepository : ShiftRepository<ShiftDbContext, SurveyInstance, SurveyInstanceListDTO, SurveyInstanceAdminDTO>
{
    public SurveyInstanceRepository(ShiftDbContext db) : base(db)
    {
    }
}
