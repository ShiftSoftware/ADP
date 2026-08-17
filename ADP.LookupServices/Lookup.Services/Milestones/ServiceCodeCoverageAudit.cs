using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// Runs this deployment's milestone reader over a corpus of distinct service codes and reports what
/// it makes of them.
/// <para>
/// Offline and supported, rather than a script somebody once wrote. Per-lookup diagnostics can only
/// ever describe the vehicle in front of you, and the failure this answers is estate-wide: a
/// convention that fits a fraction of the codes in accumulated history produces no error anywhere,
/// only rewards quietly withheld from customers who earned them. Point it at the distinct package
/// codes in the labour-line store — the accumulated history eligibility actually reads, never a
/// catalog export, which holds the codes in use today and not the ones customers were served under.
/// </para>
/// </summary>
public static class ServiceCodeCoverageAudit
{
    /// <summary>
    /// Reads every code in the corpus and summarises the result.
    /// </summary>
    /// <param name="options">The milestone settings to audit — ordinarily <c>LookupOptions.ServiceMilestones</c>.</param>
    /// <param name="corpus">Distinct codes with the number of labour lines carrying each.</param>
    /// <param name="unresolvedLimit">How many unresolved codes to report, heaviest first.</param>
    public static ServiceCodeCoverageReport Run(
        ServiceMilestoneOptions options,
        IEnumerable<ServiceCodeSample> corpus,
        int unresolvedLimit = 25)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        return Run(options.GetResolver(), corpus, unresolvedLimit);
    }

    /// <summary>
    /// Audits a resolver directly, for a host that supplies its own. A resolver that does not
    /// explain its refusals is still audited — coverage and programme volumes are read from what it
    /// returns — but the unresolved codes come back without a reason.
    /// </summary>
    public static ServiceCodeCoverageReport Run(
        IServiceMilestoneResolver resolver,
        IEnumerable<ServiceCodeSample> corpus,
        int unresolvedLimit = 25)
    {
        if (resolver is null)
            throw new ArgumentNullException(nameof(resolver));

        var packageCodeReader = resolver as PackageCodeServiceMilestoneResolver;

        var report = new ServiceCodeCoverageReport
        {
            CanRead = packageCodeReader is null || packageCodeReader.CanRead,
            Problems = packageCodeReader?.Problems ?? new ServiceMilestoneConfigurationProblem[0],
        };

        var programs = new Breakdown();
        var qualifiers = new Breakdown();
        var conventions = new Breakdown();
        var unresolved = new List<UnresolvedServiceCode>();

        // Declared conventions are seeded at zero so one that matches nothing is reported in place
        // rather than absent. A convention silently contributing nothing is how a superseded shape,
        // or one shadowed by the convention above it, stays invisible.
        if (packageCodeReader != null)
            foreach (var convention in packageCodeReader.Conventions)
                conventions.Seed(convention);

        foreach (var sample in corpus ?? Enumerable.Empty<ServiceCodeSample>())
        {
            if (sample is null)
                continue;

            var lines = sample.Lines;
            report.Codes++;
            report.Lines += lines;

            if (packageCodeReader is null)
            {
                var reading = resolver.Resolve(sample.Code);

                if (reading is null)
                {
                    unresolved.Add(new UnresolvedServiceCode { Code = sample.Code, Lines = lines });
                    continue;
                }

                Count(report, programs, qualifiers, reading, lines);
                continue;
            }

            var read = packageCodeReader.Read(sample.Code);

            if (read.Outcome != ServiceCodeReadOutcome.Read)
            {
                unresolved.Add(new UnresolvedServiceCode
                {
                    Code = sample.Code,
                    Lines = lines,
                    Reason = read.Outcome,
                    Convention = read.Convention,
                    MilestoneInKilometres = read.MilestoneInKilometres,
                });
                continue;
            }

            conventions.Add(read.Convention, lines);
            Count(report, programs, qualifiers, read.Reading, lines);
        }

        report.LineCoverage = report.Lines == 0 ? 0d : (double)report.ResolvedLines / report.Lines;
        report.Programs = programs.ByVolume();
        report.Qualifiers = qualifiers.ByVolume();
        report.Conventions = conventions.AsDeclared();
        report.TopUnresolved = unresolved
            .OrderByDescending(code => code.Lines)
            .ThenBy(code => code.Code, StringComparer.OrdinalIgnoreCase)
            .Take(unresolvedLimit < 0 ? 0 : unresolvedLimit)
            .ToList();

        return report;
    }

    private static void Count(
        ServiceCodeCoverageReport report,
        Breakdown programs,
        Breakdown qualifiers,
        ServiceMilestoneReading reading,
        long lines)
    {
        report.ResolvedCodes++;
        report.ResolvedLines += lines;
        programs.Add(reading.Program, lines);
        qualifiers.Add(reading.Qualifier, lines);
    }

    /// <summary>
    /// Volume by one facet of a reading. Keys are compared case-insensitively, matching how the
    /// conditions compare them, so two spellings of one programme are one row here rather than two
    /// that each look too small.
    /// </summary>
    private sealed class Breakdown
    {
        // Preserves the order rows were first seen, which is the declared order for conventions.
        private readonly List<ServiceCodeCoverageGroup> order = new List<ServiceCodeCoverageGroup>();
        private readonly Dictionary<string, ServiceCodeCoverageGroup> named =
            new Dictionary<string, ServiceCodeCoverageGroup>(StringComparer.OrdinalIgnoreCase);

        // Held apart rather than under a sentinel key, because any sentinel would be a name a
        // convention could legitimately be given.
        private ServiceCodeCoverageGroup unnamed;

        internal void Seed(string name) => Row(name);

        internal void Add(string name, long lines)
        {
            var row = Row(name);
            row.Codes++;
            row.Lines += lines;
        }

        internal IReadOnlyList<ServiceCodeCoverageGroup> ByVolume() =>
            order
                .OrderByDescending(row => row.Lines)
                .ThenByDescending(row => row.Codes)
                .ToList();

        internal IReadOnlyList<ServiceCodeCoverageGroup> AsDeclared() => order.ToList();

        private ServiceCodeCoverageGroup Row(string name)
        {
            if (name is null)
            {
                if (unnamed is null)
                {
                    unnamed = new ServiceCodeCoverageGroup();
                    order.Add(unnamed);
                }

                return unnamed;
            }

            if (!named.TryGetValue(name, out var row))
            {
                row = new ServiceCodeCoverageGroup { Name = name };
                named.Add(name, row);
                order.Add(row);
            }

            return row;
        }
    }
}
