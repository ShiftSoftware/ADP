using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;
using ShiftSoftware.ADP.Models.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lookup.Services.Features
{
    [Binding]
    internal class WarrantyStepDefenisions
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly ITestOutputHelper _testOutputHelper;
        private string _vin;


        public WarrantyStepDefenisions(ScenarioContext scenarioContext, ITestOutputHelper _testOutputHelper)
        {
            _scenarioContext = scenarioContext;
            this._testOutputHelper = _testOutputHelper;
        }


        [Then("The Vehicle has Active Warranty")]
        public async Task ThenTheVehicleHasActiveWarranty()
        {
            this._scenarioContext.TryGetValue("vin", out string _vin);

            this._scenarioContext.TryGetValue("companyData", out CompanyDataAggregateCosmosModel _companyDataAggregate);
    
            var match = _companyDataAggregate.VehicleEntries.Where(x => x.VIN == _vin).FirstOrDefault();

            var checker = new WarrantyAndFreeServiceEvaluator(
                new ShiftSoftware.ADP.Lookup.Services.LookupOptions {
                    WarrantyStartDateDefaultsToInvoiceDate = true
                }
            );

            var authorizedResult = await checker.EvaluateWarrantyStartDate(
                ShiftSoftware.ADP.Models.Enums.Brands.Toyota,
                true,
                new ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup.VehicleLookupDTO {
                    SaleInformation = new ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup.VehicleSaleInformation
                    {
                        InvoiceDate = match.InvoiceDate
                    }
                },
                _companyDataAggregate
            );

            this._testOutputHelper.WriteLine("");
            this._testOutputHelper.WriteLine("");
            this._testOutputHelper.WriteLine("");

            this._testOutputHelper.WriteLine("Status is: " + authorizedResult);
            //this._testOutputHelper.WriteLine("Data is: " +
            //    JsonSerializer.Serialize(
            //        new
            //        {
            //            InitialOfficialVINs = _companyDataAggregate.InitialOfficialVINs.Count,
            //            VehicleEntries = _companyDataAggregate.VehicleEntries.Count,
            //            SSCAffectedVINs = _companyDataAggregate.SSCAffectedVINs.Count,
            //        },
            //        new JsonSerializerOptions { WriteIndented = true }
            //));

            //authorizedResult.
            //Assert.True(authorizedResult);
        }


        [Then("The Vehicle Does not have Active Warranty")]
        public void ThenTheVehicleDoesNotHaveActiveWarranty()
        {
            //_scenarioContext.Pending();
        }
    }
}
