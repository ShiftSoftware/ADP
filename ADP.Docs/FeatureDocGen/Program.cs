using System.Text;
using System.Text.RegularExpressions;

var baseDir = AppContext.BaseDirectory;
var featuresDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../../ADP.LookupServices.BDD/Features"));
var outputDir = Path.GetFullPath(Path.Combine(baseDir, "../../../../Docs/docs/generated/Features"));

if (!Directory.Exists(featuresDir))
{
    Console.WriteLine($"Features directory not found: {featuresDir}");
    return;
}

Directory.CreateDirectory(outputDir);

var featureFiles = Directory.GetFiles(featuresDir, "*.feature");

foreach (var featureFile in featureFiles)
{
    var fileName = Path.GetFileNameWithoutExtension(featureFile);
    var content = File.ReadAllText(featureFile);

    var sb = new StringBuilder();
    sb.AppendLine("---");
    sb.AppendLine("hide:");
    sb.AppendLine("    - toc");
    sb.AppendLine("---");
    sb.AppendLine();

    var lines = content.Replace("\r\n", "\n").Split('\n');

    var ruleIndices = new List<int>();
    for (int i = 0; i < lines.Length; i++)
    {
        if (Regex.IsMatch(lines[i], @"^\s*Rule\s*:"))
            ruleIndices.Add(i);
    }

    if (ruleIndices.Count == 0)
    {
        sb.AppendLine("```gherkin");
        sb.Append(content);
        if (!content.EndsWith("\n"))
            sb.AppendLine();
        sb.AppendLine("```");
    }
    else
    {
        WritePreamble(sb, lines, ruleIndices[0]);

        for (int r = 0; r < ruleIndices.Count; r++)
        {
            int start = ruleIndices[r];
            int end = (r + 1 < ruleIndices.Count) ? ruleIndices[r + 1] : lines.Length;

            var ruleLine = lines[start].TrimStart();
            var ruleName = ruleLine.Substring(ruleLine.IndexOf(':') + 1).Trim();

            // First rule expanded by default; the rest collapsed.
            var marker = "???";
            sb.AppendLine($"{marker} note \"Rule: {ruleName}\"");
            sb.AppendLine();
            sb.AppendLine("    ```gherkin");

            int bodyStart = start + 1;
            int bodyEnd = end;
            // trim leading blank lines
            while (bodyStart < bodyEnd && string.IsNullOrWhiteSpace(lines[bodyStart])) bodyStart++;
            // trim trailing blank lines
            while (bodyEnd > bodyStart && string.IsNullOrWhiteSpace(lines[bodyEnd - 1])) bodyEnd--;

            for (int j = bodyStart; j < bodyEnd; j++)
            {
                sb.AppendLine("    " + lines[j]);
            }
            sb.AppendLine("    ```");
            sb.AppendLine();
        }
    }

    var outputPath = Path.Combine(outputDir, $"{fileName}.md");
    File.WriteAllText(outputPath, sb.ToString());

    Console.WriteLine($"Generated: {outputPath}");
}

static void WritePreamble(StringBuilder sb, string[] lines, int firstRuleIndex)
{
    int end = firstRuleIndex;
    while (end > 0 && string.IsNullOrWhiteSpace(lines[end - 1])) end--;
    if (end == 0) return;

    int start = 0;
    while (start < end && string.IsNullOrWhiteSpace(lines[start])) start++;
    if (start >= end) return;

    var featureLine = lines[start].TrimStart();
    if (featureLine.StartsWith("Feature:", StringComparison.OrdinalIgnoreCase))
    {
        var title = featureLine.Substring("Feature:".Length).Trim();
        sb.AppendLine($"# {title}");
        sb.AppendLine();

        var desc = new List<string>();
        for (int i = start + 1; i < end; i++) desc.Add(StripOneIndent(lines[i]));

        bool prevBlank = true;
        bool prevList = false;
        for (int i = 0; i < desc.Count; i++)
        {
            var line = desc[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
                prevBlank = true;
                prevList = false;
                continue;
            }

            bool isList = IsListItem(line);
            if (!prevBlank && isList != prevList) sb.AppendLine();

            bool nextIsText = (i + 1 < desc.Count)
                && !string.IsNullOrWhiteSpace(desc[i + 1])
                && !IsListItem(desc[i + 1]);

            if (!isList && nextIsText)
                sb.AppendLine(line + "  ");
            else
                sb.AppendLine(line);

            prevBlank = false;
            prevList = isList;
        }
        sb.AppendLine();
    }
    else
    {
        sb.AppendLine("```gherkin");
        for (int i = start; i < end; i++) sb.AppendLine(lines[i]);
        sb.AppendLine("```");
        sb.AppendLine();
    }
}

static bool IsListItem(string line)
{
    var t = line.TrimStart();
    if (t.Length == 0) return false;
    if ((t.StartsWith("- ") || t.StartsWith("* ") || t.StartsWith("+ "))) return true;
    int i = 0;
    while (i < t.Length && char.IsDigit(t[i])) i++;
    if (i == 0 || i >= t.Length) return false;
    if ((t[i] == '.' || t[i] == ')') && i + 1 < t.Length && t[i + 1] == ' ') return true;
    return false;
}

static string StripOneIndent(string line)
{
    if (line.StartsWith("\t")) return line.Substring(1);
    int spaces = 0;
    while (spaces < line.Length && spaces < 4 && line[spaces] == ' ') spaces++;
    return line.Substring(spaces);
}
