using System;
using System.Collections.Generic;
using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class PaintThicknessVehicleCosmosModel
{
    public string id { get; set; } = default!;

    public long Id { get; set; }

    public long UserId { get; set; }

    public string ModelType { get; set; }

    public string ModelYear { get; set; }

    public DateTime? CreateDate { get; set; }

    public string ModelName { get; set; }

    public string ColorCode { get; set; }

    public string SellingDealership { get; set; }

    public string FolderPath { get; set; }

    public IEnumerable<string> Images { get; set; }

    public string Qrcode { get; set; }

    public string VIN { get; set; } = default!;

    public IEnumerable<PaintThicknessPartCosmosModel> Parts { get; set; }

    public string ItemType => "PaintThickness";
}
