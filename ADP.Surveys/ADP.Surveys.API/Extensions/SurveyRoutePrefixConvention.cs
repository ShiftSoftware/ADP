using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ShiftSoftware.ADP.Surveys.API.Extensions;

internal class SurveyRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel prefix;
    private readonly string surveysApiAssemblyName;

    public SurveyRoutePrefixConvention(string routePrefix)
    {
        prefix = new AttributeRouteModel(new RouteAttribute(routePrefix));
        surveysApiAssemblyName = typeof(SurveyRoutePrefixConvention).Assembly.GetName().Name!;
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.Assembly.GetName().Name != surveysApiAssemblyName)
                continue;

            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is not null
                    ? AttributeRouteModel.CombineAttributeRouteModel(prefix, selector.AttributeRouteModel)
                    : prefix;
            }
        }
    }
}
