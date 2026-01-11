using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_BrokerBrokerBrandAccessModel
{
    public long BrandID { get; set; }
    public bool Active { get; set; }
    public DateTime? AccountStartDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? LocationCode { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public List<long> ServiceCompanies { get; set; } = new();
}
