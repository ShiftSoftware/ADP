using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ShiftSoftware.ADP.Menus.API.Extensions;

internal class MenuRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel prefix;
    private readonly string menuApiAssemblyName;

    public MenuRoutePrefixConvention(string routePrefix)
    {
        prefix = new AttributeRouteModel(new RouteAttribute(routePrefix));
        menuApiAssemblyName = typeof(MenuRoutePrefixConvention).Assembly.GetName().Name!;
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.Assembly.GetName().Name != menuApiAssemblyName)
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
