using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ShiftSoftware.ADP.Models.Enums
{
    public enum Franchises
    {
        [Description("Toyota")]
        Toyota = 0,
        [Description("Lexus")]
        Lexus = 1,
        [Description("Hino")]
        Hino = 2,
        [Description("Other")]
        Other = 3,
    }
}
