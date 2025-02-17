using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.JsonConverters;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

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
