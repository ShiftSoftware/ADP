using Reqnroll;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services.Evaluators;
using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lookup.Services.Features
{
    [Binding]
    public class SaleInformationStepDefinitions
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly ITestOutputHelper _testOutputHelper;

        public SaleInformationStepDefinitions(ScenarioContext scenarioContext, ITestOutputHelper testOutputHelper)
        {
            _scenarioContext = scenarioContext;
            _testOutputHelper = testOutputHelper;
        }

        [Then("The Vehicle Invoice Date is {string}")]
        public async Task ThenTheVehicleInvoiceDateIs(string invovicedSince)
        {
            this._scenarioContext.TryGetValue("vin", out string _vin);

            this._scenarioContext.TryGetValue("companyData", out CompanyDataAggregateCosmosModel _companyDataAggregate);
            
            var extractor = new SaleInfoExtractor(
                new ShiftSoftware.ADP.Lookup.Services.LookupOptions { },
                null
            );

            var saleInfo = await extractor.ExtractSaleInformationAsync(
                _companyDataAggregate.VehicleEntries.ToList(),
                "en",
                _companyDataAggregate
            );

            if (string.IsNullOrWhiteSpace(invovicedSince))
                Assert.Null(saleInfo.InvoiceDate);
            else
            {
                Assert.Equal(
                    SharedStepDefinitions.ConvertSinceYearsAgoToDate(invovicedSince), 
                    saleInfo.InvoiceDate
                );
            }
        }
    }
}
