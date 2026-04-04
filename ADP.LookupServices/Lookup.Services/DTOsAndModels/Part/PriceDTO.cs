using ShiftSoftware.ADP.Models;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

/// <summary>
/// Represents a monetary value with currency and formatting information.
/// </summary>
[TypeScriptModel]
[Docable]
public class PriceDTO
{
    /// <summary>The numeric price value.</summary>
    public decimal? Value { get; set; }
    /// <summary>The currency symbol (e.g., $, د.ع).</summary>
    public string CurrencySymbol { get; set; }
    /// <summary>The culture name used for formatting (e.g., "en-US", "ar-IQ").</summary>
    public string CultureName { get; set; }

    [DocIgnore]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public CultureInfo Culture { get; set; }

    /// <summary>The price formatted as a currency string using the associated culture.</summary>
    public string FormattedValue
    {
        get
        {
            if (Value is null || Culture is null)
                return null;

            try
            {
                return Value.Value.ToString("C", Culture);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }

    public PriceDTO(decimal? value)
    {
        this.Value = value;
    }
}
