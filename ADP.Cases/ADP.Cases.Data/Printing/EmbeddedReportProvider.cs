using System.Collections.Concurrent;
using System.Reflection;

namespace ShiftSoftware.ADP.Cases.Data.Printing;

/// <summary>
/// Extracts module-embedded FastReport templates (.frx <c>EmbeddedResource</c>) to per-process temp
/// files and returns the file path. Needed because ShiftEntity.Print's <c>FastReportBuilder</c> only
/// accepts a file path today (<c>AddFastReportFile</c>) — a stream overload on ShiftEntity.Print is a
/// recorded follow-up. Deliberately FastReport-free: this class only touches resources and files.
/// </summary>
public static class EmbeddedReportProvider
{
    private static readonly ConcurrentDictionary<string, string> cachedPaths = new();
    private static readonly object extractionLock = new();

    /// <summary>
    /// Returns a readable file path for the given embedded resource, extracting it once per process
    /// (re-extracting if an external temp cleaner removed the file). Thread-safe.
    /// </summary>
    /// <param name="assembly">The assembly carrying the embedded resource.</param>
    /// <param name="resourceName">The full manifest resource name
    /// (e.g. <c>ShiftSoftware.ADP.WarrantyClaims.Data.Reports.WarrantyClaim.frx</c>).</param>
    public static string GetReportPath(Assembly assembly, string resourceName)
    {
        var path = cachedPaths.GetOrAdd(
            $"{assembly.GetName().Name}|{resourceName}",
            _ => Path.Combine(
                Path.GetTempPath(),
                "adp-embedded-reports",
                // Per-process folder: concurrent app instances never contend for the same file.
                Environment.ProcessId.ToString(),
                resourceName));

        if (!File.Exists(path))
        {
            lock (extractionLock)
            {
                if (!File.Exists(path))
                {
                    using var resource = assembly.GetManifestResourceStream(resourceName)
                        ?? throw new InvalidOperationException(
                            $"Embedded report resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");

                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    using var file = File.Create(path);

                    resource.CopyTo(file);
                }
            }
        }

        return path;
    }
}
