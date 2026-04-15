using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Menus.Shared.Enums;

public enum ReplacementItemType
{
    [Description("Lubricant")]
    Lubricant = 1,

    [Description("Component")]
    Component = 2,

    [Description("Value Added")]
    ValueAdded = 3
}
