using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftBlazor.Enums;
using System.Globalization;
using System.Net.Http.Json;

namespace ShiftSoftware.ADP.ClaimableItems.Web.Pages.ClaimableItemContract;

/// <summary>
/// A service contract. The VIN and mileage drive a lookup of the vehicle's contract-type menu; the user picks
/// services from it. Prices, codes and descriptions shown here are a preview: the save reads the menu again
/// and stores what it finds there.
/// </summary>
public partial class ClaimableItemContractForm
{
    // 0% to 10%, the range the validator accepts.
    private static readonly decimal[] DefaultIncrementOptions = Enumerable.Range(0, 11).Select(x => x / 100m).ToArray();

    [Parameter]
    public object? Key { get; set; }

    private FormModes Mode { get; set; }

    private ClaimableItemContractDTO Contract { get; set; } = new();

    private ClaimableItemContractVehicleLookupDTO? lookup;
    private bool lookupLoading;
    private bool lookupFailed;

    // Bumped by every lookup, so the answer to an older request (the user kept typing) is dropped.
    private int lookupVersion;

    private bool ReadOnly => Mode < FormModes.Edit;

    private bool IsExisting => !string.IsNullOrWhiteSpace(Contract.ID);

    /// <summary>Only a draft can be changed or deleted.</summary>
    private bool IsEditable => !IsExisting || Contract.Status == ClaimableItemContractStatus.Draft;

    protected override void OnInitialized()
    {
        if (Key is null)
        {
            Contract.StartDate = DateTime.Today;
            Contract.DurationUnit = ClaimableItemContractDurationUnit.Years;
        }
    }

    // Entering edit reads the menu afresh (prices may have moved since the last edit); leaving it drops it.
    private Task ModeChangedAsync() => LoadLookupAsync();

    private async Task OnTaskFinished(FormTasks task)
    {
        if (task == FormTasks.Fetch && !ReadOnly)
            await LoadLookupAsync();
    }

    private async Task VinChangedAsync(string? vin)
    {
        Contract.VIN = vin?.Trim().ToUpperInvariant() ?? string.Empty;
        await LoadLookupAsync();
    }

    private async Task MileageChangedAsync(long? mileage)
    {
        Contract.Mileage = mileage;
        await LoadLookupAsync();
    }

    private async Task CountryChangedAsync(string? countryId)
    {
        Contract.Country = lookup?.Countries.FirstOrDefault(x => x.Value == countryId);
        await LoadLookupAsync();
    }

    private async Task TransferRateChangedAsync(decimal rate)
    {
        Contract.TransferRate = rate;
        await LoadLookupAsync();
    }

    private async Task LoadLookupAsync()
    {
        var version = ++lookupVersion;
        var vin = Contract.VIN?.Trim();

        if (ReadOnly || vin is not { Length: 17 } || Contract.Mileage is null)
        {
            lookup = null;
            lookupLoading = false;
            lookupFailed = false;
            return;
        }

        lookupLoading = true;
        lookupFailed = false;
        StateHasChanged();

        ClaimableItemContractVehicleLookupDTO? result = null;
        var failed = false;

        try
        {
            result = await Http.GetFromJsonAsync<ClaimableItemContractVehicleLookupDTO>(LookupUrl(vin));
        }
        catch
        {
            failed = true;
        }

        if (version != lookupVersion)
            return;

        lookup = result;
        lookupFailed = failed || result is null;
        lookupLoading = false;

        if (result is not null)
            ApplyLookup(result);

        StateHasChanged();
    }

    private string LookupUrl(string vin)
    {
        var query = new List<string> { $"mileage={Contract.Mileage!.Value.ToString(CultureInfo.InvariantCulture)}" };

        // Once the server has said the country is fixed, it ignores both, so they are not sent.
        if (lookup?.CountryIsFixed != true && !string.IsNullOrWhiteSpace(Contract.Country?.Value))
        {
            query.Add($"countryId={Uri.EscapeDataString(Contract.Country.Value)}");
            query.Add($"transferRate={Contract.TransferRate.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(Contract.MenuLanguage))
            query.Add($"language={Uri.EscapeDataString(Contract.MenuLanguage)}");

        if (IsExisting)
            query.Add($"contractId={Uri.EscapeDataString(Contract.ID!)}");

        return $"{CIOptions.Value.ResolvedRoutePrefix}ClaimableItemContract/vin-lookup/{Uri.EscapeDataString(vin)}?{string.Join("&", query)}";
    }

    /// <summary>
    /// Copies the vehicle, the server-chosen country and the menu's current prices onto the contract. A
    /// selected line the menu no longer offers is kept and flagged, so nothing the user picked vanishes.
    /// </summary>
    private void ApplyLookup(ClaimableItemContractVehicleLookupDTO result)
    {
        Contract.Katashiki = result.Katashiki;
        Contract.BasicModelCode = result.BasicModelCode;
        Contract.Variant = result.Variant;
        Contract.ModelCode = result.ModelCode;
        Contract.ModelDescription = result.ModelDescription;
        Contract.MenuLanguage = result.MenuLanguage;

        if (result.CountryIsFixed)
        {
            Contract.Country = result.Country;
            Contract.TransferRate = result.TransferRate;
        }

        if (result.Status != ClaimableItemContractLookupStatus.Found)
            return;

        foreach (var line in Contract.Lines)
        {
            var menuLine = result.Lines.FirstOrDefault(x => x.MenuLineKey == line.MenuLineKey && x.UnavailableReason is null);

            if (menuLine is not null)
                CopyMenuLine(menuLine, line);
        }
    }

    private (Severity Severity, string Text)? LookupMessage
    {
        get
        {
            if (lookupFailed)
                return (Severity.Error, "The vehicle lookup failed. Check the VIN and try again.");

            if (lookup is null)
                return Contract.VIN?.Trim().Length == 17 && Contract.Mileage is not null
                    ? null
                    : (Severity.Info, "Enter the VIN and the mileage to see the vehicle's contract menu.");

            return lookup.Status switch
            {
                ClaimableItemContractLookupStatus.NotOfficial => (Severity.Warning, "This VIN is not one of the official vehicles. Contracts for other vehicles are not supported yet."),
                ClaimableItemContractLookupStatus.CountryRequired => (Severity.Info, "Select the country whose menu prices the contract uses."),
                ClaimableItemContractLookupStatus.MenuNotFound => (Severity.Warning, "This vehicle has no contract menu."),
                ClaimableItemContractLookupStatus.MenuUnavailable => (Severity.Error, "The vehicle's service menu could not be read. Try again later."),
                _ => null,
            };
        }
    }

    private IEnumerable<IGrouping<ClaimableItemContractLineType, ClaimableItemContractMenuLineDTO>> OfferedGroups =>
        (lookup?.Lines ?? new())
            .OrderBy(x => x.LineType)
            .ThenBy(x => x.ServiceIntervalValueInMeter ?? 0)
            .ThenBy(x => x.VariantName)
            .ThenBy(x => x.Description)
            .GroupBy(x => x.LineType);

    private int SelectedCount(ClaimableItemContractMenuLineDTO menuLine) =>
        Contract.Lines.Count(x => x.MenuLineKey == menuLine.MenuLineKey);

    private void TogglePeriodic(ClaimableItemContractMenuLineDTO menuLine, bool selected)
    {
        if (selected)
        {
            if (SelectedCount(menuLine) == 0)
                AddLine(menuLine);
        }
        else
        {
            Contract.Lines.RemoveAll(x => x.MenuLineKey == menuLine.MenuLineKey);
        }
    }

    private void AddLine(ClaimableItemContractMenuLineDTO menuLine)
    {
        var line = new ClaimableItemContractLineDTO();
        CopyMenuLine(menuLine, line);

        // The duration of a periodic service is the user's call; only the unit gets a starting value.
        if (line.LineType == ClaimableItemContractLineType.Periodic)
            line.DurationUnit = ClaimableItemContractDurationUnit.Months;

        Contract.Lines.Add(line);
    }

    private void RemoveOne(ClaimableItemContractMenuLineDTO menuLine)
    {
        var index = Contract.Lines.FindLastIndex(x => x.MenuLineKey == menuLine.MenuLineKey);

        if (index >= 0)
            Contract.Lines.RemoveAt(index);
    }

    private static void CopyMenuLine(ClaimableItemContractMenuLineDTO menuLine, ClaimableItemContractLineDTO line)
    {
        line.MenuLineKey = menuLine.MenuLineKey;
        line.MenuCode = menuLine.MenuCode;
        line.LabourCode = menuLine.LabourCode;
        line.VariantID = menuLine.VariantID;
        line.VariantName = menuLine.VariantName;
        line.LineType = menuLine.LineType;
        line.ServiceIntervalCode = menuLine.ServiceIntervalCode;
        line.ServiceIntervalValueInMeter = menuLine.ServiceIntervalValueInMeter;
        line.Description = menuLine.Description;
        line.LabourTotalPrice = menuLine.LabourTotalPrice;
        line.PartsTotalPrice = menuLine.PartsTotalPrice;
        line.DiscountAmount = menuLine.DiscountAmount;
        line.Cost = menuLine.Price;
    }

    private bool IsNotOffered(ClaimableItemContractLineDTO line) =>
        lookup is { Status: ClaimableItemContractLookupStatus.Found }
        && !lookup.Lines.Any(x => x.MenuLineKey == line.MenuLineKey && x.UnavailableReason is null);

    /// <summary>Periodic lines by interval, then the standalone ones in the order they were added.</summary>
    private IEnumerable<ClaimableItemContractLineDTO> OrderedLines =>
        Contract.Lines
            .Where(x => x.LineType == ClaimableItemContractLineType.Periodic)
            .OrderBy(x => x.ServiceIntervalValueInMeter ?? 0)
            .Concat(Contract.Lines.Where(x => x.LineType != ClaimableItemContractLineType.Periodic));

    private IEnumerable<decimal> IncrementOptions =>
        DefaultIncrementOptions.Contains(Contract.PriceIncrement)
            ? DefaultIncrementOptions
            : DefaultIncrementOptions.Append(Contract.PriceIncrement).Order();

    private DateTime? EndDate =>
        ReadOnly && Contract.EndDate is not null
            ? Contract.EndDate
            : Contract.StartDate is { } start && Contract.Duration is > 0 && Contract.DurationUnit is { } unit
                ? ClaimableItemContractDurations.AddTo(start.Date, Contract.Duration.Value, unit)
                : null;

    private List<(ClaimableItemContractLineDTO Line, DateTime? ExpireDate)> PeriodicExpiries =>
        Contract.StartDate is { } start
            ? ClaimableItemContractCalculations.RollPeriodicExpiry(
                start.Date,
                Contract.Lines.Where(x => x.LineType == ClaimableItemContractLineType.Periodic),
                x => x.ServiceIntervalValueInMeter,
                x => x.Duration,
                x => x.DurationUnit)
            : new();

    /// <summary>The stored expiry once the contract is issued; until then the expiry it would get.</summary>
    private DateTime? ExpiryOf(ClaimableItemContractLineDTO line)
    {
        if (line.ExpireDate is not null)
            return line.ExpireDate;

        if (line.LineType != ClaimableItemContractLineType.Periodic)
            return EndDate;

        return PeriodicExpiries.FirstOrDefault(x => ReferenceEquals(x.Line, line)).ExpireDate;
    }

    private decimal PriceOf(ClaimableItemContractLineDTO line) =>
        ReadOnly ? line.PriceWithIncrement : ClaimableItemContractCalculations.PriceWithIncrement(line.Cost, Contract.PriceIncrement);

    private decimal Total => Contract.Lines.Sum(x => x.Cost);

    private decimal TotalWithIncrement => Contract.Lines.Sum(PriceOf);

    /// <summary>The last periodic service would outlive the contract. Issuing such a contract is refused.</summary>
    private string? PeriodicChainWarning
    {
        get
        {
            if (EndDate is not { } end)
                return null;

            var last = PeriodicExpiries.LastOrDefault();

            if (last.ExpireDate is not { } expiry || expiry <= end)
                return null;

            return $"The {last.Line.ServiceIntervalValueInMeter:#,0} km service would expire on {expiry:yyyy-MM-dd}, "
                + $"after the contract ends on {end:yyyy-MM-dd}. Shorten the service durations or lengthen the contract.";
        }
    }
}
