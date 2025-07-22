using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

public class BrokerModel: ICompanyProps
{
    public string id { get; set; }
    public long ID { get; set; }
    public string Name { get; set; }
    public DateTime? AccountStartDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public IEnumerable<string> AccountNumbers { get; set; }
    public bool IsDeleted { get; set; }
}