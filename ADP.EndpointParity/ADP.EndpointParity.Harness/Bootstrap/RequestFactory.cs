using System.Reflection;
using System.Text.Json.Nodes;
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here AND CANNOT: this file must reflect
// over the group's DTO types and must recognise JsonHashIdConverterAttribute to know which
// members need a real hash id rather than a string sentinel. Reviewed by reading its diff.
// ============================================================================================

/// <summary>
/// Builds write-path request bodies: a hand-authored minimal-valid body, plus a sentinel overlay
/// on every writable member that body does not need.
///
/// <para>
/// <b>Why two layers, and why neither alone works.</b> A request body containing only fields a
/// client would legitimately send cannot detect trap 3-write: an ignored member is invisible
/// unless you send it. So the body must carry sentinels in members no client would set.
/// </para>
///
/// <para>
/// <b>But a body that is ENTIRELY sentinel never reaches the mapper.</b> The canonical
/// trap-3-write instance in this repo is <c>MenuRepository.cs:39</c>
/// (<c>.IgnoreEntity(e =&gt; e.BrandID)</c>), whose derivation lives in <c>UpsertAsync</c>: it
/// does <c>dto.VehicleModel.Value.ToLong()</c>, looks the model up, and throws a 404 when it is
/// not found. A <c>PARITY::VehicleModel.Value</c> string never gets that far. Separately
/// <c>MenuDTO.cs:16-17</c> decorates <c>BrandID</c> with a hash-id converter, so a
/// <c>PARITY::</c> string will not even deserialize, and <c>MenuDTOValidator</c> hard-requires
/// <c>VehicleModel</c>. <b>A baseline in which every CREATE 400s satisfies every other gate,
/// replays identically, and reports green with trap-3-write coverage of exactly zero.</b> That
/// is what the 100% CREATE-2xx gate in ParitySummary exists to catch.
/// </para>
///
/// <para>
/// So: the minimal-valid body is hand-authored per entity and committed as
/// <c>Seed/&lt;group&gt;.&lt;Entity&gt;.create.json</c>, and this class overlays sentinels only onto
/// members that body leaves absent.
/// </para>
///
/// <para>
/// Then the readback tells you everything. Old mapper ignored the member -&gt; readback shows the
/// REPOSITORY-DERIVED value. New mapper writes it by convention -&gt; readback shows the SENTINEL.
/// A sentinel appearing in a readback is unmistakable and self-explaining in a diff.
/// </para>
/// </summary>
public sealed class RequestFactory
{
    private readonly IReadOnlyList<string> seededHashIds;
    private readonly int maxDepth;

    /// <param name="seededHashIds">
    /// Real hash ids of seeded rows. A hash-id member is filled from this list and NEVER with a
    /// PARITY:: string, which cannot decode and would 400 the request before it reached the
    /// mapper.
    /// </param>
    public RequestFactory(IReadOnlyList<string> seededHashIds, int maxDepth = 3)
    {
        this.seededHashIds = seededHashIds;
        this.maxDepth = maxDepth;
    }

    /// <summary>
    /// Overlays sentinels onto every writable member of <paramref name="dtoType"/> that
    /// <paramref name="minimalBody"/> does not already set.
    /// </summary>
    public JsonObject Overlay(Type dtoType, JsonObject minimalBody, string pathPrefix = "")
        => (JsonObject)Fill(dtoType, minimalBody, pathPrefix, 0)!;

    private JsonNode? Fill(Type type, JsonObject? existing, string path, int depth)
    {
        var result = existing is null ? new JsonObject() : (JsonObject)existing.DeepClone();

        if (depth > maxDepth) return result;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite) continue;

            // Never overlay the identity members. ID is the payload of trap 2 and is set by the
            // route or by the server; overwriting it here would corrupt the round-trip.
            if (property.Name is "ID" or "id") continue;

            // The hand-authored minimal body wins wherever it speaks. It is the layer that makes
            // the request VALID; the overlay only fills the silence.
            if (result.ContainsKey(property.Name)) continue;

            var value = SentinelFor(property, path + "." + property.Name, depth);
            if (value is not null || IsNullableReference(property))
                result[property.Name] = value;
        }

        return result;
    }

    private JsonNode? SentinelFor(PropertyInfo property, string path, int depth)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        // ---- hash-id member: a DIFFERENT seeded row's REAL hash id -----------------------
        // Never PARITY::..., which cannot decode. Detection is by attribute inheritance because
        // each entity declares its own subclass, e.g.
        // `class MenuHashId : JsonHashIdConverterAttribute<MenuHashId>`.
        if (HasHashIdConverter(property))
            return seededHashIds.Count == 0
                ? null
                : seededHashIds[Math.Abs(StableHash(path)) % seededHashIds.Count];

        if (type == typeof(string))
            return "PARITY::" + path.TrimStart('.');

        if (type == typeof(bool))
            return true;

        if (type.IsEnum)
        {
            // Second declared value, not the default: the default is what an unset member would
            // already produce, so it could not distinguish "written" from "ignored".
            var values = Enum.GetValues(type);
            return values.Length > 1
                ? Enum.GetName(type, values.GetValue(1)!)
                : Enum.GetName(type, values.GetValue(0)!);
        }

        if (IsNumeric(type))
        {
            // 900000 + stableHash(path) % 90000 - collides with nothing real in these datasets.
            var n = 900000 + Math.Abs(StableHash(path)) % 90000;
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return JsonValue.Create((decimal)n);
            return JsonValue.Create(n);
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return "2099-12-31T23:59:59.0000000+00:00";

        if (type == typeof(Guid))
            return new Guid("00000000-0000-0000-0000-0000000000" + (Math.Abs(StableHash(path)) % 90 + 10)).ToString();

        if (type == typeof(TimeSpan))
            return "23:59:59";

        // ---- collections: exactly ONE element, recursively filled, depth-capped -----------
        var element = ElementTypeOf(type);
        if (element is not null)
        {
            if (depth >= maxDepth) return new JsonArray();
            var inner = Fill(element, null, path + "[0]", depth + 1);
            return new JsonArray(inner);
        }

        // ---- nested object ---------------------------------------------------------------
        if (type.IsClass && type != typeof(object) && !type.IsPrimitive)
        {
            if (depth >= maxDepth) return null;
            return Fill(type, null, path, depth + 1);
        }

        return null;
    }

    private static bool HasHashIdConverter(PropertyInfo property) =>
        property.GetCustomAttributes(inherit: true)
            .Any(a => IsHashIdAttribute(a.GetType()));

    private static bool IsHashIdAttribute(Type? t)
    {
        while (t is not null)
        {
            if (t == typeof(JsonHashIdConverterAttribute)) return true;
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("JsonHashIdConverterAttribute"))
                return true;
            if (t.Name.StartsWith("JsonHashIdConverterAttribute", StringComparison.Ordinal)) return true;
            t = t.BaseType;
        }
        return false;
    }

    private static Type? ElementTypeOf(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();

        var enumerable = type.GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    private static bool IsNumeric(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) ||
        t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte) ||
        t == typeof(decimal) || t == typeof(double) || t == typeof(float);

    private static bool IsNullableReference(PropertyInfo p) =>
        !p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) is not null;

    /// <summary>
    /// Deterministic across runs and processes - unlike string.GetHashCode(), which is
    /// randomized per process and would make the generated request body differ between two
    /// capture runs. That would fire the REQUEST diff (verification.md section 5) on every run
    /// and make the harness useless.
    /// </summary>
    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in s) hash = hash * 31 + c;
            return hash;
        }
    }
}
