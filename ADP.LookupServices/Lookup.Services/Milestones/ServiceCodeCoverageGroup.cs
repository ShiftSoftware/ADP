using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>One line of a coverage breakdown.</summary>
[Docable]
public class ServiceCodeCoverageGroup
{
    /// <summary>
    /// What this row counts — a programme, a qualifier, or a convention. Null where the codes
    /// carried no programme or no qualifier at all.
    /// </summary>
    public string Name { get; set; }

    /// <summary>Distinct codes in this row.</summary>
    public long Codes { get; set; }

    /// <summary>Labour lines in this row.</summary>
    public long Lines { get; set; }
}
