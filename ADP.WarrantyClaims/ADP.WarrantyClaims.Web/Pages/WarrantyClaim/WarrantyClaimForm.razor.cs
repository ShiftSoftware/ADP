using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Service;
using ShiftSoftware.ShiftBlazor.Enums;
using ShiftSoftware.ShiftBlazor.Utils;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.WarrantyClaims.Web.Pages.WarrantyClaim;

public partial class WarrantyClaimForm
{
    [Parameter]
    [SupplyParameterFromQuery]
    public bool ForceSaveAsNew { get; set; }

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private bool isDistributor = false;

    private bool loadingRates = false;
    private bool errorLoadingRates = false;

    private bool EnableVINRelatedFields = false;

    private int? LastOdometer = null;

    private DeliveryDateEvaluationDTO? DeliveryDateEvaluation = null;
    private DateTime? OriginalDeliveryDate = null;
    private DateTime? PreDeliveryStashedDeliveryDate = null;
    private DateTime? PreDeliveryStashedAcInstallDate = null;

    public List<FlatRateDTO> FlatRates { get; set; } = new();
    public bool LoadingFlatRate { get; set; }

    protected async override Task OnInitializedAsync()
    {
        this.isDistributor = capabilityProvider.IsDistributor;

        await base.OnInitializedAsync();
    }

    private void LaborChanged(WarrantyClaimLaborLineDTO line, string newValue)
    {
        line.OperationNumber = newValue;

        if (Mode < FormModes.Edit)
            return;

        line.Hour = null;

        var flatRate = FlatRates.FirstOrDefault(x => x.LaborCode == newValue);

        if (flatRate is not null)
            line.Hour = flatRate.Times.Where(x => x.Key == TheItem.YearModel?.ToString()).Select(x => x.Value).FirstOrDefault();
    }

    private async void PartChanged(WarrantyClaimPartLineDTO line, string newValue)
    {
        line.PartNumber = newValue;

        if (Mode < FormModes.Edit)
            return;

        line.PartDescription = null;

        line.Price = null;

        line.Loading = true;

        try
        {
            var part = (await this.Http.GetFromJsonAsync<PartLookupDTO>($"WarrantyClaim/part-lookup/{newValue}"))!;

            line.PartDescription = part.PartDescription;

            line.Price = part.Prices.FirstOrDefault()?.WarrantyPrice?.Value;

            line.DistributorPrice = part.DistributorPurchasePrice;

            line.FoundInLookup = true;
        }
        catch
        {

        }

        line.Loading = false;

        this.StateHasChanged();
    }

    private async Task VINChanged()
    {
        TheItem.Odometer = null;
        TheItem.DeliveryDate = null;
        this.EnableVINRelatedFields = false;
        this.DeliveryDateEvaluation = null;
        this.OriginalDeliveryDate = null;
        this.LastOdometer = null;

        StateHasChanged();

        if (this.TheItem.VIN.Trim().Length == 17)
        {
            try
            {
                var url = $"WarrantyClaim/vin-lookup/{this.TheItem.VIN.Trim()}?preDelivery={this.TheItem.PreDelivery}";

                if (!string.IsNullOrWhiteSpace(TheItem.ID))
                    url += $"&excludeClaimId={Uri.EscapeDataString(TheItem.ID)}";

                var lookupResult = (await this.Http.GetFromJsonAsync<VinLookupResultDTO>(url))!;

                this.LastOdometer = lookupResult?.Odometer;
                this.DeliveryDateEvaluation = lookupResult?.DeliveryDate;

                TheItem.DeliveryDate = this.DeliveryDateEvaluation?.VerifiedDate ?? this.DeliveryDateEvaluation?.SuggestedDate;
                this.OriginalDeliveryDate = TheItem.DeliveryDate;

                this.EnableVINRelatedFields = true;
            }
            catch
            {

            }
        }

        await this.FlatRateComponentChanged();

        StateHasChanged();
    }

    private bool IsDeliveryDateLocked => this.DeliveryDateEvaluation?.VerifiedDate is not null;

    private void PreDeliveryChanged(bool v)
    {
        TheItem.PreDelivery = v;

        if (v)
        {
            this.PreDeliveryStashedDeliveryDate = TheItem.DeliveryDate;
            this.PreDeliveryStashedAcInstallDate = TheItem.AcInstallDate;
            TheItem.DeliveryDate = null;
            TheItem.AcInstallDate = null;
        }
        else
        {
            TheItem.DeliveryDate = this.PreDeliveryStashedDeliveryDate;
            TheItem.AcInstallDate = this.PreDeliveryStashedAcInstallDate;
        }
    }

    private async Task LoadDeliveryDateEvaluationAsync()
    {
        try
        {
            var url = $"WarrantyClaim/vin-lookup/{TheItem.VIN.Trim()}?preDelivery={TheItem.PreDelivery}";

            if (!string.IsNullOrWhiteSpace(TheItem.ID))
                url += $"&excludeClaimId={Uri.EscapeDataString(TheItem.ID)}";

            var lookupResult = (await this.Http.GetFromJsonAsync<VinLookupResultDTO>(url))!;

            this.LastOdometer = lookupResult?.Odometer;
            this.DeliveryDateEvaluation = lookupResult?.DeliveryDate;
            this.OriginalDeliveryDate = TheItem.DeliveryDate;

            if (this.DeliveryDateEvaluation?.VerifiedDate is not null)
                TheItem.DeliveryDate = this.DeliveryDateEvaluation.VerifiedDate;

            this.EnableVINRelatedFields = true;
        }
        catch
        {

        }
    }

    private async Task<bool> ConfirmDeliveryDatePropagationAsync()
    {
        var siblingCount = this.DeliveryDateEvaluation?.SiblingCount ?? 0;

        var selfChanging = TheItem.DeliveryDate is not null && TheItem.DeliveryDate != this.OriginalDeliveryDate;
        var hasSiblings = siblingCount > 0;

        // Nothing to confirm when the date isn't actually changing.
        if (!selfChanging) return true;

        // User manually edited the date on an isolated claim — no propagation, no silent override. Trust the input.
        if (!IsDeliveryDateLocked && !hasSiblings) return true;

        var newDate = TheItem.DeliveryDate!.Value.ToString("yyyy-MM-dd");
        var oldDate = this.OriginalDeliveryDate?.ToString("yyyy-MM-dd") ?? "(none)";

        var message = new System.Text.StringBuilder();

        if (IsDeliveryDateLocked)
        {
            message.Append($"This claim's Delivery Date will be aligned to <b>{newDate}</b> (currently <b>{oldDate}</b>) — the verified date for this VIN.");
        }
        else
        {
            message.Append($"This claim's Delivery Date will be saved as <b>{newDate}</b> (was <b>{oldDate}</b>).");
        }

        if (hasSiblings)
        {
            message.Append($"<br/><br/>It will also be applied to <b>{siblingCount}</b> other claim(s) for this VIN.");
        }

        var confirm = await DialogService.ShowMessageBoxAsync(
            title: "Confirm Delivery Date change",
            markupMessage: new MarkupString(message.ToString()),
            yesText: "Save",
            cancelText: "Cancel"
        );

        return confirm == true;
    }

    // PRR1 and the three exchange rates are required by WarrantyClaimValidator but have no input on this
    // form (they come from the central rate Setting via LoadRates). When they are 0 the model is invalid
    // yet no field can render the error, so Save appears to do nothing. Guard against that explicitly.
    private bool RatesAreConfigured =>
        TheItem.PRR1 != 0
        && TheItem.LaborExchangeRate != 0
        && TheItem.SubletExchangeRate != 0
        && TheItem.PartExchangeRate != 0;

    private async Task OnTaskStart(ShiftEvent<FormTasks> e)
    {
        if (e.Data != FormTasks.Save && e.Data != FormTasks.SaveAsNew)
            return;

        // The rate fields have no input on this form, so the framework can't render an inline error for
        // them — surface the specific reason in a dialog. We deliberately do NOT preventDefault: the
        // framework still runs its normal validation (marking every visible invalid field red) and aborts
        // the save on the invalid rate fields anyway.
        if (!RatesAreConfigured)
        {
            await DialogService.ShowMessageBoxAsync(
                title: "Warranty rates not configured",
                markupMessage: new MarkupString(
                    "This claim can't be saved because the P.R.R. and exchange rates are not set up.<br/><br/>" +
                    "These come from the warranty <b>Setting</b> and are required for every claim. " +
                    "Please configure them in Settings, then reopen this form."),
                yesText: "OK");

            return;
        }

        // The form is long, so inline field errors are easy to scroll past. Run the same validator the
        // framework uses and, if anything fails, also summarize the problems in a dialog. We do NOT
        // preventDefault: the framework's own validation still marks each field red inline and stops the
        // save — the dialog is only an additional up-front summary.
        // (Scrolling to / focusing the first invalid field is a ShiftBlazor-level concern, tracked separately.)
        var validation = new WarrantyClaimValidator().Validate(TheItem);
        if (!validation.IsValid)
        {
            var body = new System.Text.StringBuilder(
                "Please correct the following before saving:<ul style=\"margin:8px 0 0; padding-inline-start:20px;\">");

            foreach (var message in validation.Errors.Select(x => x.ErrorMessage).Distinct())
                body.Append($"<li>{System.Net.WebUtility.HtmlEncode(message)}</li>");

            body.Append("</ul>");

            await DialogService.ShowMessageBoxAsync(
                title: "The form can't be saved yet",
                markupMessage: new MarkupString(body.ToString()),
                yesText: "OK");

            return;
        }

        var ok = await ConfirmDeliveryDatePropagationAsync();
        if (!ok)
            e.ShouldPreventDefault = true;
    }

    private async Task FlatRateComponentChanged()
    {
        this.FlatRates.Clear();

        if (string.IsNullOrWhiteSpace(TheItem?.Franchise) || TheItem?.YearModel == null || string.IsNullOrWhiteSpace(TheItem?.VIN_WMI) || string.IsNullOrWhiteSpace(TheItem?.VIN_VDS))
        {
            return;
        }

        this.LoadingFlatRate = true;
        
        try
        {
            var brand = (
                this.TheItem.Franchise == Franchises.Toyota.Key ? "bArLe" :
                this.TheItem.Franchise == Franchises.Lexus.Key ? "xAYGe" :
                ""
            );

            this.FlatRates = (await this.Http.GetFromJsonAsync<List<FlatRateDTO>>($"WarrantyClaim/flat-rate/{this.TheItem.VIN_VDS}/{this.TheItem.VIN_WMI}/{brand}"))!;

            this.FlatRates = this.FlatRates
                .Where(x => x.BrandID == brand)
                .ToList();
        }
        catch
        {

        }

        this.LoadingFlatRate = false;

        this.StateHasChanged();
    }

    private async Task<IEnumerable<string>> LaborSearch(string value, CancellationToken token)
    {
        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
            return this.FlatRates.Select(x => x.LaborCode!);

        return FlatRates.Where(x => x.LaborCode!.Contains(value, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.LaborCode!);
    }

    private async Task LoadRates()
    {
        loadingRates = true;
        errorLoadingRates = false;

        try
        {
            // Phase 3 Slice 3.3: the module API now serves the rates (IWarrantyRatesStore) — the form no
            // longer reads the host's own Setting route.
            var warrantyRates = await this.Http.GetFromJsonAsync<WarrantyRatesDTO>("WarrantyClaim/current-rates");

            // PRR1 / exchange rates are required by WarrantyClaimValidator (NotEmpty rejects 0) but have
            // no input on this form, so if they stay 0 the Save silently fails validation with no visible
            // error. Treat a missing rate Setting (null body) or any zero rate as "not configured" and
            // surface it, instead of copying zeros in and letting the save no-op.
            if (warrantyRates is null
                || warrantyRates.PRR == 0
                || warrantyRates.LaborExchangeRate == 0
                || warrantyRates.SubletExchangeRate == 0
                || warrantyRates.PartExchangeRate == 0)
            {
                errorLoadingRates = true;
            }
            else
            {
                TheItem.LaborExchangeRate = warrantyRates.LaborExchangeRate;
                TheItem.SubletExchangeRate = warrantyRates.SubletExchangeRate;
                TheItem.PartExchangeRate = warrantyRates.PartExchangeRate;
                TheItem.PRR1 = warrantyRates.PRR;
            }

            loadingRates = false;
        }

        catch
        {
            loadingRates = false;
            errorLoadingRates = true;
        }

        StateHasChanged();
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (Mode == FormModes.Create)
            {
                await this.LoadRates();

                //SetTestData();
            }

            await this.FlatRateComponentChanged();
        }
    }

    private async Task OnTaskFinished(FormTasks task)
    {
        if (task == FormTasks.Fetch && !string.IsNullOrWhiteSpace(TheItem?.VIN) && TheItem.VIN.Trim().Length == 17)
        {
            await this.LoadDeliveryDateEvaluationAsync();
        }

        if (Mode != FormModes.View && !ForceSaveAsNew)
            return;

        if (task == FormTasks.Fetch)
        {
            if (this.ForceSaveAsNew)
            {
                TheItem.ReferenceWarrantyClaim = new ShiftSoftware.ShiftEntity.Model.Dtos.ShiftEntitySelectDTO
                {
                    Value = TheItem.ID!,
                    Text = TheItem.ClaimNumber
                };

                TheItem.ClaimNumber = null;

                TheItem.WarrantyClaimLaborLines.ForEach(x => x.ID = 0);
                TheItem.WarrantyClaimPartLines.ForEach(x => x.ID = 0);
                TheItem.WarrantyClaimSubletLines.ForEach(x => x.ID = 0);

                TheItem.ProcessDate = null;
                TheItem.DistributorProcessDate = null;
                TheItem.DealerCode = null;
                TheItem.DealerClaimNo = null;
                TheItem.ProcessFlg = ShiftSoftware.ADP.WarrantyClaims.Shared.Enums.ProcessFlags.ResubmissionToVendorForThatOnceReturnedOrRejectedByVendor;
            }
        }

        //Load Labor Rates
        await this.FlatRateComponentChanged();

        if (!capabilityProvider.IsDistributor)
        {
            StateHasChanged();

            return;
        }

        foreach (var partLine in TheItem.WarrantyClaimPartLines.Where(x => x.DistributorPrice is null && x.FoundInLookup && x.DistributorPrice is null))
        {
            try
            {
                var part = (await this.Http.GetFromJsonAsync<PartLookupDTO>($"WarrantyClaim/part-lookup/{partLine.PartNumber}"))!;

                partLine.DistributorPrice = part.DistributorPurchasePrice;
            }
            catch
            {

            }
        }

        StateHasChanged();
    }

    private void SetTestData()
    {
        var mock = new ShiftSoftware.ADP.WarrantyClaims.Shared.Mocks.WarrantyClaim().DealerClaim;

        mock.WarrantyClaimLaborLines.ForEach(x => x.ID = 0);
        mock.WarrantyClaimSubletLines.ForEach(x => x.ID = 0);
        mock.WarrantyClaimPartLines.ForEach(x => x.ID = 0);

        TheItem.ClaimNumber = mock.ClaimNumber;
        //TheItem.DealerCode = mock.DealerCode;
        //TheItem.DealerClaimNo = mock.DealerClaimNo;
        TheItem.DateOfReceipt = mock.DateOfReceipt;
        TheItem.ProcessFlg = mock.ProcessFlg;
        TheItem.WarrantyType = mock.WarrantyType;
        TheItem.OperationType = mock.OperationType;
        TheItem.Franchise = mock.Franchise;
        TheItem.VIN_WMI = mock.VIN_WMI;
        TheItem.VIN_VDS = mock.VIN_VDS;
        TheItem.VIN_CD = mock.VIN_CD;
        TheItem.VIN_VIS = mock.VIN_VIS;
        TheItem.DeliveryDate = mock.DeliveryDate;
        TheItem.RepairDate = mock.RepairDate;
        TheItem.RepairCompletionDate = mock.RepairCompletionDate;
        TheItem.Odometer = mock.Odometer;
        TheItem.KMFlg = mock.KMFlg;
        TheItem.RepairOrderNo = mock.RepairOrderNo;
        TheItem.DataID = mock.DataID;
        TheItem.T1 = mock.T1;
        TheItem.T2 = mock.T2;
        TheItem.T3_1 = mock.T3_1;
        TheItem.T3_2 = mock.T3_2;
        TheItem.T3_3 = mock.T3_3;
        TheItem.T3_4 = mock.T3_4;
        TheItem.T3_5 = mock.T3_5;
        TheItem.T3_6 = mock.T3_6;
        TheItem.T3_7 = mock.T3_7;
        TheItem.Condition = mock.Condition;
        TheItem.Cause = mock.Cause;
        TheItem.Remedy = mock.Remedy;
        TheItem.DealerComments = mock.DealerComments;
        TheItem.DistComment1 = mock.DistComment1;
        TheItem.BatteryTestCode11 = mock.BatteryTestCode11;
        TheItem.BatteryTestCode12 = mock.BatteryTestCode12;
        TheItem.BatteryTestCode21 = mock.BatteryTestCode21;
        TheItem.BatteryTestCode22 = mock.BatteryTestCode22;
        TheItem.TSB = mock.TSB;
        TheItem.SubletDescription = mock.SubletDescription;
        TheItem.YearModel = mock.YearModel;

        TheItem.WarrantyClaimLaborLines.AddRange(mock.WarrantyClaimLaborLines);

        TheItem.WarrantyClaimSubletLines.AddRange(mock.WarrantyClaimSubletLines);

        TheItem.WarrantyClaimPartLines.AddRange(mock.WarrantyClaimPartLines);

        // PRR1 / exchange rates are required by WarrantyClaimValidator but have no input field — they are
        // normally filled by LoadRates() from WarrantyClaim/current-rates. If no WarrantyRates row exists
        // (typical in a dev DB) they stay 0 and client-side validation silently blocks the save with no visible
        // message. Fall back to non-zero values so test claims can be created without configured rates.
        if (TheItem.PRR1 == 0) TheItem.PRR1 = 1;
        if (TheItem.LaborExchangeRate == 0) TheItem.LaborExchangeRate = 1;
        if (TheItem.SubletExchangeRate == 0) TheItem.SubletExchangeRate = 1;
        if (TheItem.PartExchangeRate == 0) TheItem.PartExchangeRate = 1;

        StateHasChanged();
    }
}