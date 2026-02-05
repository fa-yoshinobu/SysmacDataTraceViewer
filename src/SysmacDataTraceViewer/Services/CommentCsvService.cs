using System.IO;

namespace SysmacDataTraceViewer.Services;

public sealed record CommentSignalState(string Name, string Comment, bool IsVisible, string ColorHex, int Order);

public sealed class CommentCsvData
{
    public List<CommentSignalState> BoolSignals { get; } = new();
    public List<CommentSignalState> ValueSignals { get; } = new();
}

public static class CommentCsvService
{
    public static void Save(
        string filePath,
        IReadOnlyList<CommentSignalState> boolSignals,
        IReadOnlyList<CommentSignalState> valueSignals)
    {
        var lines = new List<string> { "Type,Name,Comment,IsVisible,ColorHex,Order" };
        lines.AddRange(boolSignals.Select(static s =>
            BuildLine("BOOL", s.Name, s.Comment, s.IsVisible, s.ColorHex, s.Order)));
        lines.AddRange(valueSignals.Select(static s =>
            BuildLine("VALUE", s.Name, s.Comment, s.IsVisible, string.Empty, s.Order)));

        File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
    }

    public static CommentCsvData Load(string filePath)
    {
        var result = new CommentCsvData();
        foreach (var rawLine in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var cols = CsvLineParser.Parse(rawLine);
            if (cols.Count < 3)
            {
                continue;
            }

            if (cols[0].Equals("Type", StringComparison.OrdinalIgnoreCase) &&
                cols[1].Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isVisible = cols.Count >= 4 && TryParseBool(cols[3], out var visibleValue) ? visibleValue : true;
            var colorHex = cols.Count >= 5 ? cols[4] : string.Empty;
            var order = cols.Count >= 6 && int.TryParse(cols[5], out var orderValue) ? orderValue : int.MaxValue;
            var state = new CommentSignalState(cols[1], cols[2], isVisible, colorHex, order);

            if (cols[0].Equals("BOOL", StringComparison.OrdinalIgnoreCase))
            {
                result.BoolSignals.Add(state);
            }
            else if (cols[0].Equals("VALUE", StringComparison.OrdinalIgnoreCase))
            {
                result.ValueSignals.Add(state);
            }
        }

        return result;
    }

    private static string BuildLine(string type, string name, string comment, bool isVisible, string colorHex, int order) =>
        $"{Escape(type)},{Escape(name)},{Escape(comment)},{Escape(isVisible ? "1" : "0")},{Escape(colorHex)},{Escape(order.ToString())}";

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static bool TryParseBool(string text, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed == "1")
        {
            value = true;
            return true;
        }

        if (trimmed == "0")
        {
            value = false;
            return true;
        }

        return bool.TryParse(trimmed, out value);
    }
}
