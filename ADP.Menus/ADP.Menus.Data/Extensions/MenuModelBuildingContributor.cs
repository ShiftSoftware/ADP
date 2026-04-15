using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Menus.Data.Extensions;

public class MenuModelBuildingContributor : IModelBuildingContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureMenuEntities();
    }
}
