using ShiftSoftware.ADP.Surveys.Web.Extensions;

namespace ShiftSoftware.ADP.Surveys.Web.WebServices;

/// <summary>
/// Placeholder HTTP service for bank-question operations beyond the framework's stock
/// CRUD. Left thin on purpose — most interaction is through <c>ShiftList</c> /
/// <c>ShiftEntityForm</c> binding directly to the <c>BankQuestionController</c>.
/// Will grow when the builder needs custom bank operations (e.g. "find surveys using
/// this entry").
/// </summary>
public class BankQuestionService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public BankQuestionService(HttpClient http, SurveysWebOptions options)
    {
        this.http = http;
        this.prefix = options.ResolvedRoutePrefix;
    }
}
