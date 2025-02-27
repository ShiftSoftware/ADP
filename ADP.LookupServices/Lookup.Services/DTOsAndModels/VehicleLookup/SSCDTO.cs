using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class SSCDTO
{
    public string SSCCode { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IEnumerable<SSCLaborDTO> Labors { get; set; } = default!;
    public bool Repaired { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? RepairDate { get; set; }
    public IEnumerable<SSCPartDTO> Parts { get; set; }

    public SSCDTO()
    {
        Parts = new List<SSCPartDTO>();
    }
}