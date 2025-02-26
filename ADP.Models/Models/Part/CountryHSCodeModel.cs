using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Part;

public class CountryHSCodeModel : ICountryProps
{
    public string CountryIntegrationID { get; set; }
    public IEnumerable<string> HSCode { get; set; }
}