using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Menus.Sample.Web.DTOs.DevTools;

/// <summary>
/// Outcome of a development-only schema copy. Lives in the Web project because the sample API
/// project-references the Web project (not the other way round) — the same arrangement the Todo DTOs use.
/// </summary>
public class DevDataImportResultDTO
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<DevDataImportTableResultDTO> Tables { get; set; } = new();

    /// <summary>Non-fatal problems: skipped tables, constraints that could not be re-verified, etc.</summary>
    public List<string> Warnings { get; set; } = new();

    public long ElapsedMilliseconds { get; set; }

    public int TotalRowsCopied => Tables.Sum(x => x.RowsCopied);

    public int TotalRowsDeleted => Tables.Sum(x => x.RowsDeleted);
}

/// <summary>Which copy sources a developer has configured locally — drives which buttons are enabled.</summary>
public class DevDataImportAvailabilityDTO
{
    public bool IdentityConfigured { get; set; }

    public bool MenuConfigured { get; set; }

    public string ConfigurationHint { get; set; } = string.Empty;
}

public class DevDataImportTableResultDTO
{
    public string Table { get; set; } = string.Empty;

    public int RowsDeleted { get; set; }

    public int RowsCopied { get; set; }

    /// <summary>Set when the table needed special handling (preserved rows, system versioning, …).</summary>
    public string? Note { get; set; }
}
