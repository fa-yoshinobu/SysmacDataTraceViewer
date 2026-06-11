using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class TraceNavigationServiceTests
{
    [Fact]
    public void BuildChangePointSampleIndexesSkipsMissingValuesAndReturnsSortedUniqueIndexes()
    {
        var trace = new TraceData
        {
            FileName = "trace.csv",
            ElapsedSeconds = [0, 1, 2, 3, 4, 5, 6],
            DateTexts = ["", "", "", "", "", "", ""],
            ClockTimeTexts = ["0", "1", "2", "3", "4", "5", "6"],
            BoolSignals =
            [
                new BoolSignal { Name = "a:BOOL", Values = [null, false, false, true, true, null, false], HasChange = true },
                new BoolSignal { Name = "b:BOOL", Values = [true, true, false, false, true, true, true], HasChange = true }
            ],
            ValueSignals = []
        };

        var points = TraceNavigationService.BuildChangePointSampleIndexes(trace, [0, 1]);

        Assert.Equal([2, 3, 4, 6], points);
    }

    [Fact]
    public void FindClosestSampleHandlesEmptyBoundariesAndTies()
    {
        Assert.Equal(-1, TraceNavigationService.FindClosestSample([], 1.0));
        Assert.Equal(0, TraceNavigationService.FindClosestSample([1.0, 2.0, 4.0], 0.0));
        Assert.Equal(2, TraceNavigationService.FindClosestSample([1.0, 2.0, 4.0], 10.0));
        Assert.Equal(1, TraceNavigationService.FindClosestSample([1.0, 2.0, 4.0], 3.0));
    }

    [Fact]
    public void FindPreviousAndNextChangePointUseStrictCurrentSampleBoundaries()
    {
        int[] points = [2, 5, 8];

        Assert.Null(TraceNavigationService.FindPreviousChangePoint(points, 2));
        Assert.Equal(2, TraceNavigationService.FindPreviousChangePoint(points, 3));
        Assert.Equal(5, TraceNavigationService.FindPreviousChangePoint(points, 8));

        Assert.Equal(5, TraceNavigationService.FindNextChangePoint(points, 2));
        Assert.Equal(8, TraceNavigationService.FindNextChangePoint(points, 5));
        Assert.Null(TraceNavigationService.FindNextChangePoint(points, 8));
    }
}
