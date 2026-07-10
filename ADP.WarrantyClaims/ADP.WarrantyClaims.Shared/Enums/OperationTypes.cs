using System.ComponentModel;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Enums
{
    public enum OperationTypes
    {
        [Description("Not Specified")]
        NotSpecified = 0,

        [Description("General")]
        General = 1,

        [Description("Paint")]
        Paint = 2,

        [Description("Noise")]
        Noise = 3,

        [Description("Rain")]
        Rain = 4,

        [Description("Campaign")]
        Campaign = 5,

        [Description("Dealer Stock Disposal")]
        DealerStockDisposal = 6,

        [Description("Distributor Stock Disposal")]
        DistributorStockDisposal = 7
    }
}
