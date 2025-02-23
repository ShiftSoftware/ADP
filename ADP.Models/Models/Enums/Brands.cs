using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums
{
    public enum Brands
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