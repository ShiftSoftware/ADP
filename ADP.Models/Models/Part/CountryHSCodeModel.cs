using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CountryHSCodeModel
{
    public string CountryIntegrationID { get; set; }
    public IEnumerable<string> HSCode { get; set; }
}
