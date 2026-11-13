using ShiftSoftware.ShiftIdentity.Blazor;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.Blazor;

namespace ShiftSoftware.ADP.Menus.Sample.MultiCountry.Web
{
    public partial class App
    {
        private ShiftIdentityHostingTypes shiftIdentityHostingTypes;
        private List<System.Reflection.Assembly> additionalAssemblies = new();

        [Microsoft.AspNetCore.Components.Inject]
        private SampleIdentity SampleIdentity { get; set; } = default!;

        protected override void OnInitialized()
        {
            additionalAssemblies = new() { typeof(ShiftIdentityBlazorMaker).Assembly };

            // The dashboard's pages are registered only with the fake identity (see Program.cs).
            if (!SampleIdentity.IsExternal)
                additionalAssemblies.Add(typeof(ShiftIdentityDashboarBlazorMaker).Assembly);

            shiftIdentityHostingTypes = SampleIdentity.IsExternal ? ShiftIdentityHostingTypes.External : ShiftIdentityHostingTypes.Internal;
        }
    }
}
