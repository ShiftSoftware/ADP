using ShiftSoftware.ADP.Surveys.Web.Extensions;

namespace ShiftSoftware.ADP.Surveys.Web.WebServices;

public class ScreenTemplateService
{
    private readonly HttpClient http;
    private readonly string prefix;

    public ScreenTemplateService(HttpClient http, SurveysWebOptions options)
    {
        this.http = http;
        this.prefix = options.ResolvedRoutePrefix;
    }
}
