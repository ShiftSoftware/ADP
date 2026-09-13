using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>Loads check definitions from YAML (camelCase keys; unknown keys ignored).</summary>
public static class YamlCheckLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static List<CheckDefinition> Load(string yamlText)
        => Deserializer.Deserialize<List<CheckDefinition>>(yamlText) ?? [];

    public static List<CheckDefinition> LoadFile(string path)
        => Load(File.ReadAllText(path));

    /// <summary>
    /// Loads with the same tolerant runtime deserializer, then adds strict structural and semantic
    /// diagnostics for authoring. Unknown properties are reported here without changing existing hosts'
    /// runtime behaviour.
    /// </summary>
    public static AuthoringCheckPack LoadForAuthoring(string yamlText, IEnumerable<string>? registeredSources = null)
    {
        List<CheckDefinition> checks;
        try
        {
            checks = Load(yamlText);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            return new([], [new CheckDiagnostic(
                CheckDiagnosticSeverity.Error,
                "yaml_parse_error",
                "checks",
                "The YAML could not be parsed. Fix the syntax or field type at the reported location.",
                checked((int)ex.Start.Line + 1),
                checked((int)ex.Start.Column + 1))]);
        }
        catch (Exception ex)
        {
            return new([], [new CheckDiagnostic(
                CheckDiagnosticSeverity.Error,
                "yaml_parse_error",
                "checks",
                $"The YAML could not be loaded ({ex.GetType().Name}).")]);
        }

        var diagnostics = InspectProperties(yamlText);
        diagnostics.AddRange(CheckValidator.Validate(checks, registeredSources));
        return new(checks, diagnostics);
    }

    public static AuthoringCheckPack LoadFileForAuthoring(string path, IEnumerable<string>? registeredSources = null)
        => LoadForAuthoring(File.ReadAllText(path), registeredSources);

    private static List<CheckDiagnostic> InspectProperties(string yamlText)
    {
        var diagnostics = new List<CheckDiagnostic>();
        try
        {
            var stream = new YamlStream();
            using var reader = new StringReader(yamlText);
            stream.Load(reader);

            if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is not YamlSequenceNode sequence)
            {
                diagnostics.Add(new(CheckDiagnosticSeverity.Error, "invalid_root", "checks", "The YAML root must be a list of checks."));
                return diagnostics;
            }

            for (var i = 0; i < sequence.Children.Count; i++)
            {
                if (sequence.Children[i] is not YamlMappingNode check)
                {
                    AddShape(diagnostics, sequence.Children[i], "invalid_check", $"checks[{i}]", "Each check must be a mapping.");
                    continue;
                }

                InspectMap(check, CheckLanguage.CheckProperties, $"checks[{i}]", diagnostics);
                if (Child(check, "measures") is YamlSequenceNode measures)
                {
                    for (var m = 0; m < measures.Children.Count; m++)
                    {
                        if (measures.Children[m] is YamlMappingNode measure)
                            InspectMap(measure, CheckLanguage.MeasureProperties, $"checks[{i}].measures[{m}]", diagnostics);
                        else
                            AddShape(diagnostics, measures.Children[m], "invalid_measure", $"checks[{i}].measures[{m}]", "Each measure must be a mapping.");
                    }
                }

                if (Child(check, "assert") is YamlMappingNode assertion)
                    InspectMap(assertion, CheckLanguage.AssertProperties, $"checks[{i}].assert", diagnostics);
            }
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            diagnostics.Add(new(CheckDiagnosticSeverity.Error, "yaml_structure_error", "checks",
                "The YAML structure could not be inspected. Fix the syntax at the reported location.",
                checked((int)ex.Start.Line + 1), checked((int)ex.Start.Column + 1)));
        }
        catch (Exception ex)
        {
            diagnostics.Add(new(CheckDiagnosticSeverity.Error, "yaml_structure_error", "checks",
                $"The YAML structure could not be inspected ({ex.GetType().Name})."));
        }
        return diagnostics;
    }

    private static void InspectMap(
        YamlMappingNode mapping,
        IReadOnlySet<string> known,
        string path,
        List<CheckDiagnostic> diagnostics)
    {
        foreach (var (keyNode, _) in mapping.Children)
        {
            if (keyNode is not YamlScalarNode key || string.IsNullOrWhiteSpace(key.Value)) continue;
            if (known.Contains(key.Value)) continue;

            var suggestion = known
                .Select(candidate => (candidate, distance: EditDistance(key.Value, candidate)))
                .OrderBy(x => x.distance)
                .FirstOrDefault();
            var hint = suggestion.distance <= 3 ? $" Did you mean '{suggestion.candidate}'?" : "";
            AddShape(diagnostics, key, "unknown_property", $"{path}.{key.Value}", $"Unknown property '{key.Value}'.{hint}");
        }
    }

    private static YamlNode? Child(YamlMappingNode mapping, string name)
    {
        foreach (var (key, value) in mapping.Children)
            if (key is YamlScalarNode scalar && string.Equals(scalar.Value, name, StringComparison.OrdinalIgnoreCase))
                return value;
        return null;
    }

    private static void AddShape(List<CheckDiagnostic> diagnostics, YamlNode node, string code, string path, string message) =>
        diagnostics.Add(new(CheckDiagnosticSeverity.Error, code, path, message,
            checked((int)node.Start.Line + 1), checked((int)node.Start.Column + 1)));

    private static int EditDistance(string left, string right)
    {
        var costs = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var previous = costs[0];
            costs[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var saved = costs[j];
                costs[j] = Math.Min(Math.Min(costs[j] + 1, costs[j - 1] + 1), previous +
                    (char.ToLowerInvariant(left[i - 1]) == char.ToLowerInvariant(right[j - 1]) ? 0 : 1));
                previous = saved;
            }
        }
        return costs[^1];
    }
}
