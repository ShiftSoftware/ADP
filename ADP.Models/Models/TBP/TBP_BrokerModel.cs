using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.TBP;

public class TBP_BrokerModel
{
    public long ID { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? TerminationDate { get; set; }
    public DateTimeOffset? AccountStartDate { get; set; }
    public bool IsDeleted { get; set; }
    public IEnumerable<string> AccountNumbers { get; set; }
}
