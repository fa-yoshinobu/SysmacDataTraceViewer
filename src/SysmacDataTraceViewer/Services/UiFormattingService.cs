using SysmacDataTraceViewer.Models;

namespace SysmacDataTraceViewer.Services;

internal static class UiFormattingService
{
    public static string FormatVariableName(string rawName, bool showTypeSuffix)
    {
        if (showTypeSuffix)
        {
            return rawName;
        }

        var typePos = rawName.LastIndexOf(':');
        return typePos > 0 ? rawName[..typePos] : rawName;
    }

    public static string BuildDefaultComment(string signalName)
    {
        var noType = signalName;
        var typePos = signalName.IndexOf(':', StringComparison.Ordinal);
        if (typePos > 0)
        {
            noType = signalName[..typePos];
        }

        var splitPos = noType.LastIndexOf('.');
        if (splitPos >= 0 && splitPos < noType.Length - 1)
        {
            return noType[(splitPos + 1)..];
        }

        return noType;
    }

    public static string BuildStatusText(TraceData traceData, int visibleBoolCount, int visibleValueCount) =>
        $"Loaded: {traceData.FileName} / Samples: {traceData.SampleCount:N0} / BOOL: {traceData.BoolSignals.Count} (visible: {visibleBoolCount}) / Values: {traceData.ValueSignals.Count} (visible: {visibleValueCount})";

    public static string BuildOriginalTimeText(TraceData traceData, int sampleIndex)
    {
        var date = traceData.DateTexts[sampleIndex];
        var clock = traceData.ClockTimeTexts[sampleIndex];
        if (string.IsNullOrWhiteSpace(date))
        {
            return clock;
        }

        return $"{date} {clock}";
    }
}
