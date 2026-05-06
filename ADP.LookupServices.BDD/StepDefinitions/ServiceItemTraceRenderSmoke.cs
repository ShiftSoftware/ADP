using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using Xunit;

namespace LookupServices.BDD.StepDefinitions;

/// <summary>
/// Smoke test: build a realistic populated trace and call every renderer. Verifies
/// the renderer doesn't throw on real-shaped data and (in CI) writes a visual sample
/// to a temp folder for eyeballing during development. Not part of the BDD suite.
/// </summary>
public class ServiceItemTraceRenderSmoke
{
    [Fact]
    public void Renders_populated_trace_to_html()
    {
        var trace = BuildSample();
        var html = ServiceItemTraceRenderer.ToHtml(trace);
        Assert.Contains("Lookup pipeline", html);
        Assert.Contains("Where items go", html);
        Assert.Contains("sankey-beta", html);
        Assert.Contains("Activation &amp; expiry timeline", html);
        Assert.Contains("ABC Toyota Erbil", html);
        Assert.Contains("Toyota", html);
        Assert.Contains("Iraq", html);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "adp-trace-sample.html");
        System.IO.File.WriteAllText(path, html);
    }

    [Fact]
    public void Renders_populated_trace_to_mermaid()
    {
        var trace = BuildSample();
        var mer = ServiceItemTraceRenderer.ToMermaid(trace);
        Assert.StartsWith("flowchart TB", mer);
        Assert.Contains("ABC Toyota Erbil", mer);
        Assert.Contains("Different brand", mer);
    }

    [Fact]
    public void Renders_populated_trace_timeline_gantt()
    {
        var trace = BuildSample();
        var g = ServiceItemTraceRenderer.ToTimelineMermaid(trace);
        Assert.StartsWith("gantt", g);
        Assert.Contains("dateFormat YYYY-MM-DD", g);
    }

    [Fact]
    public void Renders_populated_trace_funnel_sankey()
    {
        var trace = BuildSample();
        var s = ServiceItemTraceRenderer.ToFunnelMermaid(trace);
        Assert.StartsWith("sankey-beta", s);
        Assert.Contains("Catalog,Eligible,3", s);
        Assert.Contains("Different brand", s);
    }

    static ServiceItemTrace BuildSample()
    {
        var trace = new ServiceItemTrace
        {
            Vin = "1FDKF37GXVEB34368",
            StartedUtc = DateTime.UtcNow.AddMilliseconds(-145),
            FinishedUtc = DateTime.UtcNow,
            Inputs = new ServiceItemTraceInputs
            {
                Vin = "1FDKF37GXVEB34368",
                BrandID = 2,
                CompanyID = 1,
                Katashiki = "GUN125L-DTFNYV",
                VariantCode = "GUN125-DTFNYV",
                VehicleLoaded = true,
                SaleCountryID = 42,
                FreeServiceStartDate = new DateTime(2024, 2, 10),
                AggregateCounts = new ServiceItemTraceAggregateCounts
                {
                    CosmosServiceItems = 47, PaidServiceInvoices = 2, PaidServiceInvoiceLines = 4,
                    ItemClaims = 3, VehicleInspections = 2, VehicleServiceActivations = 1,
                },
            },
            ResolvedNames = new ServiceItemTraceNameResolutions
            {
                Brands = new() { [2] = "Toyota", [3] = "Lexus" },
                Companies = new() { [1] = "ABC Toyota Erbil" },
                Countries = new() { [42] = "Iraq", [1] = "Saudi Arabia", [2] = "UAE" },
            },
        };

        trace.Eligibility = new ServiceItemTraceEligibility
        {
            InputCount = 6, AcceptedCount = 3, RejectedCount = 3,
            Decisions = new()
            {
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-005K", Name = "5,000 km Free Service", Verdict = EligibilityVerdict.Accepted, Item = new ServiceItemSnapshot { BrandIDs = new() { 2 }, MaximumMileage = 5000, ValidityMode = ClaimableItemValidityMode.RelativeToActivation, CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.WarrantyActivation } },
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-010K", Name = "10,000 km Free Service", Verdict = EligibilityVerdict.Accepted, Item = new ServiceItemSnapshot { BrandIDs = new() { 2 }, MaximumMileage = 10000, ValidityMode = ClaimableItemValidityMode.RelativeToActivation, CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.WarrantyActivation } },
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-INSP1", Name = "Annual Inspection Bonus", Verdict = EligibilityVerdict.Accepted, Item = new ServiceItemSnapshot { BrandIDs = new() { 2 }, ValidityMode = ClaimableItemValidityMode.RelativeToActivation, CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection, VehicleInspectionTypeID = 1 } },
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-LEX01", Name = "Lexus Premium Care", Verdict = EligibilityVerdict.Rejected, RejectionStage = EligibilityRejectionStage.Brand, Reason = "Vehicle BrandID=2 not in item.BrandIDs=[3].", Item = new ServiceItemSnapshot { BrandIDs = new() { 3 } } },
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-KSA01", Name = "KSA-only Promotion", Verdict = EligibilityVerdict.Rejected, RejectionStage = EligibilityRejectionStage.Country, Reason = "Sale CountryID=42 not in item.CountryIDs=[1,2].", Item = new ServiceItemSnapshot { CountryIDs = new() { 1, 2 } } },
                new ServiceItemEligibilityDecision { ServiceItemID = "SI-OLD", Name = "2022 Campaign", Verdict = EligibilityVerdict.Rejected, RejectionStage = EligibilityRejectionStage.CampaignWindow, Reason = "freeServiceStartDate=2024-02-10 not within [2022-01-01, 2022-12-31].", Item = new ServiceItemSnapshot { CampaignStartDate = new DateTime(2022,1,1), CampaignEndDate = new DateTime(2022,12,31), CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.WarrantyActivation } },
            },
        };

        trace.FreeBuilds = new()
        {
            new ServiceItemTraceBuild { ServiceItemID = "SI-005K", Name = "5,000 km Free Service" },
            new ServiceItemTraceBuild { ServiceItemID = "SI-010K", Name = "10,000 km Free Service" },
            new ServiceItemTraceBuild { ServiceItemID = "SI-INSP1", Name = "Annual Inspection Bonus" },
        };

        trace.PaidBuilds = new()
        {
            new ServiceItemTracePaidBuild { ServiceItemID = "PS-OIL", PaidServiceInvoiceLineID = "LINE-1", Name = "Oil Change Package", ActivatedAt = new DateTime(2024,5,15), ExpiresAt = new DateTime(2025,5,15), PackageCode = "PKG-OIL-12M" },
        };

        trace.WarrantyRollingExpansion = new ServiceItemTraceWarrantyRollingExpansion
        {
            AnchorDate = new DateTime(2024, 2, 10),
            SequentialItems = new()
            {
                new ServiceItemTraceRollingItem { ServiceItemID = "SI-005K", Name = "5,000 km Free Service", MaximumMileage = 5000, ActiveFor = 6, ActiveForDurationType = DurationType.Months, ActivatedAt = new DateTime(2024,2,10), ExpiresAt = new DateTime(2024,8,10) },
                new ServiceItemTraceRollingItem { ServiceItemID = "SI-010K", Name = "10,000 km Free Service", MaximumMileage = 10000, ActiveFor = 6, ActiveForDurationType = DurationType.Months, ActivatedAt = new DateTime(2024,8,10), ExpiresAt = new DateTime(2025,2,10) },
            },
        };

        trace.VehicleInspectionExpansions = new()
        {
            new ServiceItemTraceTriggerExpansion
            {
                ServiceItemID = "SI-INSP1", Name = "Annual Inspection Bonus", ActivationType = ClaimableItemCampaignActivationTypes.EveryTrigger,
                CandidateTriggerCount = 2, SelectedTriggerCount = 2,
                Outputs = new()
                {
                    new ServiceItemTraceTriggerOutput { TriggerID = "INSP-2024-A", TriggerDate = new DateTime(2024,3,1), ActivatedAt = new DateTime(2024,3,1), ExpiresAt = new DateTime(2024,9,1) },
                    new ServiceItemTraceTriggerOutput { TriggerID = "INSP-2025-A", TriggerDate = new DateTime(2025,3,5), ActivatedAt = new DateTime(2025,3,5), ExpiresAt = new DateTime(2025,9,5) },
                },
            },
        };

        trace.Statuses = new()
        {
            new ServiceItemTraceStatus { ServiceItemID = "SI-005K", Name = "5,000 km Free Service", Status = VehcileServiceItemStatuses.Processed, ClaimMatched = true, ClaimMatchedJobNumber = "WIP-9981", ClaimMatchedInvoiceNumber = "INV-7712", Claimable = false, ClaimabilityReason = "Already processed (claim line matched).", ActivatedAt = new DateTime(2024,2,10), ExpiresAt = new DateTime(2024,8,10) },
            new ServiceItemTraceStatus { ServiceItemID = "SI-010K", Name = "10,000 km Free Service", Status = VehcileServiceItemStatuses.Pending, Claimable = true, ClaimabilityReason = "Pending status → claimable.", ActivatedAt = new DateTime(2024,8,10), ExpiresAt = new DateTime(2025,2,10) },
            new ServiceItemTraceStatus { ServiceItemID = "SI-INSP1", VehicleInspectionID = "INSP-2024-A", Name = "Annual Inspection Bonus", Status = VehcileServiceItemStatuses.Pending, Claimable = true, ActivatedAt = new DateTime(2024,3,1), ExpiresAt = new DateTime(2024,9,1) },
            new ServiceItemTraceStatus { ServiceItemID = "SI-INSP1", VehicleInspectionID = "INSP-2025-A", Name = "Annual Inspection Bonus", Status = VehcileServiceItemStatuses.Pending, Claimable = true, ActivatedAt = new DateTime(2025,3,5), ExpiresAt = new DateTime(2025,9,5) },
            new ServiceItemTraceStatus { ServiceItemID = "PS-OIL", Name = "Oil Change Package", Status = VehcileServiceItemStatuses.Pending, Claimable = true, ActivatedAt = new DateTime(2024,5,15), ExpiresAt = new DateTime(2025,5,15) },
        };

        trace.PostProcessing = new ServiceItemTracePostProcessing { IneligibleItemsPickedUp = 1 };

        trace.FinalResult = new ServiceItemTraceFinalResult
        {
            Count = 5, ActivationRequired = false,
            Items = new()
            {
                new ServiceItemTraceFinalItem { ServiceItemID = "SI-005K", Name = "5,000 km Free Service", Type = "free", Status = "Processed", Claimable = false, ActivatedAt = new DateTime(2024,2,10), ExpiresAt = new DateTime(2024,8,10), MaximumMileage = 5000 },
                new ServiceItemTraceFinalItem { ServiceItemID = "SI-010K", Name = "10,000 km Free Service", Type = "free", Status = "Pending", Claimable = true, ActivatedAt = new DateTime(2024,8,10), ExpiresAt = new DateTime(2025,2,10), MaximumMileage = 10000 },
                new ServiceItemTraceFinalItem { ServiceItemID = "SI-INSP1", Name = "Annual Inspection Bonus", Type = "free", Status = "Pending", Claimable = true, ActivatedAt = new DateTime(2024,3,1), ExpiresAt = new DateTime(2024,9,1), VehicleInspectionID = "INSP-2024-A" },
                new ServiceItemTraceFinalItem { ServiceItemID = "SI-INSP1", Name = "Annual Inspection Bonus", Type = "free", Status = "Pending", Claimable = true, ActivatedAt = new DateTime(2025,3,5), ExpiresAt = new DateTime(2025,9,5), VehicleInspectionID = "INSP-2025-A" },
                new ServiceItemTraceFinalItem { ServiceItemID = "PS-OIL", Name = "Oil Change Package", Type = "paid", Status = "Pending", Claimable = true, ActivatedAt = new DateTime(2024,5,15), ExpiresAt = new DateTime(2025,5,15) },
            },
        };

        trace.StageTimings = new()
        {
            new ServiceItemTraceStageTiming { Stage = "LoadServiceItems", Elapsed = TimeSpan.FromMilliseconds(82) },
            new ServiceItemTraceStageTiming { Stage = "Eligibility", Elapsed = TimeSpan.FromMilliseconds(3) },
            new ServiceItemTraceStageTiming { Stage = "NameResolution", Elapsed = TimeSpan.FromMilliseconds(48) },
        };

        return trace;
    }
}
