using System.ComponentModel;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Enums
{
    public enum ProcessFlags
    {
        [Description("First Submission From Dealer")]
        FirstSubmissionFromDealer = 0,

        [Description("Resubmission from dealer for that once returned")]
        ResubmissionFromDealerForThatOnceReturned = 1,

        [Description("Recycled internally in Dealer")]
        RecycledInternallyInDealer = 2,

        [Description("Resubmission to vender for that once returned or rejected by vendor")]
        ResubmissionToVendorForThatOnceReturnedOrRejectedByVendor = 3,
    }
}
