using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_BrokerModel : ICompanyProps, IRegionProps
{
    public long ID { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public bool IsDeleted { get; set; }
    public IEnumerable<string> AccountNumbers { get; set; }
    public string CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string RegionID { get; set; }
    public string RegionHashID { get; set; }
    public IEnumerable<TBP_BrokerBrokerBrandAccessModel> BrandAccesses { get; set; }
}
