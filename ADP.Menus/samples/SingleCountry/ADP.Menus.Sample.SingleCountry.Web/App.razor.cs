using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;

namespace ShiftSoftware.ADP.Menus.Sample.SingleCountry.Web
{
    public partial class App
    {
        // Users sign in on the external sample identity server, which also hosts the identity dashboard.
        private readonly ShiftIdentityHostingTypes shiftIdentityHostingTypes = ShiftIdentityHostingTypes.External;
        private readonly List<System.Reflection.Assembly> additionalAssemblies = new() { typeof(ShiftIdentityBlazorMaker).Assembly };
    }
}
