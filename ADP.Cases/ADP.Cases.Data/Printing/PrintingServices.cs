using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Cases.Shared.Printing;

namespace ShiftSoftware.ADP.Cases.Data.Printing;

/// <summary>
/// Resolves the consumer-registered printing seams from the application service provider behind a
/// <see cref="DbContext"/> (the same provider ShiftEntity's repositories use for their own service
/// lookups). Print methods resolve lazily — at print time, from the live scope — instead of taking
/// constructor dependencies, so a consumer that never prints needs no registrations and a derived
/// repository can never accidentally sever the seams by not forwarding constructor parameters.
/// </summary>
public static class PrintingServices
{
    /// <summary>
    /// The consumer's <see cref="ICompanyInfoProvider"/>. There is deliberately no module default —
    /// printing is impossible without real company identity — so a missing registration throws with
    /// registration guidance.
    /// </summary>
    public static ICompanyInfoProvider GetRequiredCompanyInfoProvider(DbContext db)
        => GetService<ICompanyInfoProvider>(db)
            ?? throw new InvalidOperationException(
                "Printing requires an ShiftSoftware.ADP.Cases.Shared.Printing.ICompanyInfoProvider registration. " +
                "Register one (services.AddScoped<ICompanyInfoProvider, YourCompanyInfoProvider>()) that supplies " +
                "the distributor/manufacturer/dealer/branch info your printouts render.");

    /// <summary>
    /// The consumer's <see cref="IPrintoutDateFormatter"/>, falling back to
    /// <see cref="DefaultPrintoutDateFormatter"/> (the module API extensions TryAdd it, so the
    /// fallback only fires for hosts wired without the module extension).
    /// </summary>
    public static IPrintoutDateFormatter GetPrintoutDateFormatter(DbContext db)
        => GetService<IPrintoutDateFormatter>(db) ?? new DefaultPrintoutDateFormatter();

    /// <summary>
    /// Resolves <typeparamref name="TService"/> from the application service provider behind
    /// <paramref name="db"/>; throws <see cref="InvalidOperationException"/> with the given
    /// message when unavailable.
    /// </summary>
    public static TService GetRequiredService<TService>(DbContext db, string missingRegistrationMessage)
        where TService : class
        => GetService<TService>(db) ?? throw new InvalidOperationException(missingRegistrationMessage);

    /// <summary>
    /// Resolves <typeparamref name="TService"/> from the application service provider behind
    /// <paramref name="db"/> (null when the service — or the application provider itself — is absent).
    /// </summary>
    public static TService? GetService<TService>(DbContext db)
        where TService : class
        => db.GetService<IDbContextOptions>()
            .Extensions.OfType<CoreOptionsExtension>()
            .FirstOrDefault()
            ?.ApplicationServiceProvider
            ?.GetService<TService>();
}
