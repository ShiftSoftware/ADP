using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
}
