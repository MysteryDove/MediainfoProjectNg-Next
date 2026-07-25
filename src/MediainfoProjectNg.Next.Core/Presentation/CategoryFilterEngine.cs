namespace MediainfoProjectNg.Next.Core.Presentation;

/// <summary>
/// Pure OR category filter over row display items. Counts are distinct-file counts
/// over the canonical loaded set and do not change when filters toggle.
/// </summary>
public static class CategoryFilterEngine
{
    public static IReadOnlyDictionary<IssueCategory, int> ComputeDistinctFileCounts(
        IReadOnlyList<IReadOnlyList<IssueDisplayItem>> allRowsItems)
    {
        var counts = IssueCategoryRegistry.FilterableCategories
            .ToDictionary(c => c, _ => 0);

        foreach (var items in allRowsItems)
        {
            var present = items
                .Where(i => i.IsFilterable)
                .Select(i => i.Category)
                .Distinct()
                .ToList();
            foreach (var cat in present)
            {
                counts[cat]++;
            }
        }

        return counts;
    }

    public static bool RowMatches(
        IReadOnlyList<IssueDisplayItem> items,
        IReadOnlySet<IssueCategory> activeCategories)
    {
        if (activeCategories.Count == 0)
        {
            return true;
        }

        return items.Any(i => i.IsFilterable && activeCategories.Contains(i.Category));
    }

    public static IReadOnlyList<TRow> FilterRows<TRow>(
        IReadOnlyList<TRow> canonical,
        Func<TRow, IReadOnlyList<IssueDisplayItem>> itemsSelector,
        IReadOnlySet<IssueCategory> activeCategories)
    {
        if (activeCategories.Count == 0)
        {
            return canonical.ToList();
        }

        return canonical
            .Where(r => RowMatches(itemsSelector(r), activeCategories))
            .ToList();
    }
}

/// <summary>
/// Pure Extended-selection reconciliation after filter changes.
/// </summary>
public static class SelectionReconciler
{
    public static (TRow? Primary, IReadOnlyList<TRow> Selected) Reconcile<TRow>(
        TRow? primary,
        IReadOnlyList<TRow> selected,
        IReadOnlySet<TRow> visible)
        where TRow : class
    {
        if (primary is null)
        {
            return (null, Array.Empty<TRow>());
        }

        if (!visible.Contains(primary))
        {
            // Primary filtered out → clear all selection.
            return (null, Array.Empty<TRow>());
        }

        // Primary remains; drop only hidden secondary selections.
        var kept = selected.Where(visible.Contains).ToList();
        if (!kept.Contains(primary))
        {
            kept.Insert(0, primary);
        }

        return (primary, kept);
    }
}
