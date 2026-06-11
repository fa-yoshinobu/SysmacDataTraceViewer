using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class CursorStateTests
{
    [Fact]
    public void InitializeForTraceDefaultsBothCursorsToFirstSample()
    {
        var state = new CursorState();
        var trace = CreateTrace();

        state.InitializeForTrace(trace);

        Assert.Equal(1.0, state.PrimaryX);
        Assert.Equal(1.0, state.DeltaX);
        Assert.Equal(0, state.LastPrimarySampleIndex);
        Assert.Equal(0, state.LastDeltaSampleIndex);
    }

    [Fact]
    public void InitializeForTraceClampsExistingCursorPositions()
    {
        var state = new CursorState();
        var trace = CreateTrace();

        Assert.True(state.TryMovePrimaryToSample(trace, 2));
        Assert.True(state.TryMoveDeltaToSample(trace, 1));
        state.InitializeForTrace(CreateShorterTrace());

        Assert.Equal(2.0, state.PrimaryX);
        Assert.Equal(2.0, state.DeltaX);
        Assert.Equal(1, state.LastPrimarySampleIndex);
        Assert.Equal(1, state.LastDeltaSampleIndex);
    }

    [Fact]
    public void TryMovePrimaryToSampleRejectsInvalidAndRepeatedSamples()
    {
        var state = new CursorState();
        var trace = CreateTrace();
        state.InitializeForTrace(trace);

        Assert.False(state.TryMovePrimaryToSample(trace, -1));
        Assert.False(state.TryMovePrimaryToSample(trace, trace.SampleCount));
        Assert.False(state.TryMovePrimaryToSample(trace, 0));
        Assert.True(state.TryMovePrimaryToSample(trace, 2));

        Assert.Equal(3.0, state.PrimaryX);
        Assert.Equal(2, state.LastPrimarySampleIndex);
    }

    [Fact]
    public void TryMoveDeltaToSampleRejectsInvalidAndRepeatedSamples()
    {
        var state = new CursorState();
        var trace = CreateTrace();
        state.InitializeForTrace(trace);

        Assert.False(state.TryMoveDeltaToSample(trace, -1));
        Assert.False(state.TryMoveDeltaToSample(trace, trace.SampleCount));
        Assert.False(state.TryMoveDeltaToSample(trace, 0));
        Assert.True(state.TryMoveDeltaToSample(trace, 1));

        Assert.Equal(2.0, state.DeltaX);
        Assert.Equal(1, state.LastDeltaSampleIndex);
    }

    [Fact]
    public void TrySwapExchangesCursorPositionsAndSampleIndexes()
    {
        var state = new CursorState();
        var trace = CreateTrace();
        state.InitializeForTrace(trace);
        Assert.True(state.TryMovePrimaryToSample(trace, 2));
        Assert.True(state.TryMoveDeltaToSample(trace, 1));

        Assert.True(state.TrySwap(trace));

        Assert.Equal(2.0, state.PrimaryX);
        Assert.Equal(3.0, state.DeltaX);
        Assert.Equal(1, state.LastPrimarySampleIndex);
        Assert.Equal(2, state.LastDeltaSampleIndex);
    }

    private static TraceData CreateTrace() =>
        new()
        {
            FileName = "trace.csv",
            ElapsedSeconds = [1, 2, 3],
            DateTexts = ["", "", ""],
            ClockTimeTexts = ["1", "2", "3"],
            BoolSignals = [new BoolSignal { Name = "Flag:BOOL", Values = [false, true, false], HasChange = true }],
            ValueSignals = []
        };

    private static TraceData CreateShorterTrace() =>
        new()
        {
            FileName = "short.csv",
            ElapsedSeconds = [0, 2],
            DateTexts = ["", ""],
            ClockTimeTexts = ["0", "2"],
            BoolSignals = [new BoolSignal { Name = "Flag:BOOL", Values = [false, true], HasChange = true }],
            ValueSignals = []
        };
}
