using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class RedeemQRCodeDTO
{
    public string VIN { get; set; } = default!;
    public long ToyotaLoyaltyProgramRedeemableItemID { get; set; }
    public long? TLPPackageInvoiceTLPItemID { get; set; }
    public long? ModelCostID { get; set; }
    public VehcileServiceItemTypes Type { get; set; }
}
