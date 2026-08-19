using Reqnroll;
using ShiftSoftware.ADP.Models.Vehicle;
using System.Text.Json;

namespace LookupServices.BDD.Support;

/// <summary>
/// Reads the declarative eligibility grammar out of a scenario table.
/// <para>
/// One reader for both things the grammar gates. Service items and extended-warranty definitions
/// share the condition contract in production, but each step class used to parse its own table, and
/// the warranty side quietly kept an older subset of the columns: a milestone condition could not be
/// written for a coverage at all. Nothing in the suite could have caught a deployment whose codes
/// stopped fitting a suffix comparison, because the shape that survives such codes was unreachable
/// from a warranty scenario. Sharing the reader is what keeps the two from drifting again.
/// </para>
/// </summary>
internal static class EligibilityConditionTable
{
    internal static List<EligibilityConditionModel> Read(DataTable dataTable) =>
        dataTable.Rows.Select(ReadRow).ToList();

    private static EligibilityConditionModel ReadRow(DataTableRow row)
    {
        var hasScope = HasValue(row, "Selection") || HasValue(row, "Count");

        var condition = new EligibilityConditionModel
        {
            Field = row["Field"],
            Operator = Enum.Parse<EligibilityConditionOperator>(row["Operator"]),
            Scope = hasScope ? new EligibilityConditionScope
            {
                Selection = HasValue(row, "Selection")
                    ? Enum.Parse<EligibilityConditionSelection>(row["Selection"])
                    : default,
                Count = HasValue(row, "Count") ? int.Parse(row["Count"]) : null,
            } : null,
            Values = ReadValues(row),
        };

        if (HasValue(row, "ValueMatch"))
            condition.ValueMatch = Enum.Parse<EligibilityConditionValueMatch>(row["ValueMatch"]);

        if (HasValue(row, "WhenUnmet"))
            condition.WhenUnmet = Enum.Parse<EligibilityConditionUnmetBehavior>(row["WhenUnmet"]);

        condition.Program = ReadOptionalList(row, "Program", "ProgramJson");

        // Any of the three columns brings the qualifier into being, so a scenario can pin the
        // selection, its values, or both — and leaving all three out is how a scenario says the
        // author omitted the qualifier altogether.
        if (HasValue(row, "Qualifier") ||
            HasValue(row, "QualifierValues") ||
            HasValue(row, "QualifierValuesJson"))
        {
            condition.Qualifier = new EligibilityConditionQualifier
            {
                Selection = HasValue(row, "Qualifier")
                    ? Enum.Parse<EligibilityConditionQualifierSelection>(row["Qualifier"])
                    : default,
                Values = ReadOptionalList(row, "QualifierValues", "QualifierValuesJson"),
            };
        }

        return condition;
    }

    private static IEnumerable<string> ReadValues(DataTableRow row)
    {
        if (row.ContainsKey("ValuesJson"))
            return JsonSerializer.Deserialize<string[]>(row["ValuesJson"]) ?? [];

        return row["Values"].Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// An optional list-valued condition property, written either comma-separated for readability or
    /// as a JSON array when the scenario is about a shape the shorthand cannot express — an empty
    /// list, or an entry that is blank or null. Absent from both columns means the property was
    /// omitted, which is a distinct case from an empty list in this grammar.
    /// </summary>
    private static IEnumerable<string>? ReadOptionalList(
        DataTableRow row,
        string column,
        string jsonColumn)
    {
        if (HasValue(row, jsonColumn))
            return JsonSerializer.Deserialize<string[]>(row[jsonColumn]) ?? [];

        if (!HasValue(row, column))
            return null;

        return row[column].Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool HasValue(DataTableRow row, string column) =>
        row.ContainsKey(column) && !string.IsNullOrWhiteSpace(row[column]);
}
