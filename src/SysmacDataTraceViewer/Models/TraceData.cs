namespace SysmacDataTraceViewer.Models;

internal sealed class TraceData
{
    public required string FileName { get; init; }
    public required IReadOnlyList<double> ElapsedSeconds { get; init; }
    public required IReadOnlyList<string> DateTexts { get; init; }
    public required IReadOnlyList<string> ClockTimeTexts { get; init; }
    public required IReadOnlyList<BoolSignal> BoolSignals { get; init; }
    public required IReadOnlyList<ValueSignal> ValueSignals { get; init; }
    public int SampleCount => ElapsedSeconds.Count;
}

internal sealed class BoolSignal
{
    public required string Name { get; init; }
    public required IReadOnlyList<bool?> Values { get; init; }
    public required bool HasChange { get; init; }
}

internal sealed class ValueSignal
{
    public required string Name { get; init; }
    public required IReadOnlyList<string?> Values { get; init; }
    public required bool HasChange { get; init; }
}
