using ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using System.Text.Json;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Services;

/// <summary>
/// The module's default <see cref="IWarrantyRatesStore"/> (Phase 3 Slice 3.6, D24): the warranty
/// rates live in the module-owned temporal <see cref="Entities.WarrantyRates"/> entity. Absorbs the
/// original host's store implementation byte-identically — both halves of the dissolved rates flow:
/// the export endpoint's <c>JsonSerializer.Deserialize</c> of the raw <c>rates</c> query JSON
/// (default serializer options, exactly as the host deserialized its Setting DTO) and the
/// audit-upsert (FindAsync + Update when the DTO carries an ID, Insert otherwise). The echo object
/// handed back for <c>Additional["Rates"]</c> is the deserialized <see cref="WarrantyRatesDTO"/>
/// itself — base view-and-upsert properties included — so the export response JSON keeps the shape
/// the host always served. Registered TryAddScoped by the module API extension: the repository
/// shares the request's DbContext, so the module controller's SaveChanges (AFTER the CSV file
/// write — frozen ordering) persists the upsert; a consumer registration made BEFORE the module
/// extension replaces this default (e.g. an org that keeps rates elsewhere).
/// </summary>
public class DefaultWarrantyRatesStore : IWarrantyRatesStore
{
    private readonly WarrantyRatesRepository warrantyRatesRepository;

    public DefaultWarrantyRatesStore(WarrantyRatesRepository warrantyRatesRepository)
    {
        this.warrantyRatesRepository = warrantyRatesRepository;
    }

    public async Task<WarrantyRatesDTO?> GetCurrentAsync()
    {
        // Same query the WarrantyRates admin endpoint serves (latest non-deleted row by
        // LastSaveDate); the ID rides along so the export dialog can round-trip it back into
        // PersistExportRatesAsync as an update.
        return await this.warrantyRatesRepository.GetCurrentRatesAsync();
    }

    public async Task<WarrantyRatesPersistResult> PersistExportRatesAsync(string ratesJson, long? userId)
    {
        var warrantyRates = JsonSerializer.Deserialize<WarrantyRatesDTO>(ratesJson)!;

        Entities.WarrantyRates? rates = null;

        if (!string.IsNullOrWhiteSpace(warrantyRates?.ID))
        {
            rates = await this.warrantyRatesRepository.FindAsync(warrantyRates.ID!.ToLong(), asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        }

        if (rates is not null)
            await this.warrantyRatesRepository.UpsertAsync(rates, warrantyRates!, ActionTypes.Update, userId, null, disableDefaultDataLevelAccess: false, disableGlobalFilters: false);
        else
        {
            this.warrantyRatesRepository.Add(await this.warrantyRatesRepository.UpsertAsync(new(), warrantyRates!, ActionTypes.Insert, userId, null, disableDefaultDataLevelAccess: false, disableGlobalFilters: false));
        }

        return new WarrantyRatesPersistResult
        {
            // Only the four rate fields feed the module's export math — the ID deliberately does not
            // ride along (it never did: same mapping as the dissolved repository overload).
            Rates = new WarrantyRatesDTO
            {
                PRR = warrantyRates!.PRR,
                LaborExchangeRate = warrantyRates.LaborExchangeRate,
                SubletExchangeRate = warrantyRates.SubletExchangeRate,
                PartExchangeRate = warrantyRates.PartExchangeRate,
            },
            RatesEcho = warrantyRates,
        };
    }
}
