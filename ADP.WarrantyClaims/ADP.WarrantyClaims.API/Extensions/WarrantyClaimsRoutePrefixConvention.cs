using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Extensions;

internal class WarrantyClaimsRoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel prefix;
    private readonly string apiAssemblyName;

    public WarrantyClaimsRoutePrefixConvention(string routePrefix)
    {
        prefix = new AttributeRouteModel(new RouteAttribute(routePrefix));
        apiAssemblyName = typeof(WarrantyClaimsRoutePrefixConvention).Assembly.GetName().Name!;
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.Assembly.GetName().Name != apiAssemblyName)
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
