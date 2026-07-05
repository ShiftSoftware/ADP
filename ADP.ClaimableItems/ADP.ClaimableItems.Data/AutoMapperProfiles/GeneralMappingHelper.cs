using System.Text.Json;

namespace ShiftSoftware.ADP.ClaimableItems.Data.AutoMapperProfiles;

// Module-local copy of the trivial localized-name deserializer (the TCA consumer keeps its own
// Services.Data.AutoMapperProfiles.GeneralMappingHelper for its remaining profiles).
public class GeneralMappingHelper
{
    public static Dictionary<string, string>? DeserializeDict(string? json) => json == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);
}
