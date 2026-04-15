using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuHashId : JsonHashIdConverterAttribute<MenuHashId>
{
    public MenuHashId() : base(5)
    {
        
    }
}
