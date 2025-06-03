using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_BrokerBrokerBrandAccessModel
{
    public long BrandID { get; set; }
    public Brands Brand { get; set; }
    public bool Active { get; set; }
    public DateTime? AccountStartDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}
