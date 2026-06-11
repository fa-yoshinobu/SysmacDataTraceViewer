using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class FormattingAndColorServiceTests
{
    [Fact]
    public void UiFormattingBuildsLabelsStatusAndOriginalTimeText()
    {
        var trace = new TraceData
        {
            FileName = "trace.csv",
            ElapsedSeconds = [0, 1],
            DateTexts = ["2026/02/05", ""],
            ClockTimeTexts = ["12:00:00.000", "12:00:01.000"],
            BoolSignals = [new BoolSignal { Name = "Machine.Flag:BOOL", Values = [false, true], HasChange = true }],
            ValueSignals = [new ValueSignal { Name = "Machine.Count:DINT", Values = ["1", "2"], HasChange = true }]
        };

        Assert.Equal("Machine.Flag", UiFormattingService.FormatVariableName("Machine.Flag:BOOL", showTypeSuffix: false));
        Assert.Equal("Machine.Flag:BOOL", UiFormattingService.FormatVariableName("Machine.Flag:BOOL", showTypeSuffix: true));
        Assert.Equal("Flag", UiFormattingService.BuildDefaultComment("Machine.Flag:BOOL"));
        Assert.Equal("Name", UiFormattingService.BuildDefaultComment("Name"));
        Assert.Equal("Loaded: trace.csv / Samples: 2 / BOOL: 1 (visible: 1) / Values: 1 (visible: 1)", UiFormattingService.BuildStatusText(trace, 1, 1));
        Assert.Equal("2026/02/05 12:00:00.000", UiFormattingService.BuildOriginalTimeText(trace, 0));
        Assert.Equal("12:00:01.000", UiFormattingService.BuildOriginalTimeText(trace, 1));
    }

    [Fact]
    public void SignalColorServiceUsesTenColorCycleAndParsesRgbHex()
    {
        var first = SignalColorService.GetDefaultPaletteColor(0);
        var eleventh = SignalColorService.GetDefaultPaletteColor(10);

        Assert.Equal(first, eleventh);
        Assert.Equal("#0072B2", SignalColorService.ToHex(first));

        Assert.True(SignalColorService.TryParseHexColor("#aabbCC", out var hashColor));
        Assert.Equal("#AABBCC", SignalColorService.ToHex(hashColor));

        Assert.True(SignalColorService.TryParseHexColor("112233", out var plainColor));
        Assert.Equal("#112233", SignalColorService.ToHex(plainColor));

        Assert.False(SignalColorService.TryParseHexColor("#12345", out _));
        Assert.False(SignalColorService.TryParseHexColor("#GGGGGG", out _));
    }
}
