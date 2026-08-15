using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// The DuckDB implementation of <see cref="IServiceMenuLookupStorageService"/> — reads the menu tables
/// laid out per <see cref="DuckDBServiceMenuSchema"/>. Populating them is the menu sync's job (a
/// separate concern, not implemented yet — see <see cref="DuckDBServiceMenuSyncService"/>); the reader
/// only fixes the contract those tables must meet, so the documents round-trip storage-identically.
///
/// <para><b>One <c>IN</c>-clause query per table for a whole set of codes.</b> The single-code read is
/// the bulk read of a one-element set; the bulk read fetches every requested model in four queries per
/// 500-code chunk — the same batching shape as <c>DuckDBVehicleLookupStorageService</c>'s aggregate
/// fetch, which is what makes bulk lookups cheap on this backend.</para>
///
/// <para><b>A per-code cache, scoped to this instance's lifetime.</b> The Cosmos reader deliberately
/// has no cache (staleness), but hosts point DuckDB readers at published snapshot files and register
/// them scoped — the vehicle DuckDB service caches on the same reasoning — and the cache is what makes
/// the vehicle lookup's bulk flow cheap here: many VINs share few basic model codes, and every repeat
/// is a dictionary hit instead of four queries. Two caveats callers own: cached
/// <see cref="ServiceMenuDocuments"/> instances are SHARED between calls, so treat them as read-only
/// (the vehicle DuckDB caches share instances the same way); and a host that syncs into the same
/// database this instance is reading should use a fresh scope (a fresh reader) afterwards.</para>
///
/// <para><b>Faults are translated, not swallowed, and never invent an empty menu.</b> Missing menu
/// tables throw <see cref="ServiceMenuContainerNotFoundException"/> (this store was never synced —
/// the DuckDB analogue of the unprovisioned container); any other storage failure throws
/// <see cref="ServiceMenuStorageException"/>, which the vehicle lookup's menu section contains exactly
/// like a <c>CosmosException</c>. Returning empty results on error would tell every caller "this model
/// has no menu", permanently and invisibly.</para>
/// </summary>
public class DuckDBServiceMenuLookupStorageService : IServiceMenuLookupStorageService, IDisposable
{
    private const int CodeChunkSize = 500;

    private readonly global::DuckDB.NET.Data.DuckDBConnection connection;
    private readonly bool ownsConnection;

    private readonly ConcurrentDictionary<string, ServiceMenuDocuments> menuCache = new(StringComparer.Ordinal);
    private bool tablesVerified;

    /// <summary>
    /// Over a connection the HOST owns: the host opens it, shares it (typically with the vehicle
    /// DuckDB services), and disposes it. This reader never closes it.
    /// </summary>
    public DuckDBServiceMenuLookupStorageService(global::DuckDB.NET.Data.DuckDBConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>
    /// Over the reader's OWN connection, opened here from a connection string and disposed with the
    /// reader. For hosts that keep no shared <c>DuckDBConnection</c> registration — each instance
    /// (each scope, under DI) opens the file itself, so point the string at a published read snapshot
    /// and prefer <c>access_mode=read_only</c>.
    /// </summary>
    public DuckDBServiceMenuLookupStorageService(string connectionString)
    {
        connection = new global::DuckDB.NET.Data.DuckDBConnection(connectionString);
        connection.Open();
        ownsConnection = true;
    }

    public void Dispose()
    {
        if (ownsConnection)
            connection.Dispose();
    }

    public async Task<ServiceMenuDocuments> GetMenuDocumentsAsync(string basicModelCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(basicModelCode))
            return new ServiceMenuDocuments { BasicModelCode = basicModelCode };

        var results = await GetMenuDocumentsAsync(new[] { basicModelCode }, cancellationToken);

        return results.Count > 0
            ? results[0]
            : new ServiceMenuDocuments { BasicModelCode = basicModelCode.Trim() };
    }

    public async Task<IReadOnlyList<ServiceMenuDocuments>> GetMenuDocumentsAsync(IEnumerable<string> basicModelCodes, CancellationToken cancellationToken = default)
    {
        var codes = ServiceMenuCosmosService.NormalizeCodes(basicModelCodes);

        if (codes.Count == 0)
            return new List<ServiceMenuDocuments>();

        var missing = codes.Where(code => !menuCache.ContainsKey(code)).ToList();

        if (missing.Count > 0)
        {
            EnsureMenuTablesExist();

            foreach (var chunk in Chunk(missing, CodeChunkSize))
            {
                // Every chunk code gets an entry up front, so a code with no rows caches as an EMPTY
                // document set — "no menu" is an answer worth caching too, or bulk vehicle lookups
                // would re-query the same missing model for every VIN that maps to it.
                var sets = chunk.ToDictionary(
                    code => code,
                    code => new ServiceMenuDocuments { BasicModelCode = code },
                    StringComparer.Ordinal);

                var inClause = BuildInClause(chunk);

                Assign(await QueryAsync<MenuVariantCosmosModel>(SelectByCodes(ModelTypes.MenuVariant, inClause), cancellationToken),
                    sets, document => document.BasicModelCode, (documents, items) => documents.Variants = items);
                Assign(await QueryAsync<MenuPeriodCosmosModel>(SelectByCodes(ModelTypes.MenuPeriod, inClause), cancellationToken),
                    sets, document => document.BasicModelCode, (documents, items) => documents.Periods = items);
                Assign(await QueryAsync<MenuLabourCosmosModel>(SelectByCodes(ModelTypes.MenuLabour, inClause), cancellationToken),
                    sets, document => document.BasicModelCode, (documents, items) => documents.Labours = items);
                Assign(await QueryAsync<MenuItemCosmosModel>(SelectByCodes(ModelTypes.MenuItem, inClause), cancellationToken),
                    sets, document => document.BasicModelCode, (documents, items) => documents.Items = items);

                foreach (var entry in sets)
                    menuCache[entry.Key] = entry.Value;
            }
        }

        return codes.Select(code => menuCache[code]).ToList();
    }

    private static string SelectByCodes(string tableName, string inClause) =>
        $"SELECT * FROM {DuckDBServiceMenuSchema.QuoteIdentifier(tableName)} WHERE BasicModelCode IN ({inClause})";

    /// <summary>
    /// The missing-table check, done ONCE per instance against information_schema rather than by
    /// sniffing error messages: a store that was never synced must fail loudly as "not provisioned",
    /// not per-query and not as an empty menu.
    /// </summary>
    private void EnsureMenuTablesExist()
    {
        if (tablesVerified)
            return;

        List<string> existing;

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT table_name FROM information_schema.tables";

            existing = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                existing.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            throw new ServiceMenuStorageException(
                "The DuckDB menu store could not be inspected (querying information_schema failed). " +
                "The connection may be closed, or the file unreadable.",
                ex);
        }

        var missing = DuckDBServiceMenuSchema.Tables
            .Select(table => table.TableName)
            .Where(name => !existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count > 0)
            throw new ServiceMenuContainerNotFoundException(
                connection.Database,
                string.Join(", ", missing),
                $"The DuckDB menu table(s) '{string.Join(", ", missing)}' do not exist in this database. " +
                "Service-menu lookups over DuckDB read tables laid out per DuckDBServiceMenuSchema, so this " +
                "store has never been populated with menu data (or the connection points at the wrong file).",
                null);

        tablesVerified = true;
    }

    private static void Assign<T>(
        List<T> items,
        Dictionary<string, ServiceMenuDocuments> sets,
        Func<T, string> codeSelector,
        Action<ServiceMenuDocuments, List<T>> assigner)
    {
        foreach (var group in items.GroupBy(item => codeSelector(item)?.Trim() ?? string.Empty, StringComparer.Ordinal))
        {
            if (sets.TryGetValue(group.Key, out var documents))
                assigner(documents, group.ToList());
        }
    }

    private static IEnumerable<List<string>> Chunk(IReadOnlyList<string> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
            yield return items.Skip(i).Take(size).ToList();
    }

    private static string BuildInClause(IEnumerable<string> codes) =>
        string.Join(",", codes.Select(code => $"'{DuckDBServiceMenuSchema.EscapeLiteral(code)}'"));

    /// <summary>
    /// Reads rows onto the Cosmos model: native columns by name (case-insensitive), JSON columns
    /// deserialized back to the embedded shapes. Unlike the vehicle reader this does NOT swallow —
    /// a row that cannot materialize means the store and the reader disagree about the schema, and a
    /// menu quietly missing its parts is a wrong price, not a degraded one.
    /// </summary>
    private async Task<List<T>> QueryAsync<T>(string sql, CancellationToken cancellationToken) where T : new()
    {
        try
        {
            var results = new List<T>();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columnOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                columnOrdinals[reader.GetName(i)] = i;

            var properties = DuckDBServiceMenuSchema.GetColumns(typeof(T));

            while (await reader.ReadAsync(cancellationToken))
            {
                var model = new T();

                foreach (var property in properties)
                {
                    if (!columnOrdinals.TryGetValue(property.Name, out var ordinal) || reader.IsDBNull(ordinal))
                        continue;

                    property.SetValue(model, ReadValue(reader, ordinal, property));
                }

                results.Add(model);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ServiceMenuStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ServiceMenuStorageException(
                $"The DuckDB menu store could not be read (query failed or a row did not match the " +
                $"expected schema): {sql}",
                ex);
        }
    }

    private static object ReadValue(DbDataReader reader, int ordinal, PropertyInfo property)
    {
        var targetType = property.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string))
            return reader.GetValue(ordinal)?.ToString();

        if (underlyingType == typeof(bool))
            return reader.GetBoolean(ordinal);

        if (underlyingType == typeof(int))
            return Convert.ToInt32(reader.GetValue(ordinal));

        if (underlyingType == typeof(long))
            return Convert.ToInt64(reader.GetValue(ordinal));

        if (underlyingType == typeof(decimal))
            return Convert.ToDecimal(reader.GetValue(ordinal));

        if (underlyingType == typeof(double))
            return Convert.ToDouble(reader.GetValue(ordinal));

        if (underlyingType == typeof(DateTime))
            return reader.GetDateTime(ordinal);

        if (underlyingType == typeof(DateTimeOffset))
        {
            var value = reader.GetValue(ordinal);
            return value is DateTimeOffset offset ? offset : new DateTimeOffset(Convert.ToDateTime(value), TimeSpan.Zero);
        }

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, Convert.ToInt32(reader.GetValue(ordinal)));

        if (DuckDBServiceMenuSchema.IsJsonColumn(targetType))
        {
            // Newtonsoft, NOT System.Text.Json, so the embedded shapes materialize under exactly the
            // serializer semantics the Cosmos reader's JObject.ToObject applies (case-insensitive
            // member matching, the same lenient conversions). The same stored document must not
            // deserialize differently per backend.
            var json = reader.GetString(ordinal);
            return string.IsNullOrWhiteSpace(json) ? null : Newtonsoft.Json.JsonConvert.DeserializeObject(json, targetType);
        }

        return reader.GetValue(ordinal);
    }
}
