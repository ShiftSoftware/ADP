namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuLineDTO
{
    public string Code { get; set; }
    public long? BrandID { get; set; }
    public string BasicModelCode { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public string LabourCode { get; set; }
    public decimal LabourRate { get; set; }
    public decimal AllowedTime { get; set; }
    public decimal Consumable { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsStandalone { get; set; }
    public IEnumerable<MenuLinePartDTO> Parts { get; set; }
    public decimal PartsCost => Parts?.Sum(x => x.TotalCost) ?? 0;
    public decimal PartsPrice => Parts?.Sum(x => x.TotalPrice) ?? 0;
    public decimal PartsProfit => PartsPrice - PartsCost;
    public decimal PartsProfitPercentage {
        get
        {
            if(PartsCost == 0)
                return 0; 

            return Math.Round((PartsProfit / PartsCost) * 100, 2);
        }
    }
    public decimal LabourCost => 10 * AllowedTime;
    public decimal LabourPrice => LabourRate * AllowedTime;
    public decimal LabourTotalPrice => LabourPrice + Consumable;
    public decimal LabourProfit => LabourPrice - LabourCost;
    public decimal GrossProfit => PartsProfit + LabourProfit;
    public decimal GrossProfitPercentage { get
        {
            decimal revenue = PartsPrice + LabourPrice;
            if (revenue == 0)
                return 0;

            return Math.Round((GrossProfit / revenue) * 100, 2);
        }
    }
    public decimal MenuProfit => PartsProfit + LabourProfit + Consumable;
    public decimal MenuTotalPrice { get
        {
            decimal totalPrice = LabourTotalPrice + PartsPrice;

            // Calculate discount
            totalPrice = totalPrice - (DiscountPercentage.GetValueOrDefault() / 100 * totalPrice);

            return totalPrice;
        }
    }
}

public class MenuLinePartDTO
{
    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice => Price * Quantity;
    public decimal TotalCost => Cost * Quantity;
}
