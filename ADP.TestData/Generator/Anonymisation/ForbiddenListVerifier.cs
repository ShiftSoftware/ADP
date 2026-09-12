using System.Text.Json;

namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// Scans environment and fixture JSON for strings that must never appear in this repository — the
/// private forbidden list a deployment keeps next to its seed: its own names, hosts, places, and every
/// real identifier its key maps re-keyed. A hit fails the run. The list is never echoed: a hit is
/// reported as the list's line number and the JSON path it matched at — with any path segment that is
/// itself a hit (a dictionary key) masked — so the console carries no forbidden string either.
/// <para>
/// List format, one term per line: a plain line matches as a case-insensitive substring of any string value
/// or property name; a line starting with <c>word:</c> matches only where the term is not glued to other
/// letters or digits (for short codes and numbers that would otherwise match inside unrelated values);
/// <c>case:</c> is <c>word:</c> matched case-sensitively (for an abbreviation that is also a word);
/// <c>#</c> starts a comment.
/// </para>
/// </summary>
public static class ForbiddenListVerifier
{
    public static int Run(string listPath, IEnumerable<string> scanPaths)
    {
        var terms = LoadTerms(listPath);
        if (terms.Count == 0)
        {
            Console.WriteLine($"--verify: the list at '{listPath}' has no terms; nothing to check.");
            return 1;
        }

        var matcher = new AhoCorasick(terms.Select(t => t.text.ToLowerInvariant()));
        var files = scanPaths
            .SelectMany(p => Directory.Exists(p) ? Directory.GetFiles(p, "*.json", SearchOption.AllDirectories) : new[] { p })
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        var hits = 0;
        var values = 0;

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var fileHits = new List<string>();

            void Scan(JsonElement element, string path)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        foreach (var property in element.EnumerateObject())
                        {
                            var hit = Check(property.Name, path + "/* (name)");
                            var segment = hit is { } line ? $"[term #{line}]" : property.Name;
                            Scan(property.Value, path + "/" + segment);
                        }
                        break;
                    case JsonValueKind.Array:
                        var i = 0;
                        foreach (var item in element.EnumerateArray())
                            Scan(item, path + "[" + i++ + "]");
                        break;
                    case JsonValueKind.String:
                        Check(element.GetString()!, path);
                        break;
                }
            }

            int? Check(string value, string path)
            {
                values++;
                int? first = null;
                foreach (var (termIndex, start, length) in matcher.Find(value.ToLowerInvariant()))
                {
                    var term = terms[termIndex];
                    if (term.whole && !IsWhole(value, start, length))
                        continue;
                    if (term.caseSensitive && !string.Equals(value.Substring(start, length), term.text, StringComparison.Ordinal))
                        continue;

                    first ??= term.line;
                    fileHits.Add($"  term #{term.line} at {path}");
                }

                return first;
            }

            Scan(document.RootElement, string.Empty);

            if (fileHits.Count > 0)
            {
                hits += fileHits.Count;
                Console.WriteLine($"{file}: {fileHits.Count} hit(s)");
                foreach (var hit in fileHits.Distinct().Take(50))
                    Console.WriteLine(hit);
                if (fileHits.Count > 50)
                    Console.WriteLine($"  … {fileHits.Count - 50} more");
            }
        }

        Console.WriteLine($"--verify: {terms.Count} terms, {files.Count} file(s), {values} strings scanned, {hits} hit(s). (\"term #n\" is line n of the list.)");
        return hits == 0 ? 0 : 1;
    }

    private static List<(string text, bool whole, bool caseSensitive, int line)> LoadTerms(string listPath)
    {
        var terms = new List<(string text, bool whole, bool caseSensitive, int line)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lineNumber = 0;

        foreach (var raw in File.ReadLines(listPath))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var caseSensitive = line.StartsWith("case:", StringComparison.OrdinalIgnoreCase);
            var whole = caseSensitive || line.StartsWith("word:", StringComparison.OrdinalIgnoreCase);
            var term = (whole ? line[5..] : line).Trim();
            if (!caseSensitive)
                term = term.ToLowerInvariant();
            if (term.Length == 0 || !seen.Add((caseSensitive ? "c:" : whole ? "w:" : "s:") + term))
                continue;

            terms.Add((term, whole, caseSensitive, lineNumber));
        }

        return terms;
    }

    private static bool IsWhole(string value, int start, int length)
    {
        var before = start == 0 ? ' ' : value[start - 1];
        var after = start + length >= value.Length ? ' ' : value[start + length];
        return !char.IsLetterOrDigit(before) && !char.IsLetterOrDigit(after);
    }

    /// <summary>Aho–Corasick over lower-cased terms: every occurrence of every term in one pass per string.</summary>
    private sealed class AhoCorasick
    {
        private readonly List<Dictionary<char, int>> next = new();
        private readonly List<int> fail = new();
        private readonly List<List<int>> output = new();
        private readonly List<int> lengths = new();

        public AhoCorasick(IEnumerable<string> terms)
        {
            AddNode();
            var index = 0;
            foreach (var term in terms)
            {
                var node = 0;
                foreach (var c in term)
                {
                    if (!next[node].TryGetValue(c, out var child))
                    {
                        child = AddNode();
                        next[node][c] = child;
                    }
                    node = child;
                }
                output[node].Add(index);
                lengths.Add(term.Length);
                index++;
            }

            var queue = new Queue<int>();
            foreach (var (c, child) in next[0])
            {
                fail[child] = 0;
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var (c, child) in next[node])
                {
                    var f = fail[node];
                    while (f != 0 && !next[f].ContainsKey(c))
                        f = fail[f];
                    fail[child] = next[f].TryGetValue(c, out var target) && target != child ? target : 0;
                    output[child].AddRange(output[fail[child]]);
                    queue.Enqueue(child);
                }
            }
        }

        private int AddNode()
        {
            next.Add(new Dictionary<char, int>());
            fail.Add(0);
            output.Add(new List<int>());
            return next.Count - 1;
        }

        public IEnumerable<(int term, int start, int length)> Find(string text)
        {
            var node = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                while (node != 0 && !next[node].ContainsKey(c))
                    node = fail[node];
                node = next[node].TryGetValue(c, out var child) ? child : 0;

                foreach (var term in output[node])
                    yield return (term, i - lengths[term] + 1, lengths[term]);
            }
        }
    }
}
