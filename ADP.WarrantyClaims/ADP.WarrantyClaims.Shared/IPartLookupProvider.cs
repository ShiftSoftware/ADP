using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam behind the module's <c>part-lookup/{partNumber}</c> endpoint (Phase 3 Slice 3.3).
/// Keeps the module free of the lookup-service registration/storage requirements: the original host
/// wraps <c>PartLookupService.LookupAsync(partNumber, skipLogging: true)</c> (price visibility driven
/// by its own authorization nodes) and serves JSON fixtures in development. Resolved lazily from the
/// request services — consumers that never call the endpoint need no registration.
/// </summary>
public interface IPartLookupProvider
{
    Task<PartLookupDTO> LookupAsync(string partNumber);
}
