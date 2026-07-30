using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>Minimal RFC-4180 CSV reader (quoted fields with embedded commas/newlines, CRLF/LF, BOM via StreamReader).</summary>
public static class Csv
{
    public static IEnumerable<List<string>> ReadRows(string path)
    {
        using var r = new StreamReader(path); // detects + strips UTF-8 BOM
        var field = new StringBuilder();
        var row = new List<string>();
        bool inQuotes = false;
        int c;
        while ((c = r.Read()) != -1)
        {
            char ch = (char)c;
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (r.Peek() == '"') { field.Append('"'); r.Read(); }
                    else inQuotes = false;
                }
                else field.Append(ch);
            }
            else if (ch == '"') inQuotes = true;
            else if (ch == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (ch == '\r') { /* swallow; \n closes the row */ }
            else if (ch == '\n')
            {
                row.Add(field.ToString()); field.Clear();
                yield return row;
                row = new List<string>();
            }
            else field.Append(ch);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row; }
    }
}
