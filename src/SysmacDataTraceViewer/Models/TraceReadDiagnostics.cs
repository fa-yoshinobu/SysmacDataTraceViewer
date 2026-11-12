namespace SysmacDataTraceViewer.Models;

internal sealed record TraceReadIssue(int LineNumber, int? ColumnNumber, string Reason);

internal sealed class TraceReadDiagnostics
{
    public const int DetailLimit = 20;
    private readonly List<TraceReadIssue> _issues = [];
    public int SkippedRows { get; private set; }
    public int InvalidValues { get; private set; }
    public int ReversedTimeRows { get; private set; }
    public bool HasIssues => SkippedRows + InvalidValues + ReversedTimeRows > 0;
    public IReadOnlyList<TraceReadIssue> Issues => _issues;

    public void Skip(int line, string reason)
    {
        SkippedRows++;
        Add(line, null, reason);
    }

    public void InvalidValue(int line, int column, string reason)
    {
        InvalidValues++;
        Add(line, column, reason);
    }

    public void ReversedTime(int line)
    {
        ReversedTimeRows++;
        Add(line, null, "Time moved backwards; elapsed time was clamped as before.");
    }

    private void Add(int line, int? column, string reason)
    {
        if (_issues.Count < DetailLimit)
        {
            _issues.Add(new TraceReadIssue(line, column, reason));
        }
    }

    public string Describe() =>
        $"Skipped rows: {SkippedRows}; invalid values: {InvalidValues}; reversed-time rows: {ReversedTimeRows}."
        + Environment.NewLine
        + string.Join(Environment.NewLine, _issues.Select(i =>
            $"Line {i.LineNumber}" + (i.ColumnNumber.HasValue ? $", column {i.ColumnNumber}" : string.Empty) + $": {i.Reason}"))
        + (SkippedRows + InvalidValues + ReversedTimeRows > _issues.Count ? $"{Environment.NewLine}Only the first {DetailLimit} issues are listed." : string.Empty);
}
