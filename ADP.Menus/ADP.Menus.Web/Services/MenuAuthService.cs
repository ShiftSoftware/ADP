using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Menus.Shared.ActionTrees;
using ShiftSoftware.ADP.Menus.Web.Extensions;
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.Menus.Web.Services;

/// <summary>
/// Helper for Menu Blazor UI to check action-tree permissions.
/// When authorization is disabled (default), all checks return true so the UI behaves like before.
/// </summary>
public class MenuAuthService
{
    private readonly MenuWebOptions options;
    private readonly ITypeAuthService typeAuth;

    public MenuAuthService(IOptions<MenuWebOptions> options, ITypeAuthService typeAuth)
    {
        this.options = options.Value;
        this.typeAuth = typeAuth;
    }

    public bool Enabled => options.EnableMenuActionTreeAuthorization;

    public bool CanRead(ReadWriteDeleteAction action) => !Enabled || typeAuth.CanRead(action);
    public bool CanWrite(ReadWriteDeleteAction action) => !Enabled || typeAuth.CanWrite(action);
    public bool CanDelete(ReadWriteDeleteAction action) => !Enabled || typeAuth.CanDelete(action);
    public bool CanAccess(BooleanAction action) => !Enabled || typeAuth.CanAccess(action);

    public bool CanReadMenus => CanRead(MenuActionTree.Menus);
    public bool CanReadMenuVariants => CanRead(MenuActionTree.MenuVariants);
    public bool CanReadMenuVersions => CanRead(MenuActionTree.MenuVersions);
    public bool CanReadVehicleModels => CanRead(MenuActionTree.VehicleModels);
    public bool CanReadReplacementItems => CanRead(MenuActionTree.ReplacementItems);
    public bool CanReadServiceIntervals => CanRead(MenuActionTree.ServiceIntervals);
    public bool CanReadServiceIntervalGroups => CanRead(MenuActionTree.ServiceIntervalGroups);
    public bool CanReadBrandMappings => CanRead(MenuActionTree.BrandMappings);
    public bool CanReadLabourRateMappings => CanRead(MenuActionTree.LabourRateMappings);
    public bool CanReadStandaloneReplacementItemGroups => CanRead(MenuActionTree.StandaloneReplacementItemGroups);

    public bool CanCreateMenuVersion => CanAccess(MenuActionTree.Operations.CreateMenuVersion);
    public bool CanUpdatePartsPrice => CanAccess(MenuActionTree.Operations.UpdatePartsPrice);

    public bool CanExportMenusToExcel => CanAccess(MenuActionTree.Reports.ExportMenusToExcel);
    public bool CanExportRTSCodesToExcel => CanAccess(MenuActionTree.Reports.ExportRTSCodesToExcel);
    public bool CanExportMenuDetailReport => CanAccess(MenuActionTree.Reports.ExportMenuDetailReport);
}
