using System;
using System.Data.Common;
using DuckDBConnection = global::DuckDB.NET.Data.DuckDBConnection;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// One family's rows in VIN order, on its own connection, one row ahead: <see cref="CurrentVin"/>
/// is the key the merge compares and <see cref="Current"/> the mapped model for it. The order is
/// DuckDB's binary order of the normalized VIN, which is the ordinal order the merge uses, then the
/// family's <see cref="AggregateFamily.RowOrder"/> within a VIN — <c>rowid</c> on a base table, the
/// order a per-VIN scan has always handed the evaluators, so lists that keep input order come out
/// the same. The relation scanned is the family's <see cref="AggregateFamily.From"/>: the bare
/// table of a read snapshot, or whatever a source binding pointed it at. A row that arrives out of
/// VIN order fails loudly rather than being silently attached to the wrong vehicle.
/// </summary>
internal sealed class VinOrderedFamilyReader : IDisposable
{
    private readonly DuckDBConnection connection;
    private readonly DbCommand command;
    private readonly DbDataReader reader;
    private readonly Func<DbDataReader, object> read;
    private readonly int vinOrdinal;

    public AggregateFamily Family { get; }
    public string CurrentVin { get; private set; }
    public object Current { get; private set; }
    public bool Exhausted { get; private set; }
    public long RowsRead { get; private set; }
    public long BlankVinRows { get; private set; }
    /// <summary>Rows whose stored VIN is not in canonical form (trimmed, upper case): never attached, see <see cref="MoveNext"/>.</summary>
    public long NonCanonicalVinRows { get; private set; }

    public VinOrderedFamilyReader(string connectionString, AggregateFamily family, string vinColumn = "VIN")
    {
        Family = family ?? throw new ArgumentNullException(nameof(family));
        connection = new DuckDBConnection(connectionString);
        connection.Open();
        command = connection.CreateCommand();
        command.CommandText =
            $"SELECT * FROM {family.From}" +
            (string.IsNullOrWhiteSpace(family.Where) ? "" : $" WHERE {family.Where}") +
            $" ORDER BY upper(trim(\"{vinColumn}\")), {family.RowOrder}";   // rowid on a base table: the physical order a per-VIN scan returns
        // A missing table, column or filter fails here, at open — declared, not discovered.
        reader = command.ExecuteReader();
        vinOrdinal = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), vinColumn, StringComparison.OrdinalIgnoreCase))
            {
                vinOrdinal = i;
                break;
            }
        }
        if (vinOrdinal < 0)
            throw new InvalidOperationException($"Family '{family.Table}' has no '{vinColumn}' column to stream by.");
        read = family.BuildReader(reader);
    }

    public bool MoveNext()
    {
        while (reader.Read())
        {
            RowsRead++;
            var stored = reader.IsDBNull(vinOrdinal) ? null : reader.GetValue(vinOrdinal)?.ToString();
            var vin = Normalize(stored);
            if (string.IsNullOrEmpty(vin))
            {
                BlankVinRows++;
                continue;
            }
            if (!string.Equals(vin, stored, StringComparison.Ordinal))
            {
                // The per-VIN storage selects a family's rows with `VIN IN (<normalized request VINs>)`
                // — the row's VIN AS STORED against the canonical form — so a row stored as
                // ' jtdbr32e0x0000001' never reaches an evaluator on that path, and Cosmos, keyed on
                // the stored value, serves it no better. The same rule here: counted, never attached
                // to the vehicle it names. (App-owned families in production data carry such rows.)
                NonCanonicalVinRows++;
                continue;
            }
            if (CurrentVin is not null && string.CompareOrdinal(vin, CurrentVin) < 0)
            {
                throw new InvalidOperationException(
                    $"Family '{Family.Table}' delivered VIN '{vin}' after '{CurrentVin}': the stream is not in ordinal VIN order.");
            }
            CurrentVin = vin;
            Current = read(reader);
            return true;
        }
        Exhausted = true;
        CurrentVin = null;
        Current = null;
        return false;
    }

    /// <summary>The canonical VIN — the per-VIN storage's own rule: every whitespace character removed, upper case.</summary>
    internal static string Normalize(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin))
            return null;
        var chars = new char[vin.Length];
        var length = 0;
        foreach (var ch in vin)
        {
            if (!char.IsWhiteSpace(ch))
                chars[length++] = ch;
        }
        return new string(chars, 0, length).ToUpperInvariant();
    }

    public void Dispose()
    {
        reader.Dispose();
        command.Dispose();
        connection.Dispose();
    }
}
