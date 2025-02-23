using Moq;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.DealerData.CosmosModels;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;
using ShiftSoftware.ADP.Models.Stock.CosmosModels;

namespace Lookup.Services.Tests;

public class VehicleLookupServiceTests
{
    Mock<IIdentityCosmosService> _identityCosmosServiceMock = new();

    [Fact(DisplayName = "Authorized: the VIN should exists in VSDatat")]
    public async Task Authorized_ExistsInVSData()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCSV.VIN), vin);
        vsData.Add(nameof(VSDataCSV.ItemType), "VS");

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.IsAuthorized);
    }

    [Fact(DisplayName = "Authorized: the VIN should exists in SSC affected VINs")]
    public async Task Authorized_ExistsInSSCAffectedVINs()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCSV.VIN), vin);
        vsData.Add(nameof(VSDataCSV.ItemType), "SSCAffectedVin");

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.IsAuthorized);
    }

    [Fact(DisplayName = "Authorized: the VIN should exists in official VINs")]
    public async Task Authorized_ExistsInOfficialVINs()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCSV.VIN), vin);
        vsData.Add(nameof(VSDataCSV.ItemType), "OfficialVIN");

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.IsAuthorized);
    }

    [Fact(DisplayName = "Unauthorized: the VIN is not exists in VSData, official VINs and SSC affected VINs")]
    public async Task UnAuthorized_VINDoesNotExistsInVSDataAndOfficialVinsAndSSCAffecttedVINs()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.IsAuthorized);
    }

    [Fact(DisplayName = "Next service date:  Gets form last CPU")]
    public async Task NextServiceDate_GetItFromLastCPU()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        var serviceDate = DateTime.Now.Date.AddDays(30);

        JObject cpu1 = new JObject();
        cpu1.Add(nameof(CPUCSV.VIN), vin);
        cpu1.Add(nameof(CPUCSV.ItemType), "CPU");
        cpu1.Add(nameof(CPUCSV.InvoiceDate), DateTime.Now.AddDays(-1));
        cpu1.Add(nameof(CPUCSV.next_service), serviceDate);

        JObject cpu2 = new JObject();
        cpu2.Add(nameof(CPUCSV.VIN), vin);
        cpu2.Add(nameof(CPUCSV.ItemType), "CPU");
        cpu2.Add(nameof(CPUCSV.InvoiceDate), DateTime.Now.AddDays(-50));
        cpu2.Add(nameof(CPUCSV.next_service), DateTime.Now.AddDays(3));

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { cpu1, cpu2 }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.Equal(serviceDate, result.NextServiceDate);
    }

    [Fact(DisplayName = "SSC: If there is no ssc recalls then set ssc null")]
    public async Task SSC_ReturnNull_NoSSCRecalls()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.SSC?.Count() > 0);
    }

    [Fact(DisplayName = "SSC: return SSC recalls")]
    public async Task SSC_ReturnSSCRecalls()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var partNumber1 = "partNumber1";
        var part1Description = "part1Description";
        var partNumber2 = "partNumber2";
        var part2Description = "part2Description";
        var regionCode = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.PartNumber1), partNumber1);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.PartNumber2), partNumber2);


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc }));

        var p1 = new TIQStockCosmosModel
        {
            PartNumber = partNumber1,
            RegionIntegrationID = regionCode,
            PartDescription = part1Description,
            Qty = 5
        };

        var p2 = new TIQStockCosmosModel
        {
            PartNumber = partNumber2,
            RegionIntegrationID = "2",
            PartDescription = part2Description,
            Qty = 5
        };

        var p3 = new TIQStockCosmosModel
        {
            PartNumber = partNumber2,
            RegionIntegrationID = regionCode,
            PartDescription = part2Description,
            Qty = -5
        };

        mock.Setup(x => x.GetStockItemsAsync(new List<string> { partNumber1, partNumber2 }))
            .ReturnsAsync(new List<TIQStockCosmosModel> { p1, p2, p3 });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, regionCode, null);

        Assert.True(result.SSC?.Count() > 0);

        Assert.True(result.SSC?.Any(x => x.Labors.Any(y => y.LaborCode == opCode) && x.SSCCode == sscCode &&
            x.Parts.Any(p => p.PartNumber == partNumber1) && x.Parts.Any(x => x.PartNumber == partNumber2)));

        var part1 = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode))?.Parts.FirstOrDefault(x => x.PartNumber == partNumber1);
        Assert.True(part1?.IsAvailable);
        Assert.Equal(part1Description, part1?.PartDescription);

        var part2 = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode))?.Parts.FirstOrDefault(x => x.PartNumber == partNumber2);
        Assert.False(part2?.IsAvailable);
        Assert.Equal(part2Description, part2?.PartDescription);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.False(sscRecall?.Repaired);
        Assert.Null(sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "SSC: Case 1 - Repaired by labor")]
    public async Task SSC_Case1_RepairedByLabor()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var repairDate = DateTime.Now.Date.AddDays(-7);

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);

        JObject labor1 = new JObject();
        labor1.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor1.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor1.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor1.Add(nameof(SOLaborCosmosModel.InvoiceState), "X");
        labor1.Add(nameof(SOLaborCosmosModel.LoadStatus), "a");
        labor1.Add(nameof(SOLaborCosmosModel.DateEdited), repairDate);

        JObject labor2 = new JObject();
        labor2.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor2.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor2.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor2.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor2.Add(nameof(SOLaborCosmosModel.LoadStatus), "d");
        labor2.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now);

        JObject labor3 = new JObject();
        labor3.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor3.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor3.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor3.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor3.Add(nameof(SOLaborCosmosModel.LoadStatus), "C");
        labor3.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now.AddDays(-10));


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc, labor1, labor2, labor3 }));

        mock.Setup(x => x.GetStockItemsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<TIQStockCosmosModel> { });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "1", null);

        Assert.True(result.SSC?.Count() > 0);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.True(sscRecall?.Repaired);
        Assert.Equal(repairDate, sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "SSC: Case 2 - Repaired by labor")]
    public async Task SSC_Case2_RepairedByLabor()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var repairDate = DateTime.Now.Date.AddDays(-7);

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);

        JObject labor1 = new JObject();
        labor1.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor1.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor1.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor1.Add(nameof(SOLaborCosmosModel.InvoiceState), "c");
        labor1.Add(nameof(SOLaborCosmosModel.LoadStatus), "a");
        labor1.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now);

        JObject labor2 = new JObject();
        labor2.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor2.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor2.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor2.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor2.Add(nameof(SOLaborCosmosModel.LoadStatus), "d");
        labor2.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now);

        JObject labor3 = new JObject();
        labor3.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor3.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor3.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor3.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor3.Add(nameof(SOLaborCosmosModel.LoadStatus), "C");
        labor3.Add(nameof(SOLaborCosmosModel.DateEdited), repairDate);


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc, labor1, labor2, labor3 }));

        mock.Setup(x => x.GetStockItemsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<TIQStockCosmosModel> { });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "1", null);

        Assert.True(result.SSC?.Count() > 0);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.True(sscRecall?.Repaired);
        Assert.Equal(repairDate, sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "SSC: Not repaired if labor status is not at final state")]
    public async Task SSC_NotRepaired_LaborStatusNotCorrect()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var repairDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);

        JObject labor1 = new JObject();
        labor1.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor1.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor1.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor1.Add(nameof(SOLaborCosmosModel.InvoiceState), "c");
        labor1.Add(nameof(SOLaborCosmosModel.LoadStatus), "a");
        labor1.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now);

        JObject labor2 = new JObject();
        labor2.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor2.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor2.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor2.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor2.Add(nameof(SOLaborCosmosModel.LoadStatus), "d");
        labor2.Add(nameof(SOLaborCosmosModel.DateEdited), DateTime.Now);

        JObject labor3 = new JObject();
        labor3.Add(nameof(SOLaborCosmosModel.VIN), vin);
        labor3.Add(nameof(SOLaborCosmosModel.ItemType), "Labor");
        labor3.Add(nameof(SOLaborCosmosModel.RTSCode), opCode);
        labor3.Add(nameof(SOLaborCosmosModel.InvoiceState), "d");
        labor3.Add(nameof(SOLaborCosmosModel.LoadStatus), "b");
        labor3.Add(nameof(SOLaborCosmosModel.DateEdited), repairDate.ToDateTime(new TimeOnly(0)));


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc, labor1, labor2, labor3 }));

        mock.Setup(x => x.GetStockItemsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<TIQStockCosmosModel> { });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "1", null);

        Assert.True(result.SSC?.Count() > 0);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.False(sscRecall?.Repaired);
        Assert.Null(sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "SSC: Case 1 - Repaired by warranty claim")]
    public async Task SSC_Case1_RepairedByWarrantyClaim()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var repairDate = DateTime.Now.Date.AddDays(-7);

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);

        var warrantyClaim1 = new JObject();
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.VIN), vin);
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.ItemType), "ToyotaWarrantyClaim");
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.DistComment1), $"This is campaing code: {sscCode}");
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.Twcstatus), 5);
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.RepairDate), repairDate);

        var warrantyClaim2 = new JObject();
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.VIN), vin);
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.ItemType), "ToyotaWarrantyClaim");
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.LaborOperationNoMain), opCode);
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.RepairDate), DateTime.Now.AddDays(-10));


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc, warrantyClaim1, warrantyClaim2 }));

        mock.Setup(x => x.GetStockItemsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<TIQStockCosmosModel> { });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "1", null);

        Assert.True(result.SSC?.Count() > 0);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.True(sscRecall?.Repaired);
        Assert.Equal(repairDate, sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "SSC: Case 2 - Repaired by warranty claim")]
    public async Task SSC_Case2_RepairedByWarrantyClaim()
    {
        var vin = "1";
        var opCode = "opCode1";
        var sscCode = "sscCode1";
        var repairDate = DateTime.Now.Date.AddDays(-7);

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject ssc = new JObject();
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.VIN), vin);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.ItemType), "SSCAffectedVin");
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.OpCode), opCode);
        ssc.Add(nameof(TiqSSCAffectedVinCosmosModel.CampaignCode), sscCode);

        var warrantyClaim1 = new JObject();
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.VIN), vin);
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.ItemType), "ToyotaWarrantyClaim");
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.DistComment1), $"This is campaing code: {sscCode}");
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.Twcstatus), 1);
        warrantyClaim1.Add(nameof(ToyotaWarrantyClaimCosmosModel.RepairDate), DateTime.Now.AddDays(-10));

        var warrantyClaim2 = new JObject();
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.VIN), vin);
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.ItemType), "ToyotaWarrantyClaim");
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.LaborOperationNoMain), opCode);
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.Twcstatus), 6);
        warrantyClaim2.Add(nameof(ToyotaWarrantyClaimCosmosModel.RepairDate), repairDate);

        var warrantyClaim3 = new JObject();
        warrantyClaim3.Add(nameof(ToyotaWarrantyClaimCosmosModel.VIN), vin);
        warrantyClaim3.Add(nameof(ToyotaWarrantyClaimCosmosModel.ItemType), "ToyotaWarrantyClaim");
        warrantyClaim3.Add(nameof(ToyotaWarrantyClaimCosmosModel.LaborOperationNoMain), opCode);
        warrantyClaim3.Add(nameof(ToyotaWarrantyClaimCosmosModel.Twcstatus), 3);
        warrantyClaim3.Add(nameof(ToyotaWarrantyClaimCosmosModel.RepairDate), repairDate.AddDays(5));


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { ssc, warrantyClaim1, warrantyClaim2, warrantyClaim3 }));

        mock.Setup(x => x.GetStockItemsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new List<TIQStockCosmosModel> { });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "1", null);

        Assert.True(result.SSC?.Count() > 0);

        var sscRecall = result.SSC?.FirstOrDefault(x => x.Labors.Any(y => y.LaborCode == opCode));
        Assert.True(sscRecall?.Repaired);
        Assert.Equal(repairDate, sscRecall?.RepairDate);
    }

    [Fact(DisplayName = "Warranty: Case 1 - Not sold, still not activated")]
    public async Task Warrany_Case_1_NotSold_StillNotActivated()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCSV.VIN), vin);
        vsData.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData.Add(nameof(VSDataCSV.InvoiceDate), null);

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.Warranty.HasActiveWarranty);
        Assert.Null(result.Warranty.WarrantyStartDate);
        Assert.Null(result.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Case 2 - Not sold, but sold between dealers, still not activated")]
    public async Task Warrany_Case_2_NotSold_StillNotActivated()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCSV.VIN), vin);
        vsData1.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData1.Add(nameof(VSDataCSV.InvoiceDate), DateTime.Now);

        JObject vsData2 = new JObject();
        vsData2.Add(nameof(VSDataCSV.VIN), vin);
        vsData2.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData2.Add(nameof(VSDataCSV.InvoiceDate), null);

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1, vsData2 }));

        mock.Setup(x => x.GetBrokerAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { "55" },
                AccountStartDate = DateTime.Now.AddDays(-10),
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.Warranty.HasActiveWarranty);
        Assert.Null(result.Warranty.WarrantyStartDate);
        Assert.Null(result.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Case 3 - Not sold, but sold to broker, still not activated")]
    public async Task Warrany_Case_3_NotSold_StillNotActivated()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData1.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData1.Add(nameof(VSDataCosmosModel.InvoiceDate), DateTime.Now.AddDays(-15));

        JObject vsData2 = new JObject();
        vsData2.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData2.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData2.Add(nameof(VSDataCosmosModel.CustomerAccount), brokerAccountNumber);
        vsData2.Add(nameof(VSDataCosmosModel.DealerIntegrationID), dealerId);
        vsData2.Add(nameof(VSDataCosmosModel.InvoiceDate), DateTime.Now.AddDays(-10));

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1, vsData2 }));

        mock.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.Warranty.HasActiveWarranty);
        Assert.Null(result.Warranty.WarrantyStartDate);
        Assert.Null(result.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Case 4 - Not sold, but sold to broker before account start date, still not activated")]
    public async Task Warrany_Case_4_NotSold_StillNotActivated()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";
        long brokerId = 1;

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData1.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData1.Add(nameof(VSDataCosmosModel.InvoiceDate), DateTime.Now.AddDays(-15));

        JObject vsData2 = new JObject();
        vsData2.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData2.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData2.Add(nameof(VSDataCosmosModel.CustomerAccount), brokerAccountNumber);
        vsData2.Add(nameof(VSDataCosmosModel.DealerIntegrationID), dealerId);
        vsData2.Add(nameof(VSDataCosmosModel.InvoiceDate), DateTime.Now.AddDays(-10));

        JObject brokerInitialVehicle = new JObject();
        brokerInitialVehicle.Add(nameof(BrokerInitialVehicleCosmosModel.VIN), vin);
        brokerInitialVehicle.Add(nameof(BrokerInitialVehicleCosmosModel.ItemType), "BrokerInitialVehicle");
        brokerInitialVehicle.Add(nameof(BrokerInitialVehicleCosmosModel.BrokerId), brokerId);


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1, vsData2, brokerInitialVehicle }));

        mock.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-3),
                Id = brokerId
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.Warranty.HasActiveWarranty);
        Assert.Null(result.Warranty.WarrantyStartDate);
        Assert.Null(result.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Directly sold to customer, Has active warranty")]
    public async Task Warrany_DirectlySoldToCustomer_HasActiveWarranty()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";
        var invoiceDate = DateTime.Now.Date.AddDays(-10);


        // Toyota brand should have 3 years warranty
        var mock1 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData1.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData1.Add(nameof(VSDataCosmosModel.Franchise), "T");
        vsData1.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Toyota));
        vsData1.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);

        mock1.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1 }));

        mock1.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
            });

        var service1 = new VehicleLookupService(mock1.Object, _identityCosmosServiceMock.Object);

        var result1 = await service1.LookupAsync(vin, "", null);

        Assert.True(result1.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result1.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(3), result1.Warranty.WarrantyEndDate);

        // Hino brand should have 3 years warranty
        var mock2 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData2 = new JObject();
        vsData2.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData2.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData2.Add(nameof(VSDataCosmosModel.Franchise), "H");
        vsData2.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Hino));
        vsData2.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);

        mock2.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData2 }));

        mock2.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
            });

        var service2 = new VehicleLookupService(mock2.Object, _identityCosmosServiceMock.Object);

        var result2 = await service2.LookupAsync(vin, "", null);

        Assert.True(result2.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result2.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(3), result2.Warranty.WarrantyEndDate);

        // Lexus brand should have 4 years warranty
        var mock3 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData3 = new JObject();
        vsData3.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData3.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData3.Add(nameof(VSDataCosmosModel.Franchise), "L");
        vsData3.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Lexus));
        vsData3.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);

        
        mock3.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData3 }));

        mock3.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
            });

        var service3 = new VehicleLookupService(mock3.Object, _identityCosmosServiceMock.Object);

        var result3 = await service3.LookupAsync(vin, "", null);

        Assert.True(result3.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result3.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(4), result3.Warranty.WarrantyEndDate);

        // Lexus brand should have 4 years warranty, but the warranty is expired
        var invoiceDate2 = DateTime.Now.Date.AddYears(-10);
        var mock4 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData4 = new JObject();
        vsData4.Add(nameof(VSDataCSV.VIN), vin);
        vsData4.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData4.Add(nameof(VSDataCSV.Franchise), "L");
        vsData4.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Lexus));
        vsData4.Add(nameof(VSDataCSV.InvoiceDate), invoiceDate2);

        mock4.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData4 }));

        mock4.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-40),
            });

        var service4 = new VehicleLookupService(mock4.Object, _identityCosmosServiceMock.Object);

        var result4 = await service4.LookupAsync(vin, "", null);

        Assert.False(result4.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate2, result4.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate2.AddYears(4), result4.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Sold to custoemr by broker, Has active warranty")]
    public async Task Warrany_SoldToCustomerByBroker_HasActiveWarranty()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";
        var invoiceDate = DateTime.Now.Date.AddDays(-10);

        var mock1 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData1.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData1.Add(nameof(VSDataCosmosModel.Franchise), "L");
        vsData1.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Lexus));
        vsData1.Add(nameof(VSDataCosmosModel.VSData_DealerId), dealerId);
        vsData1.Add(nameof(VSDataCosmosModel.CustomerAccount), brokerAccountNumber);
        vsData1.Add(nameof(VSDataCosmosModel.InvoiceDate), DateTime.Now.AddDays(-30));

        JObject brokerInvoice = new JObject();
        brokerInvoice.Add(nameof(BrokerInvoiceCosmosModel.VIN), vin);
        brokerInvoice.Add(nameof(BrokerInvoiceCosmosModel.ItemType), "BrokerInvoice");
        brokerInvoice.Add(nameof(BrokerInvoiceCosmosModel.InvoiceDate), invoiceDate);

        mock1.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1, brokerInvoice}));

        mock1.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
                TerminationDate = DateTime.Now.AddDays(-1)
            });

        var service1 = new VehicleLookupService(mock1.Object, _identityCosmosServiceMock.Object);

        var result1 = await service1.LookupAsync(vin, "", null);

        Assert.True(result1.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result1.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(4), result1.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Sold to borker but broker is terminated, Has active warranty of vs-data")]
    public async Task Warrany_SoldToBrokerButBrokerTerminated_HasActiveWarrantyOfVSData()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";
        var invoiceDate = DateTime.Now.Date.AddDays(-10);

        var mock1 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCSV.VIN), vin);
        vsData1.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData1.Add(nameof(VSDataCSV.Franchise), "L");
        vsData1.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Lexus));
        vsData1.Add(nameof(VSDataCSV.VSData_DealerId), dealerId);
        vsData1.Add(nameof(VSDataCSV.CustomerAccount), brokerAccountNumber);
        vsData1.Add(nameof(VSDataCSV.InvoiceDate), invoiceDate);

        mock1.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1 }));

        mock1.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-30),
                TerminationDate = DateTime.Now.AddDays(-1)
            });

        var service1 = new VehicleLookupService(mock1.Object, _identityCosmosServiceMock.Object);

        var result1 = await service1.LookupAsync(vin, "", null);

        Assert.True(result1.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result1.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(4), result1.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Warranty: Sold to borker before account start date, Has active warranty of vs-data")]
    public async Task Warrany_SoldToBrokerBeforeAccountStartDate_HasActiveWarrantyOfVSData()
    {
        string vin = "1";
        string brokerAccountNumber = "Broker1";
        string dealerId = "1";
        var invoiceDate = DateTime.Now.Date.AddDays(-10);

        var mock1 = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData1 = new JObject();
        vsData1.Add(nameof(VSDataCSV.VIN), vin);
        vsData1.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData1.Add(nameof(VSDataCSV.Franchise), "L");
        vsData1.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(Franchises.Lexus));
        vsData1.Add(nameof(VSDataCSV.VSData_DealerId), dealerId);
        vsData1.Add(nameof(VSDataCSV.CustomerAccount), brokerAccountNumber);
        vsData1.Add(nameof(VSDataCSV.InvoiceDate), invoiceDate);
        
        mock1.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData1 }));

        mock1.Setup(x => x.GetBrokerAsync(brokerAccountNumber, dealerId))
            .ReturnsAsync(new BrokerCosmosModel
            {
                AccountNumbers = new List<string> { brokerAccountNumber },
                DealerIntegrationID = dealerId,
                AccountStartDate = DateTime.Now.AddDays(-1),
            });

        var service1 = new VehicleLookupService(mock1.Object, _identityCosmosServiceMock.Object);

        var result1 = await service1.LookupAsync(vin, "", null);

        Assert.True(result1.Warranty.HasActiveWarranty);
        Assert.Equal(invoiceDate, result1.Warranty.WarrantyStartDate);
        Assert.Equal(invoiceDate.AddYears(4), result1.Warranty.WarrantyEndDate);
    }

    [Fact(DisplayName = "Service Items: InvoiceDate is null, there is no free services")]
    public async Task ServiceItems_Case_1_InvoiceDateNull_NoFreeServices()
    {
        string vin = "1";

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCSV.VIN), vin);
        vsData.Add(nameof(VSDataCSV.ItemType), "VS");
        vsData.Add(nameof(VSDataCSV.InvoiceDate), null);

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> { vsData }));

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.False(result.ServiceItems?.Count() > 0);
    }

    [Fact(DisplayName = "Service Items: There is items with pending, processed, expired and canceled")]
    public async Task ServiceItems_HaveAllKindOfServiceItemsAndStatuses()
    {
        var vin = "1";
        var katashiki = "katashiki1";
        var variant = "variant1";
        var invoiceDate = DateTime.Now.Date.AddMonths(-2);
        var brand = Franchises.Toyota;
        var redeemDate1 = DateTime.Now.Date.AddDays(-10);
        var redeemDate2 = DateTime.Now.Date.AddDays(-5);

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);
        vsData.Add(nameof(VSDataCosmosModel.Katashiki), katashiki);
        vsData.Add(nameof(VSDataCosmosModel.VariantCode), variant);
        vsData.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(brand));

        JObject transactionLine1 = new JObject();
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 2);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 4);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate1);

        JObject transactionLine2 = new JObject();
        transactionLine2.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine2.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine2.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 12);
        transactionLine2.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 7);
        transactionLine2.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate2);

        JObject transactionLine3 = new JObject();
        transactionLine3.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine3.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine3.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 101);
        transactionLine3.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 4);
        transactionLine3.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate1);

        JObject transactionLine4 = new JObject();
        transactionLine4.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine4.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine4.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 102);
        transactionLine4.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 4);
        transactionLine4.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate1);

        JObject tlpInvoiceItem1 = new JObject();
        tlpInvoiceItem1.Add(nameof(TLPPackageInvoiceCosmosModel.VIN), vin);
        tlpInvoiceItem1.Add(nameof(TLPPackageInvoiceCosmosModel.ItemType), "TLPPackageInvoice");
        tlpInvoiceItem1.Add(nameof(TLPPackageInvoiceCosmosModel.StartDate), DateTime.Now.AddMonths(-3));
        tlpInvoiceItem1.Add(nameof(TLPPackageInvoiceCosmosModel.Items),
            JArray.FromObject(new List<TLPPackageInvoiceTLPItemCosmosModel>
        {
            new TLPPackageInvoiceTLPItemCosmosModel
            {
                ExpireDate = DateTime.Now.AddMonths(-1),
                ToyotaLoyaltyProgramRedeemableItemId = 11,
                MenuCode="3"
            },
            new TLPPackageInvoiceTLPItemCosmosModel
            {
                ExpireDate = DateTime.Now.AddMonths(1),
                ToyotaLoyaltyProgramRedeemableItemId = 12,
            }
        }));

        JObject tlpInvoiceItem2 = new JObject();
        tlpInvoiceItem2.Add(nameof(TLPPackageInvoiceCosmosModel.VIN), vin);
        tlpInvoiceItem2.Add(nameof(TLPPackageInvoiceCosmosModel.ItemType), "TLPPackageInvoice");
        tlpInvoiceItem2.Add(nameof(TLPPackageInvoiceCosmosModel.StartDate), DateTime.Now.AddMonths(-3));
        tlpInvoiceItem2.Add(nameof(TLPPackageInvoiceCosmosModel.Items),
            JArray.FromObject(new List<TLPPackageInvoiceTLPItemCosmosModel>
        {
            new TLPPackageInvoiceTLPItemCosmosModel
            {
                ExpireDate = DateTime.Now.AddMonths(2),
                ToyotaLoyaltyProgramRedeemableItemId = 13,
            },
            new TLPPackageInvoiceTLPItemCosmosModel
            {
                ID=401,
                ExpireDate = DateTime.Now.AddMonths(2),
                ToyotaLoyaltyProgramRedeemableItemId = 301,
            }
        }));

        JObject canceledServiceItem1 = new JObject();
        canceledServiceItem1.Add(nameof(TLPCancelledServiceItemCosmosModel.Type), JToken.FromObject(VehcileServiceItemTypes.Free));
        canceledServiceItem1.Add(nameof(TLPCancelledServiceItemCosmosModel.ToyotaLoyaltyProgramRedeemableItemID), 300);
        canceledServiceItem1.Add(nameof(TLPCancelledServiceItemCosmosModel.VIN), vin);
        canceledServiceItem1.Add(nameof(TLPCancelledServiceItemCosmosModel.ItemType), "TLPCancelledServiceItem");

        JObject canceledServiceItem2 = new JObject();
        canceledServiceItem2.Add(nameof(TLPCancelledServiceItemCosmosModel.Type), JToken.FromObject(VehcileServiceItemTypes.Paid));
        canceledServiceItem2.Add(nameof(TLPCancelledServiceItemCosmosModel.ToyotaLoyaltyProgramRedeemableItemID), 301);
        canceledServiceItem2.Add(nameof(TLPCancelledServiceItemCosmosModel.TLPPackageInvoiceTLPItemID), 401);
        canceledServiceItem2.Add(nameof(TLPCancelledServiceItemCosmosModel.VIN), vin);
        canceledServiceItem2.Add(nameof(TLPCancelledServiceItemCosmosModel.ItemType), "TLPCancelledServiceItem");


        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> {
                vsData,
                transactionLine1, transactionLine2, transactionLine3, transactionLine4,
                tlpInvoiceItem1, tlpInvoiceItem2,
                canceledServiceItem1, canceledServiceItem2
            }));

        var redeemalbeItem1 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 1,
            ActiveFor = 2,
            ActiveForInterval = "Months",
            MaximumMileage = 1000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.UtcNow.AddMonths(10),
            ModelCosts = new List<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel>
            {
                new ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel { Katashiki=katashiki.Substring(0,3) }
            }
        };

        var redeemalbeItem2 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 2,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 5000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.UtcNow.AddMonths(100),
            ModelCosts = new List<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel>
            {
                new ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel { Katashiki=katashiki }
            }
        };

        var redeemalbeItem3 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 3,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 100000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.UtcNow.AddMonths(10),
            ModelCosts = new List<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel>
            {
                new ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel { Variant=variant.Substring(0,3) }
            }
        };

        var redeemalbeItem4 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 101,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 20000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.Now.AddMonths(-10),
            ModelCosts = new List<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel>
            {
                new ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel { Variant=variant, MenuCode="1" }
            }
        };

        var redeemalbeItem5 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 25000,
            ToyotaLoyaltyProgramRedeemableItemId = 102,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.Now.AddMonths(10),
            Deleted = true,
            MenuCode = "2",
            ModelCosts = new List<ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel>()
        };

        var redeemalbeItem6 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 300,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 35000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-30),
            ExpireDate = DateTime.UtcNow.AddMonths(100),
        };

        mock.Setup(x => x.GetRedeemableItemsAsync(brand))
            .ReturnsAsync(new List<ToyotaLoyaltyProgramRedeemableItemCosmosModel>
            {
                redeemalbeItem1,
                redeemalbeItem2,
                redeemalbeItem3,
                redeemalbeItem4,
                redeemalbeItem5,
                redeemalbeItem6
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.ServiceItems?.Count() > 0);

        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Expired));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Pending));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed && x.ToyotaLoyaltyProgramRedeemableItemID == 2));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed && x.ToyotaLoyaltyProgramRedeemableItemID == 101));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed && x.ToyotaLoyaltyProgramRedeemableItemID == 102));
        Assert.Equal(redeemDate1, result.ServiceItems?.FirstOrDefault(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed)?.RedeemDate);

        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Paid
            && x.StatusEnum == VehcileServiceItemStatuses.Expired));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Paid
            && x.StatusEnum == VehcileServiceItemStatuses.Pending));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Paid
            && x.StatusEnum == VehcileServiceItemStatuses.Processed));
        Assert.Equal(redeemDate2, result.ServiceItems?.FirstOrDefault(x => x.TypeEnum == VehcileServiceItemTypes.Paid
            && x.StatusEnum == VehcileServiceItemStatuses.Processed)?.RedeemDate);

        Assert.Contains("1", result.ServiceItems.Select(x => x.MenuCode));
        Assert.Contains("2", result.ServiceItems.Select(x => x.MenuCode));
        Assert.Contains("3", result.ServiceItems.Select(x => x.MenuCode));

        Assert.Contains(VehcileServiceItemStatuses.Cancelled,
            result.ServiceItems.Where(x => x.TypeEnum == VehcileServiceItemTypes.Free).Select(x => x.StatusEnum));
        Assert.Contains(VehcileServiceItemStatuses.Cancelled,
            result.ServiceItems.Where(x => x.TypeEnum == VehcileServiceItemTypes.Paid).Select(x => x.StatusEnum));
    }

    [Fact(DisplayName = "Service Items: Dynamically cancel free service items")]
    public async Task ServiceItems_DynamicCancelFreeServices()
    {
        var vin = "1";
        var katashiki = "katashiki1";
        var variant = "variant1";
        var invoiceDate = DateTime.Now.Date.AddDays(-40);
        var brand = Franchises.Toyota;
        var redeemDate = DateTime.Now.Date;

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);
        vsData.Add(nameof(VSDataCosmosModel.Katashiki), katashiki);
        vsData.Add(nameof(VSDataCosmosModel.VariantCode), variant);
        vsData.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(brand));

        JObject transactionLine1 = new JObject();
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 4);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 4);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate);

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> 
            {
                vsData,
                transactionLine1,
            }));

        var redeemalbeItem1 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 1,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 1000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem2 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 2,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 5000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem3 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 3,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 10000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem4 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 4,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 15000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        mock.Setup(x => x.GetRedeemableItemsAsync(brand))
            .ReturnsAsync(new List<ToyotaLoyaltyProgramRedeemableItemCosmosModel>
            {
                redeemalbeItem1,
                redeemalbeItem2,
                redeemalbeItem3,
                redeemalbeItem4,
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.ServiceItems?.Count() > 0);
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Expired));
        Assert.Equal(2, result.ServiceItems?.Count(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Cancelled));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed));
    }

    [Fact(DisplayName = "Service Items: Non sequential item does not cancel dynamically and expires with package expiration")]
    public async Task ServiceItems_NonSquentialItemDynamicCancleAndExpire()
    {
        var vin = "1";
        var katashiki = "katashiki1";
        var variant = "variant1";
        var invoiceDate = DateTime.Now.Date.AddDays(-40);
        var brand = Franchises.Toyota;
        var redeemDate = DateTime.Now.Date;

        var mock = new Mock<IVehicleLoockupCosmosService>();

        JObject vsData = new JObject();
        vsData.Add(nameof(VSDataCosmosModel.VIN), vin);
        vsData.Add(nameof(VSDataCosmosModel.ItemType), "VS");
        vsData.Add(nameof(VSDataCosmosModel.InvoiceDate), invoiceDate);
        vsData.Add(nameof(VSDataCosmosModel.Katashiki), katashiki);
        vsData.Add(nameof(VSDataCosmosModel.VariantCode), variant);
        vsData.Add(nameof(VSDataCosmosModel.Brand), JToken.FromObject(brand));

        JObject transactionLine1 = new JObject();
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.VIN), vin);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.ItemType), "ToyotaLoyaltyProgramTransactionLine");
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemableItemIdRef), 4);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.RedeemType), 4);
        transactionLine1.Add(nameof(ToyotaLoyaltyProgramTransactionLineCosmosModel.EntryDate), redeemDate);

        mock.Setup(x => x.GetAggregatedDealerData(vin))
            .ReturnsAsync(VehicleLoockupCosmosService.ConvertDynamicListItemsToDealerData(new List<dynamic> {
                vsData,
                transactionLine1,
            }));

        var redeemalbeItem1 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 1,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 1000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem2 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 2,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = null,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem3 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 3,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = null,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        var redeemalbeItem4 = new ToyotaLoyaltyProgramRedeemableItemCosmosModel
        {
            ToyotaLoyaltyProgramRedeemableItemId = 4,
            ActiveFor = 1,
            ActiveForInterval = "Months",
            MaximumMileage = 15000,
            Brands = new List<Franchises> { brand },
            PublishDate = DateTime.UtcNow.AddMonths(-300),
            ExpireDate = DateTime.UtcNow.AddMonths(100)
        };

        mock.Setup(x => x.GetRedeemableItemsAsync(brand))
            .ReturnsAsync(new List<ToyotaLoyaltyProgramRedeemableItemCosmosModel>
            {
                redeemalbeItem1,
                redeemalbeItem2,
                redeemalbeItem3,
                redeemalbeItem4,
            });

        var service = new VehicleLookupService(mock.Object, _identityCosmosServiceMock.Object);

        var result = await service.LookupAsync(vin, "", null);

        Assert.True(result.ServiceItems?.Count() > 0);
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Expired));
        Assert.Equal(0, result.ServiceItems?.Count(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Cancelled));
        Assert.Equal(2, result.ServiceItems?.Count(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Pending && x.ActivatedAt == invoiceDate
            && x.ExpiresAt == invoiceDate.AddMonths(1).AddMonths(1)));
        Assert.True(result.ServiceItems?.Any(x => x.TypeEnum == VehcileServiceItemTypes.Free
            && x.StatusEnum == VehcileServiceItemStatuses.Processed));
    }
}
