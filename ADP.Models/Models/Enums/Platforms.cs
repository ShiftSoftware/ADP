using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ShiftSoftware.ADP.Models.Enums
{
    public enum Platforms
    {
        [Description("IOS OS")]
        IOS = 1,
        [Description("Normal Android")]
        Android = 2,
        [Description("HMS Android")]
        Huawei = 3,
    }
}
