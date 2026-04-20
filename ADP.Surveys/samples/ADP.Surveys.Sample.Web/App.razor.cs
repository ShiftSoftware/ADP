using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor;

namespace ShiftSoftware.ADP.Surveys.Sample.Web;

public partial class App
{
    private ShiftIdentityHostingTypes shiftIdentityHostingTypes;
    private List<System.Reflection.Assembly> additionalAssemblies;

    public App()
    {
        additionalAssemblies = new()
        {
            typeof(ShiftIdentityBlazorMaker).Assembly,
            typeof(ShiftIdentityDashboarBlazorMaker).Assembly,
        };
        shiftIdentityHostingTypes = ShiftIdentityHostingTypes.Internal;
    }
}
