using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.API.Extensions;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Data.Repositories;
using ShiftSoftware.ADP.Surveys.Shared.ActionTrees;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;
using ShiftSoftware.ShiftEntity.Web;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

[Route("[controller]")]
[ApiController]
public class BankQuestionController : ShiftEntitySecureControllerAsync<BankQuestionRepository, BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO>
{
    public BankQuestionController(SurveyApiOptions options)
        : base(options.EnableSurveysActionTreeAuthorization ? SurveysActionTree.BankQuestions : null)
    {
    }
}
