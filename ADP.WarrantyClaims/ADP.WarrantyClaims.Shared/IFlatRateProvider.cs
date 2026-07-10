using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Service;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam behind the module's <c>flat-rate/{vds}/{wmi?}/{brandHashId}</c> endpoint (Phase 3
/// Slice 3.3). Flat-rate manuals are a warranty-universal concept (the module's claim form consumes
/// this endpoint), but the data sources are consumer-specific — the original host queries its own
/// <c>[GetFlatRate]</c> stored procedure and merges its supplementary labor-operation-code table, and
/// serves JSON fixtures in development. Resolved lazily from the request services — consumers that
/// never call the endpoint need no registration.
/// </summary>
public interface IFlatRateProvider
{
    /// <summary>
    /// The flat-rate entries for a VIN's VDS/WMI and the (hashid-encoded) brand. The consumer owns
    /// brand-hashid decoding and any brand-code mapping.
    /// </summary>
    Task<FlatRateLookupResult> GetFlatRatesAsync(string vds, string? wmi, string brandHashId);
}

/// <summary>Result of <see cref="IFlatRateProvider.GetFlatRatesAsync"/>.</summary>
public class FlatRateLookupResult
{
    public List<FlatRateDTO> FlatRates { get; set; } = new();

    /// <summary>
    /// When non-null the endpoint emits it as the <c>X-Total-Count</c> response header. The original
    /// host reports its stored-procedure row count here — and only on that code path, so the header's
    /// presence/absence is preserved exactly.
    /// </summary>
    public int? TotalCount { get; set; }
}
