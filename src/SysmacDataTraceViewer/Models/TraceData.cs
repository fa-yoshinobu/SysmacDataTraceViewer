namespace SysmacDataTraceViewer.Models;

public sealed class TraceData
{
    public required string FileName { get; init; }
    public required double[] ElapsedSeconds { get; init; }
    public required string[] DateTexts { get; init; }
    public required string[] ClockTimeTexts { get; init; }
    public required IReadOnlyList<BoolSignal> BoolSignals { get; init; }
    public required IReadOnlyList<ValueSignal> ValueSignals { get; init; }
    public int SampleCount => ElapsedSeconds.Length;
}

public sealed class BoolSignal
{
    public required string Name { get; init; }
    public required bool?[] Values { get; init; }
    public required bool HasChange { get; init; }
}

public sealed class ValueSignal
{
    public required string Name { get; init; }
    public required string?[] Values { get; init; }
    public required bool HasChange { get; init; }
}
