using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Cases.Data.Flow;

/// <summary>Reconciles a case's child rows with the ones its form sent.</summary>
public static class CaseRows
{
    /// <summary>
    /// Makes a stored grid match the one that was sent, keeping the rows that are still there: a row is matched on the
    /// identity it carries and updated in place, and only what was not sent back is removed. A row whose id is not on
    /// this case is a new row.
    /// </summary>
    /// <param name="apply">
    /// Must never write the row's identity or its parent key, so a row cannot reach into another case.
    /// </param>
    public static void Reconcile<TEntity, TDto>(
        ICollection<TEntity> stored,
        List<TDto>? sent,
        Func<TDto, string?> idOf,
        Action<TDto, TEntity> apply)
        where TEntity : ShiftEntityBase, new()
    {
        sent ??= [];

        var original = stored.ToList();
        var byId = original.ToDictionary(x => x.ID);
        var matched = new HashSet<long>();

        foreach (var row in sent)
        {
            if (long.TryParse(idOf(row), out var id) && byId.TryGetValue(id, out var existing))
            {
                apply(row, existing);
                matched.Add(existing.ID);
            }
            else
            {
                var added = new TEntity();
                apply(row, added);
                stored.Add(added);
            }
        }

        // Leaving the tracked collection deletes the row when the consumer's relationship cascades.
        foreach (var row in original.Where(x => !matched.Contains(x.ID)))
            stored.Remove(row);
    }
}
