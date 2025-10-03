using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_BrokerModel : IRegionProps
{
    public long ID { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public IEnumerable<TBP_BrokerAccountNumberModel> AccountNumbers { get; set; }
    public long? RegionID { get; set; }
    public string RegionHashID { get; set; }
    public IEnumerable<TBP_BrokerBrokerBrandAccessModel> BrandAccesses { get; set; }
}
