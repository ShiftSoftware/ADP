using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

internal class DarlasticRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel prefix;
    private readonly string darlasticApiAssemblyName;

    public DarlasticRoutePrefixConvention(string routePrefix)
    {
        prefix = new AttributeRouteModel(new RouteAttribute(routePrefix));
        darlasticApiAssemblyName = typeof(DarlasticRoutePrefixConvention).Assembly.GetName().Name!;
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.Assembly.GetName().Name != darlasticApiAssemblyName)
                continue;

            foreach (var selector in controller.Selectors)
            {
                if (selector.AttributeRouteModel is not null)
                {
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(prefix, selector.AttributeRouteModel);
                }
                else
                {
                    selector.AttributeRouteModel = prefix;
                }
            }
        }
    }
}
