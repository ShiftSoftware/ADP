using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class TIQOfficialVINCosmosModel : CacheableCSV
{
    public new string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string Model { get; set; } = default!;
    public DateTime Date { get; set; }
    public string ItemType => "TIQOfficialVIN";
}
