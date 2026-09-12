namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// One identifier family: every real value of the family maps to one synthetic value, the same everywhere
/// it occurs. The anonymiser walks an environment twice — a collecting pass that registers every real
/// value, then the assignment, then a rewriting pass — so a family can guarantee that distinct real values
/// get distinct synthetic ones (a keyed derivation is retried on a clash, in a fixed order, so the result
/// is a pure function of the seed and the set of real values) and that no synthetic value coincides with
/// a real one of the same family.
/// </summary>
public sealed class Family
{
    private readonly Func<string, int, string> derive;
    private readonly Func<string, string> normalise;
    private readonly SortedSet<string> real = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> assigned = new(StringComparer.Ordinal);
    private readonly HashSet<string> used = new(StringComparer.Ordinal);
    private bool isAssigned;

    /// <param name="name">The family name, part of every keyed derivation so families never share a keystream.</param>
    /// <param name="derive">real value, attempt → synthetic candidate.</param>
    /// <param name="normalise">The identity of a real value within the family (a case-insensitive family folds case here).</param>
    /// <param name="unique">Whether distinct real values must get distinct synthetic ones (retried on a clash).</param>
    public Family(string name, Func<string, int, string> derive, Func<string, string>? normalise = null, bool unique = true)
    {
        Name = name;
        this.derive = derive;
        this.normalise = normalise ?? (v => v);
        Unique = unique;
    }

    public string Name { get; }
    public bool Unique { get; }
    public int Count => assigned.Count;

    /// <summary>The real values collected so far (normalised), for membership tests during the rewrite.</summary>
    public IReadOnlySet<string> Real => real;

    public void Collect(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var key = normalise(value);
        if (!real.Contains(key))
        {
            if (isAssigned)
                throw new InvalidOperationException($"Family '{Name}' met a value during the rewrite that the collecting pass did not see; the two walks must cover the same fields.");

            real.Add(key);
        }
    }

    public bool Contains(string? value) => value is not null && real.Contains(normalise(value));

    /// <summary>Assigns every collected real value its synthetic one, in sorted order, retrying clashes.</summary>
    public void Assign()
    {
        foreach (var key in real)
        {
            for (var attempt = 0; ; attempt++)
            {
                var candidate = derive(key, attempt);

                if (attempt == 0 && candidate == key)
                    throw new InvalidOperationException($"Family '{Name}' cannot re-key a value shaped '{Mask(key)}': the derivation leaves it unchanged (nothing in it is a letter or a digit it knows). The field it came from needs a different treatment.");

                if (!Unique)
                {
                    assigned[key] = candidate;
                    break;
                }

                // A synthetic value must not be another real value of the family (it would read as a
                // reference to it) and must not be shared between two real values (a verdict could merge).
                if (!real.Contains(candidate) && used.Add(candidate))
                {
                    assigned[key] = candidate;
                    break;
                }

                if (attempt > 1000)
                    throw new InvalidOperationException($"Family '{Name}' found no free synthetic value after 1000 attempts for a value shaped '{Mask(key)}'; the pool is too small for this environment, or the value has nothing to re-key.");
            }
        }

        isAssigned = true;
    }

    /// <summary>The synthetic value of a real one. Null and empty pass through untouched.</summary>
    public string? Get(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var key = normalise(value);

        if (assigned.TryGetValue(key, out var synthetic))
            return synthetic;

        if (!isAssigned)
            throw new InvalidOperationException($"Family '{Name}' was read before its assignment.");

        throw new InvalidOperationException($"Family '{Name}' has no assignment for a value the collecting pass did not register.");
    }

    /// <summary>
    /// The synthetic value of a real one, assigning it on the spot when the collecting pass did not see it.
    /// For values that surface only inside free text after other replacements have run (a name the
    /// vocabulary maps to a coined word); identifier fields use the strict <see cref="Get"/>.
    /// </summary>
    public string GetOrAssign(string value)
    {
        var key = normalise(value);

        if (assigned.TryGetValue(key, out var synthetic))
            return synthetic;

        real.Add(key);

        for (var attempt = 0; ; attempt++)
        {
            var candidate = derive(key, attempt);

            if (!Unique || (!real.Contains(candidate) && used.Add(candidate)))
            {
                assigned[key] = candidate;
                return candidate;
            }

            if (attempt > 1000)
                throw new InvalidOperationException($"Family '{Name}' found no free synthetic value after 1000 attempts.");
        }
    }

    /// <summary>The shape of a value (digits → 9, letters → A / a), safe to print: it is not the value.</summary>
    public static string Mask(string value) =>
        new(value.Select(c => char.IsAsciiDigit(c) ? '9' : char.IsAsciiLetterUpper(c) ? 'A' : char.IsAsciiLetterLower(c) ? 'a' : char.IsLetterOrDigit(c) ? 'x' : c).ToArray());

    /// <summary>real → synthetic, for the private key map. The real side never goes anywhere public.</summary>
    public IReadOnlyDictionary<string, string> Map => assigned;
}

/// <summary>
/// Case-shape helpers: a fictional replacement takes the case shape of the text it replaces, so an
/// all-capitals description stays all-capitals and a capitalised name stays capitalised.
/// </summary>
public static class CaseShape
{
    public static string Like(string replacement, string original)
    {
        if (original.Length == 0 || replacement.Length == 0)
            return replacement;

        var letters = original.Where(char.IsLetter).ToList();
        if (letters.Count == 0)
            return replacement;

        if (letters.All(char.IsUpper))
            return replacement.ToUpperInvariant();

        if (letters.All(char.IsLower))
            return replacement.ToLowerInvariant();

        return replacement;
    }

    /// <summary>The value with its leading and trailing whitespace kept aside, so a padded field stays padded.</summary>
    public static (string leading, string core, string trailing) Trim(string value)
    {
        var core = value.Trim();
        if (core.Length == 0)
            return (value, string.Empty, string.Empty);

        var start = value.IndexOf(core, StringComparison.Ordinal);
        return (value[..start], core, value[(start + core.Length)..]);
    }

    public static string Capitalise(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();

    public static bool IsAllUpper(string s) => s.Any(char.IsLetter) && s.Where(char.IsLetter).All(char.IsUpper);
}

/// <summary>Which script a token is written in, so a replacement name keeps the script of the name it replaces.</summary>
public enum Script { Latin, Arabic, Cyrillic }

public static class Scripts
{
    public static Script Of(string token)
    {
        foreach (var c in token)
        {
            if (c is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F')
                return Script.Arabic;
            if (c is >= '\u0400' and <= '\u04FF')
                return Script.Cyrillic;
        }

        return Script.Latin;
    }
}
