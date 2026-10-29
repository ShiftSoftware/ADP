using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>What the contract VIN lookup found. Only <see cref="Found"/> carries menu lines.</summary>
public enum ClaimableItemContractLookupStatus
{
    [Description("Found")]
    Found = 0,

    /// <summary>The VIN is not among the official vehicles. Contracts for these come later.</summary>
    [Description("Not an official vehicle")]
    NotOfficial = 1,

    /// <summary>Several countries are configured and none was chosen. The countries are returned.</summary>
    [Description("Select a country")]
    CountryRequired = 2,

    /// <summary>The vehicle has no contract menu.</summary>
    [Description("No contract menu")]
    MenuNotFound = 3,

    /// <summary>The menu could not be read (storage or generation failed, or the menu lookup is not registered).</summary>
    [Description("Menu unavailable")]
    MenuUnavailable = 4,
}
