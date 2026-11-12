using System.IO;
using System.Text;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class ReadDiagnosticsTests
{
    [Fact]
    public void CountsSkippedRowsInvalidValuesAndBackwardTimeWithPhysicalLines()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                Index,ClockTime,FictionalFlag:BOOL,FictionalCount:INT
                0,00:00:00,0,1
                1
                2,broken,1,2
                3,00:00:01,wrong,no
                4,00:00:00.500,1,3
                5,00:00:02
                6,00:00:03,,
                """);
            var trace = CsvTraceParser.Parse(path);
            Assert.Equal(5, trace.SampleCount);
            Assert.Equal([0, 1, 1, 2.5, 3.5], trace.ElapsedSeconds);
            Assert.Equal(2, trace.Diagnostics.SkippedRows);
            Assert.Equal(4, trace.Diagnostics.InvalidValues);
            Assert.Equal(1, trace.Diagnostics.ReversedTimeRows);
            Assert.Equal([3, 4, 5, 5, 6, 7, 7], trace.Diagnostics.Issues.Select(i => i.LineNumber));
            Assert.Contains("Line 7, column 3", trace.Diagnostics.Describe(), StringComparison.Ordinal);
            Assert.DoesNotContain("wrong", trace.Diagnostics.Describe(), StringComparison.Ordinal);
            Assert.Contains("skipped 2, invalid values 4, reversed time 1", UiFormattingService.BuildStatusText(trace, 1, 1), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DetailListIsBoundedWithoutLosingCounts()
    {
        var diagnostics = new TraceReadDiagnostics();
        for (var i = 0; i < 1000; i++)
        {
            diagnostics.InvalidValue(i + 2, 3, "Invalid value.");
        }

        Assert.Equal(1000, diagnostics.InvalidValues);
        Assert.Equal(TraceReadDiagnostics.DetailLimit, diagnostics.Issues.Count);
        Assert.Contains("Only the first 20", diagnostics.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void MidnightRolloverAndEmptyFieldsAreNotWarnings()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "Index,ClockTime,FictionalFlag:BOOL\n0,23:59:59,\n1,00:00:00,1\n");
            var trace = CsvTraceParser.Parse(path);
            Assert.False(trace.Diagnostics.HasIssues);
            Assert.Equal([0, 1], trace.ElapsedSeconds);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ErrorLogsRotateAndExcludeExceptionMessages()
    {
        var path = Path.GetTempFileName();
        try
        {
            var log = new BoundedErrorLog(path, 256);
            var error = new InvalidOperationException("FictionalPrivateVariable=12345", new IOException("Secret CSV cell"));
            for (var i = 0; i < 40; i++)
            {
                log.Record("Read CSV", error);
            }

            Assert.True(new FileInfo(path).Length <= 256);
            Assert.True(new FileInfo(path + ".1").Length <= 256);
            var text = File.ReadAllText(path, new UTF8Encoding(false, true)) + File.ReadAllText(path + ".1");
            Assert.Contains("System.InvalidOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FictionalPrivateVariable", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Secret CSV cell", text, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".1");
        }
    }

    [Fact]
    public void LoggingFailureDoesNotEscapeTheErrorHandler()
    {
        var blocker = Path.GetTempFileName();
        try
        {
            var log = new BoundedErrorLog(Path.Combine(blocker, "errors.log"));
            Assert.Null(Record.Exception(() => log.Record("Failure", new IOException())));
        }
        finally
        {
            File.Delete(blocker);
        }
    }
}
