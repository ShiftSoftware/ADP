using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam over the warranty-rates persistence (Phase 3 Slice 3.3). The <c>current-rates</c>
/// endpoint and the manufacturer CSV export resolve this store lazily from the request services.
/// Since Phase 3 Slice 3.6 (D24) the module ships a default implementation over its own temporal
/// <c>WarrantyRates</c> entity (TryAdd — register your own store BEFORE the module extension to
/// keep rates in org-private storage instead).
/// </summary>
public interface IWarrantyRatesStore
{
    /// <summary>
    /// The latest persisted rates, or null when none exist yet. <see cref="WarrantyRatesDTO.ID"/> must
    /// be populated: the warranty claim list round-trips it through the export dialog so
    /// <see cref="PersistExportRatesAsync"/> can update the same row instead of inserting a new one.
    /// </summary>
    Task<WarrantyRatesDTO?> GetCurrentAsync();

    /// <summary>
    /// The audit-upsert half of the manufacturer CSV export: takes the raw <c>rates</c> query-string
    /// JSON exactly as the client sent it, persists it as the rates row used for this export (update
    /// when the payload carries the store's row ID, insert otherwise — every export leaves an audit
    /// row), and returns both the <see cref="WarrantyRatesDTO"/> the export math runs on and the
    /// opaque echo object placed in the response's <c>Additional["Rates"]</c>. The echo is
    /// deliberately consumer-shaped (the original host echoes its own Setting DTO — base
    /// view-and-upsert properties included — deserialized from <paramref name="ratesJson"/>) so the
    /// response JSON stays byte-identical to the pre-module endpoint.
    /// Implementations must only TRACK the persistence — the controller calls SaveChanges after the
    /// CSV file write (frozen ordering).
    /// </summary>
    Task<WarrantyRatesPersistResult> PersistExportRatesAsync(string ratesJson, long? userId);
}

/// <summary>Result pair of <see cref="IWarrantyRatesStore.PersistExportRatesAsync"/>.</summary>
public class WarrantyRatesPersistResult
{
    /// <summary>The rates the module's export amount-calculations run on.</summary>
    public WarrantyRatesDTO Rates { get; set; } = default!;

    /// <summary>
    /// The object echoed back to the client in <c>Additional["Rates"]</c>. Serialized by its runtime
    /// type, so the consumer controls the exact response shape.
    /// </summary>
    public object RatesEcho { get; set; } = default!;
}
