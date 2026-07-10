namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;

public static class WarrantyTypes
{
    public static readonly KeyValuePair<string, string> SSC = new("SSC", "SSC: Special Service Campaign");
    public static readonly KeyValuePair<string, string> VE = new("VE", "VE: Vehicle Warranty");
    public static readonly KeyValuePair<string, string> P1 = new("P1", "P1: Service Parts Warranty");
    public static readonly KeyValuePair<string, string> P2 = new("P2", "P2: Counter Sales Parts Warranty");
    public static readonly KeyValuePair<string, string> A1 = new("A1", "A1: Accessories Warranty");
    public static readonly KeyValuePair<string, string> A2 = new("A2", "A2: Counter Sales Accessories Warranty");
}
