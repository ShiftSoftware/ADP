using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent;

namespace ADP.SyncAgent.Tests;

/// <summary>A destination model with the shapes that bit production: a JSON column mid-model and trailing columns.</summary>
public class VehicleRow
{
    public string id { get; set; } = default!;
    public string? VIN { get; set; }
    public List<LaborLine>? Labors { get; set; }   // JSON, mid-model (the SSCAffectedVIN incident)
    public string? LaborCode1 { get; set; }
    public double? LaborHour1 { get; set; }
    public decimal? Price { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public Intermediary? Intermediary { get; set; } // JSON, trailing (the VehicleEntry incident)
    public string? Distributor { get; set; }
}

public class LaborLine
{
    public string? LaborCode { get; set; }
    public double? LaborHour { get; set; }
}

public class Intermediary
{
    public string? InvoiceNumber { get; set; }
}

/// <summary>Collects every log line the engine and adapters emit, so tests can assert on what was said.</summary>
public sealed class CapturingLogger : ISyncEngineLogger
{
    public List<(string Level, string Message, Exception? Exception)> Lines { get; } = [];

    public IEnumerable<SyncEngineLoggerStatus> SyncTaskStatuses { get; } = [];
    public SyncEngineLoggerStatus? CurrentSyncTaskStatus { get; private set; }
    public string ID { get; } = Guid.NewGuid().ToString("N");
    public string? SyncID => null;
    public long? OperationTimeoutInSeconds { get; private set; }
    public DateTime OperationStart { get; private set; }

    public IEnumerable<string> Errors => Lines.Where(x => x.Level == "Error").Select(x => x.Message);
    public IEnumerable<string> Warnings => Lines.Where(x => x.Level == "Warning").Select(x => x.Message);

    public ISyncEngineLogger SetOperationTimeoutInSeconds(long? seconds) { OperationTimeoutInSeconds = seconds; return this; }
    public ISyncEngineLogger SetOperationStart(DateTime startDate) { OperationStart = startDate; return this; }
    public ValueTask<ISyncEngineLogger> SetSyncTaskStatus(SyncEngineLoggerStatus syncTaskStatus) { CurrentSyncTaskStatus = syncTaskStatus; return new(this); }
    public ValueTask FailAllRunningTasks() => default;
    public ValueTask CompleteAllRunningTasks() => default;

    public ValueTask LogInformation(string? message, params object?[] args) => Add("Information", message, null, args);
    public ValueTask LogError(string? message, params object?[] args) => Add("Error", message, null, args);
    public ValueTask LogError(Exception? exception, string? message, params object?[] args) => Add("Error", message, exception, args);
    public ValueTask LogWarning(string? message, params object?[] args) => Add("Warning", message, null, args);

    private ValueTask Add(string level, string? message, Exception? exception, object?[] args)
    {
        var text = message ?? string.Empty;
        try { text = string.Format(text, args); } catch (FormatException) { /* keep raw */ }
        Lines.Add((level, text, exception));
        return default;
    }
}

public static class Db
{
    public static DuckDBConnection Open(string path)
    {
        var conn = new DuckDBConnection($"DataSource={path}");
        conn.Open();
        return conn;
    }

    public static int Exec(DuckDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(DuckDBConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var value = cmd.ExecuteScalar();
        return value is DBNull ? null : value;
    }

    public static List<string> Columns(DuckDBConnection conn, string table)
    {
        var list = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT column_name FROM duckdb_columns() WHERE database_name = current_database() AND table_name = '{table}' ORDER BY column_index";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(reader.GetString(0));
        return list;
    }
}

public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "adp-syncagent-tests", Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
    }
}
