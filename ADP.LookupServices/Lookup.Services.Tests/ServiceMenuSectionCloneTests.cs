using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using System.Reflection;

namespace Lookup.Services.Tests;

/// <summary>
/// The drift guard for <c>VehicleLookupService</c>'s hand-written service-menu clone.
/// <para>
/// A bulk lookup evaluates one menu section per model and hands every later vehicle of that model a
/// CLONE — copied property by property, because the DTOs have no clone of their own. A property added
/// to <see cref="VehicleServiceMenuDTO"/>, <see cref="VehicleServiceMenuLineDTO"/> or
/// <see cref="VehicleServiceMenuPartDTO"/> but not to the clone would silently vanish for the
/// 2nd..Nth vehicle of each model — a bug only visible as bulk answers differing from single answers.
/// This test populates every public settable property with a non-default value by reflection and
/// asserts the clone carries each one, so forgetting the clone fails the build instead of the data.
/// </para>
/// </summary>
public class ServiceMenuSectionCloneTests
{
    [Fact]
    public void TheClone_CarriesEveryProperty_OfTheSectionItsLinesAndTheirParts()
    {
        var section = new VehicleServiceMenuDTO();
        PopulateEveryProperty(section, seed: 3);

        var line = new VehicleServiceMenuLineDTO();
        PopulateEveryProperty(line, seed: 17);

        var part = new VehicleServiceMenuPartDTO();
        PopulateEveryProperty(part, seed: 41);

        line.Parts = new List<VehicleServiceMenuPartDTO> { part };
        section.Services = new List<VehicleServiceMenuLineDTO> { line };

        var clone = InvokeClone(section);

        Assert.NotSame(section, clone);
        AssertEveryPropertyEqual(section, clone);

        var clonedLine = Assert.Single(clone.Services);
        Assert.NotSame(line, clonedLine);
        AssertEveryPropertyEqual(line, clonedLine, except: nameof(VehicleServiceMenuLineDTO.Parts));

        var clonedPart = Assert.Single(clonedLine.Parts);
        Assert.NotSame(part, clonedPart);
        AssertEveryPropertyEqual(part, clonedPart);
    }

    [Fact]
    public void ANullSection_ClonesToNull()
    {
        Assert.Null(InvokeClone(null));
    }

    private static VehicleServiceMenuDTO InvokeClone(VehicleServiceMenuDTO section)
    {
        var method = typeof(VehicleLookupService).GetMethod(
            "CloneServiceMenuSection", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (VehicleServiceMenuDTO)method!.Invoke(null, new object[] { section })!;
    }

    /// <summary>
    /// A distinct non-default value per property, derived from the seed and the property's position,
    /// so a clone that swaps two same-typed properties fails too.
    /// </summary>
    private static void PopulateEveryProperty(object instance, int seed)
    {
        var properties = SettableProperties(instance.GetType()).ToList();

        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var value = MakeValue(property.PropertyType, seed + i + 1);

            if (value is not null)
                property.SetValue(instance, value);
        }
    }

    private static object MakeValue(Type type, int seed)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string)) return $"value-{seed}";
        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return seed;
        if (type == typeof(long)) return (long)seed * 1000;
        if (type == typeof(decimal)) return seed + 0.25m;
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1); // last member: never the default(0) of a fresh instance
        }

        // Collections (Services, Parts) are wired explicitly by the test; anything genuinely new and
        // unconstructible here should extend MakeValue rather than be skipped.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return null;

        throw new InvalidOperationException(
            $"No test value for {type.Name} — extend MakeValue so the clone drift guard keeps covering every property.");
    }

    private static void AssertEveryPropertyEqual(object expected, object actual, string except = null)
    {
        foreach (var property in SettableProperties(expected.GetType()))
        {
            if (property.Name == except || IsList(property.PropertyType))
                continue;

            Assert.True(
                Equals(property.GetValue(expected), property.GetValue(actual)),
                $"{expected.GetType().Name}.{property.Name} was not carried by the clone: " +
                $"expected '{property.GetValue(expected)}', got '{property.GetValue(actual)}'.");
        }
    }

    private static IEnumerable<PropertyInfo> SettableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

    private static bool IsList(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
}
