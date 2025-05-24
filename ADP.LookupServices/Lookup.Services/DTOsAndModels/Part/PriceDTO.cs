using ShiftSoftware.ADP.Models;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

[TypeScriptModel]
public class PriceDTO
{
    public decimal? Value { get; set; }
    public string CurrecntySymbol { get; set; }
    public string CultureName { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public CultureInfo Culture { get; set; }
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
