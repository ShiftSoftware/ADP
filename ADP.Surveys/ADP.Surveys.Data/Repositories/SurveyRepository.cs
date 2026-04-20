using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.Data.Repositories;

public class SurveyRepository : ShiftRepository<ShiftDbContext, Survey, SurveyListDTO, SurveyAdminDTO>
{
    public SurveyRepository(ShiftDbContext db) : base(db)
    {
    }
}
