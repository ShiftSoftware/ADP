using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Services;

/// <summary>
/// Single source of truth for "what is the Delivery Date for this VIN?".
/// Owns the predicate that decides which claims count as "verified" — keep that logic here
/// so the rest of the system can ask one question without caring about claim/manufacturer status mappings.
/// </summary>
public class DeliveryDateService
{
    private static readonly ClaimStatus[] VerifiedClaimStatuses =
    {
        ClaimStatus.Certified,
        ClaimStatus.Invoiced,
    };

    private static readonly WarrantyManufacturerClaimStatus[] VerifiedManufacturerStatuses =
    {
        WarrantyManufacturerClaimStatus.Paid,
    };

    private readonly ShiftDbContext db;
    private readonly ILastServiceActivationDateProvider? lastServiceActivationDateProvider;

    public DeliveryDateService(ShiftDbContext db, ILastServiceActivationDateProvider? lastServiceActivationDateProvider = null)
    {
        this.db = db;
        this.lastServiceActivationDateProvider = lastServiceActivationDateProvider;
    }

    /// <summary>
    /// Rich evaluation for the form: verified date (if any), suggested date, and the sibling claims that
    /// would be touched on propagation. Excludes PreDelivery / null-date claims everywhere.
    /// </summary>
    public async Task<DeliveryDateEvaluationDTO> EvaluateAsync(string vin, long? excludeClaimId)
    {
        // Include the current claim when searching for a verified date — if the claim being edited is itself
        // verified (Certified/Invoiced/Paid), the field must be locked. excludeClaimId only affects the sibling count.
        var claims = await this.db.Set<WarrantyClaim>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.VIN == vin)
            .Where(x => x.DeliveryDate != null)
            .OrderByDescending(x => x.ID)
            .Select(x => new ClaimSnapshot(x.ID, x.DeliveryDate, x.ClaimStatus, x.ManufacturerStatus))
            .ToListAsync();

        var result = new DeliveryDateEvaluationDTO();

        var verifiedSource = claims.FirstOrDefault(IsVerified);

        if (verifiedSource is not null)
        {
            result.VerifiedDate = verifiedSource.DeliveryDate;
            result.VerifiedReason = DescribeVerifiedReason(verifiedSource.ClaimStatus, verifiedSource.ManufacturerStatus);
            result.SuggestedDate = verifiedSource.DeliveryDate;
        }
        else
        {
            var suggestionSource = claims.FirstOrDefault(x => excludeClaimId == null || x.ID != excludeClaimId);
            if (suggestionSource is not null)
            {
                result.SuggestedDate = suggestionSource.DeliveryDate;
            }
            else
            {
                result.SuggestedDate = await GetLastServiceActivationAsync(vin);
            }
        }

        // Mirrors what PropagateDeliveryDateAsync will actually write to: count of non-self, non-verified claims.
        // Identifiers omitted to avoid leaking other dealers' claim numbers.
        result.SiblingCount = claims
            .Where(x => excludeClaimId == null || x.ID != excludeClaimId)
            .Count(x => !IsVerified(x));

        return result;
    }

    /// <summary>
    /// Slim race-check used at save time. Returns the verified date if one exists for this VIN, else null.
    /// </summary>
    public async Task<DateTime?> GetVerifiedDeliveryDateAsync(string vin, long? excludeClaimId)
    {
        var verified = await this.db.Set<WarrantyClaim>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.VIN == vin)
            .Where(x => x.DeliveryDate != null)
            .Where(x => excludeClaimId == null || x.ID != excludeClaimId)
            .Where(x =>
                (x.ClaimStatus != null && VerifiedClaimStatuses.Contains(x.ClaimStatus.Value)) ||
                (x.ManufacturerStatus != null && VerifiedManufacturerStatuses.Contains(x.ManufacturerStatus.Value))
            )
            .OrderByDescending(x => x.ID)
            .Select(x => x.DeliveryDate)
            .FirstOrDefaultAsync();

        return verified;
    }

    /// <summary>
    /// Writes <paramref name="newDate"/> to every other claim sharing this VIN whose date is non-null and differs.
    /// Skips PreDelivery / null-date claims and verified claims — verified rows are immutable to the system,
    /// matching the rule in <see cref="WarrantyClaimService.ValidationAndAssignCalculatedFieldsAsync"/>.
    /// Caller is responsible for invoking SaveChangesAsync.
    /// </summary>
    public async Task<int> PropagateDeliveryDateAsync(string vin, long currentClaimId, DateTime newDate)
    {
        var siblings = await this.db.Set<WarrantyClaim>()
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.VIN == vin)
            .Where(x => x.ID != currentClaimId)
            .Where(x => x.DeliveryDate != null)
            .Where(x => x.DeliveryDate != newDate)
            .Where(x => !(
                (x.ClaimStatus != null && VerifiedClaimStatuses.Contains(x.ClaimStatus.Value)) ||
                (x.ManufacturerStatus != null && VerifiedManufacturerStatuses.Contains(x.ManufacturerStatus.Value))
            ))
            .ToListAsync();

        foreach (var sibling in siblings)
        {
            sibling.DeliveryDate = newDate;
        }

        return siblings.Count;
    }

    /// <summary>
    /// Admin escape hatch: forces <paramref name="newDate"/> onto EVERY non-PreDelivery claim for this VIN,
    /// INCLUDING verified (Certified/Invoiced/Paid) claims. This deliberately bypasses the "verified is immutable"
    /// rule enforced by <see cref="PropagateDeliveryDateAsync"/> and the form — it is the only way to correct a VIN
    /// whose wrong Delivery Date was locked in by a verified claim. Only the DeliveryDate field is changed.
    /// Uses IgnoreQueryFilters so claims across all dealers for the VIN are corrected.
    /// Caller is responsible for authorization and for invoking SaveChangesAsync.
    /// </summary>
    public async Task<int> OverrideDeliveryDateForVinAsync(string vin, DateTime newDate)
    {
        var claims = await this.db.Set<WarrantyClaim>()
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.VIN == vin)
            .Where(x => !x.PreDelivery)
            .Where(x => x.DeliveryDate != newDate)
            .ToListAsync();

        foreach (var claim in claims)
        {
            claim.DeliveryDate = newDate;
        }

        return claims.Count;
    }

    public async Task<int?> GetLastOdometerAsync(string vin)
    {
        return await this.db.Set<WarrantyClaim>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted)
            .Where(x => x.VIN == vin)
            .OrderByDescending(x => x.ID)
            .Select(x => x.Odometer)
            .FirstOrDefaultAsync();
    }

    private async Task<DateTime?> GetLastServiceActivationAsync(string vin)
    {
        // ServiceActivation is a consumer-owned entity; the fallback is served by an optional consumer seam.
        // When no provider is registered the suggestion is simply unavailable.
        if (this.lastServiceActivationDateProvider is null)
            return null;

        return await this.lastServiceActivationDateProvider.GetLastServiceActivationAsync(vin);
    }

    private static bool IsVerified(ClaimSnapshot claim)
    {
        return (claim.ClaimStatus != null && VerifiedClaimStatuses.Contains(claim.ClaimStatus.Value)) ||
               (claim.ManufacturerStatus != null && VerifiedManufacturerStatuses.Contains(claim.ManufacturerStatus.Value));
    }

    private record ClaimSnapshot(
        long ID,
        DateTime? DeliveryDate,
        ClaimStatus? ClaimStatus,
        WarrantyManufacturerClaimStatus? ManufacturerStatus
    );

    private static string DescribeVerifiedReason(ClaimStatus? claimStatus, WarrantyManufacturerClaimStatus? manufacturerStatus)
    {
        if (manufacturerStatus is not null && VerifiedManufacturerStatuses.Contains(manufacturerStatus.Value))
            return manufacturerStatus.Value.Describe();

        if (claimStatus is not null && VerifiedClaimStatuses.Contains(claimStatus.Value))
            return claimStatus.Value.Describe();

        return string.Empty;
    }
}
