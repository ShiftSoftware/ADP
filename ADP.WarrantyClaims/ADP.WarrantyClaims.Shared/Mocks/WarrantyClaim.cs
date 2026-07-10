using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Mocks;

public class WarrantyClaim
{
    public WarrantyClaimDTO DealerClaim = new WarrantyClaimDTO
    {
        //ClaimNumber = "1000001",
        DealerCode = "11111",
        DealerClaimNo = "11111",
        DateOfReceipt = new DateTime(2024, 12, 1),
        ProcessFlg = ProcessFlags.FirstSubmissionFromDealer,
        WarrantyType = WarrantyTypes.VE.Key,
        OperationType = OperationTypes.General,
        Franchise = Franchises.Toyota.Key,
        VIN_WMI = "RL4",
        VIN_VDS = "BU3HE",
        VIN_CD = "X",
        YearModel = 2020,
        VIN_VIS = "A9167938",
        DeliveryDate = new DateTime(2024, 11, 1),
        RepairDate = new DateTime(2024, 12, 4),
        RepairCompletionDate = new DateTime(2024, 12, 5),
        Odometer = 10_000,
        KMFlg = KMFlags.K,
        RepairOrderNo = "1294844",
        DataID = DataIDs.W.Key,
        T1 = "11",
        T2 = "22",
        T3_1 = "31",
        T3_2 = "32",
        T3_3 = "33",
        T3_4 = "34",
        T3_5 = "35",
        T3_6 = "36",
        T3_7 = "37",
        Condition = "Condition",
        Cause = "Cause",
        Remedy = "Remedy",
        DealerComments = "Dealer Comments",
        BatteryTestCode11 = "11",
        BatteryTestCode12 = "12",
        BatteryTestCode21 = "21",
        BatteryTestCode22 = "22",
        TSB = "TSB Number",
        SubletDescription = "Sublet Description",
        WarrantyClaimLaborLines = new List<WarrantyClaimLaborLineDTO>
        {
            new WarrantyClaimLaborLineDTO
            {
                ID = 1,
                PayCode = "1",
                MainOperation = true,
                OperationNumber = "1",
                Hour = 1,
                DistributorHour = 10
            },
            new WarrantyClaimLaborLineDTO
            {
                ID = 2,
                PayCode = "1",
                MainOperation = false,
                OperationNumber = "2",
                Hour = 2,
                DistributorHour = 20
            }
        },
        WarrantyClaimSubletLines = new List<WarrantyClaimSubletLineDTO>
        {
            new WarrantyClaimSubletLineDTO
            {
                ID = 1,
                PayCode = "1",
                Amount = 100,
                Description = "Description",
                InvoiceNo = "121212",
                SubletType = SubletTypes.PT.Key,
            },
            new WarrantyClaimSubletLineDTO
            {
                ID = 2,
                PayCode = "1",
                Amount = 50,
                Description = "Description",
                InvoiceNo = "141911",
                SubletType = SubletTypes.PT.Key,
            }
        },
        WarrantyClaimPartLines = new List<WarrantyClaimPartLineDTO>
        {
            new WarrantyClaimPartLineDTO
            {
                ID = 1,
                PayCode = "1",
                OFP = true,
                LocalF = "L",
                PartNumber = "T193949323",
                PartDescription = "Part Description",
                Qty = 1,
                Price = 10,
                DistributorPrice = 20
            },
            new WarrantyClaimPartLineDTO
            {
                ID = 2,
                PayCode = "1",
                OFP = false,
                LocalF = "L",
                PartNumber = "T193949323",
                PartDescription = "Part Description",
                Qty = 2,
                Price = 20,
                DistributorPrice = 30
            }
        },
    };
}
