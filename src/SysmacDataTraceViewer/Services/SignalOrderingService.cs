namespace SysmacDataTraceViewer.Services;

internal static class SignalOrderingService
{
    public static List<T>? BuildOrderedRows<T>(
        IEnumerable<T> rows,
        IReadOnlyList<string> orderedNames,
        Func<T, string> getName)
    {
        if (orderedNames.Count == 0)
        {
            return null;
        }

        var sourceRows = rows.ToList();
        var map = sourceRows.ToDictionary(getName, StringComparer.Ordinal);
        var ordered = new List<T>(sourceRows.Count);

        foreach (var name in orderedNames)
        {
            if (map.Remove(name, out var row))
            {
                ordered.Add(row);
            }
        }

        ordered.AddRange(map.Values);
        return ordered.Count == sourceRows.Count ? ordered : null;
    }
}
