using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

public class SurveyRepository : ShiftRepository<ShiftDbContext, Survey, SurveyListDTO, SurveyAdminDTO>
{
    // The maps - the Draft JSON column and the server-owned PublishedVersionNumber - are in Mappers/SurveysMapper.cs.
    public SurveyRepository(ShiftDbContext db) : base(db)
    {
    }
}
