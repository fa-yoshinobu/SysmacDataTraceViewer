using System.IO;

namespace SysmacDataTraceViewer.Services;

internal static class TraceFileInput
{
    public static bool TryGetDropPath(IReadOnlyList<string> paths, out string path, out string reason)
    {
        path = string.Empty;
        reason = string.Empty;
        if (paths.Count != 1)
        {
            reason = "Drop one CSV file at a time.";
            return false;
        }
        if (!Path.GetExtension(paths[0]).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Only CSV files can be opened.";
            return false;
        }
        path = paths[0];
        return true;
    }
}
