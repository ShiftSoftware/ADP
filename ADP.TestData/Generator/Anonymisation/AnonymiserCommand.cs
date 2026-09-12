namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// The generator's <c>--anonymise</c> mode. Arguments:
/// <list type="bullet">
/// <item><c>--anonymise=&lt;raw environment json&gt;</c> — the private input, a <see cref="GeneratorEnvironment"/> with real identifiers.</item>
/// <item><c>--seed=&lt;text&gt;</c> or <c>--seed-file=&lt;path&gt;</c> — the private seed every derivation is keyed under (16 characters or more).</item>
/// <item><c>--keys=&lt;path&gt;</c> — where to write the real → synthetic key map (private; never inside this repository).</item>
/// <item><c>--vocabulary=&lt;path&gt;</c> — optional; the private free-text word list (<see cref="Vocabulary"/>).</item>
/// <item><c>--name=&lt;environment&gt;</c> — optional; the output name, default the input's file stem without a <c>.raw</c> suffix.</item>
/// <item><c>--to=&lt;dir&gt;</c> — optional; the output directory, default <c>ADP.TestData/environments</c>.</item>
/// </list>
/// The console reports counts and paths only — never a real value.
/// </summary>
public static class AnonymiserCommand
{
    public static int Run(IReadOnlyDictionary<string, string> arguments, string repoRoot)
    {
        var rawPath = Path.GetFullPath(arguments.GetValueOrDefault("--anonymise") ?? arguments["--anonymize"]);
        if (!File.Exists(rawPath))
            throw new FileNotFoundException("The raw environment file was not found.", rawPath);

        var seed = arguments.TryGetValue("--seed", out var inlineSeed) ? inlineSeed
            : arguments.TryGetValue("--seed-file", out var seedFile) ? File.ReadAllText(seedFile)
            : throw new ArgumentException("--anonymise needs --seed=<text> or --seed-file=<path>.");

        if (!arguments.TryGetValue("--keys", out var keysPath))
            throw new ArgumentException("--anonymise needs --keys=<path>: the key map must be written somewhere private.");

        var name = arguments.GetValueOrDefault("--name")
            ?? Path.GetFileNameWithoutExtension(rawPath).Replace(".raw", string.Empty, StringComparison.OrdinalIgnoreCase);
        var outputDir = arguments.TryGetValue("--to", out var to) ? Path.GetFullPath(to) : Path.Combine(repoRoot, "ADP.TestData", "environments");
        var outputPath = Path.Combine(outputDir, name + ".json");

        var vocabulary = Vocabulary.Load(arguments.GetValueOrDefault("--vocabulary"));
        var keyed = new Keyed(seed);

        Console.WriteLine($"Anonymising '{Path.GetFileName(rawPath)}' as environment '{name}' (seed {keyed.Fingerprint}, {vocabulary.Terms.Count} vocabulary terms)");

        var result = new EnvironmentAnonymiser(keyed, name, vocabulary).Anonymise(rawPath, outputPath, Path.GetFullPath(keysPath));

        Console.WriteLine($"  families: " + string.Join(", ", result.FamilyCounts.Where(f => f.Value > 0).Select(f => $"{f.Key} {f.Value}")));
        Console.WriteLine($"  derived values (names, phones, model codes, ids): {result.DerivedCount}");
        Console.WriteLine($"  minted unauthorized VIN: {result.MintedVin}");

        if (result.DefaultedFields.Count > 0)
        {
            Console.WriteLine("  string fields the schema walk does not name (free-text vocabulary applied; check none is an identifier):");
            foreach (var (path, count) in result.DefaultedFields)
                Console.WriteLine($"    {path} ×{count}");
        }

        Console.WriteLine($"  environment written to: {outputPath}");
        Console.WriteLine($"  key map written to:     {Path.GetFullPath(keysPath)}");

        return 0;
    }
}
