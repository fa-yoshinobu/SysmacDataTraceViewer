using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class TracePlotModelBuilderTests
{
    [Fact]
    public void BuildCreatesAxesAndVisibleBoolSeries()
    {
        var trace = CreateTrace();

        var model = TracePlotModelBuilder.Build(trace, [1, 0], signalIndex => signalIndex == 1 ? OxyColors.Red : OxyColors.Blue);

        Assert.Equal(OxyColors.White, model.Background);
        Assert.False(model.IsLegendVisible);
        Assert.Equal(TracePlotModelBuilder.PlotLeftMargin, model.PlotMargins.Left);
        Assert.Equal(TracePlotModelBuilder.PlotTopMargin, model.PlotMargins.Top);
        Assert.Equal(TracePlotModelBuilder.PlotRightMargin, model.PlotMargins.Right);
        Assert.Equal(TracePlotModelBuilder.PlotBottomMargin, model.PlotMargins.Bottom);

        var xAxis = Assert.IsType<LinearAxis>(model.Axes[0]);
        Assert.Equal(AxisPosition.Bottom, xAxis.Position);
        Assert.Equal("Elapsed Time [s]", xAxis.Title);
        Assert.Equal("0.000", xAxis.StringFormat);
        Assert.Equal(0, xAxis.AbsoluteMinimum);
        Assert.Equal(2, xAxis.AbsoluteMaximum);
        Assert.False(xAxis.IsPanEnabled);
        Assert.Equal(LineStyle.Solid, xAxis.MajorGridlineStyle);
        Assert.Equal(LineStyle.Dot, xAxis.MinorGridlineStyle);

        var yAxis = Assert.IsType<LinearAxis>(model.Axes[1]);
        Assert.Equal(AxisPosition.Left, yAxis.Position);
        Assert.Equal(-0.5, yAxis.Minimum);
        Assert.Equal(1.5, yAxis.Maximum);
        Assert.Equal(1, yAxis.StartPosition);
        Assert.Equal(0, yAxis.EndPosition);
        Assert.Equal(string.Empty, yAxis.LabelFormatter(0));

        Assert.Equal(2, model.Series.Count);
        var firstSeries = Assert.IsType<StairStepSeries>(model.Series[0]);
        Assert.Equal(OxyColors.Red, firstSeries.Color);
        Assert.Equal(2, firstSeries.Points.Count);
        AssertPoint(firstSeries.Points[0], 0, -0.32);
        AssertPoint(firstSeries.Points[1], 2, 0.32);

        var secondSeries = Assert.IsType<StairStepSeries>(model.Series[1]);
        Assert.Equal(OxyColors.Blue, secondSeries.Color);
        Assert.Equal(3, secondSeries.Points.Count);
        AssertPoint(secondSeries.Points[0], 0, 1.32);
        AssertPoint(secondSeries.Points[1], 1, 0.68);
        AssertPoint(secondSeries.Points[2], 2, 0.68);
    }

    [Fact]
    public void BuildKeepsStableYAxisWhenNoBoolSignalsAreVisible()
    {
        var trace = CreateTrace();

        var model = TracePlotModelBuilder.Build(trace, [], _ => OxyColors.Black);

        Assert.Empty(model.Series);
        var yAxis = Assert.IsType<LinearAxis>(model.Axes[1]);
        Assert.Equal(-0.5, yAxis.Minimum);
        Assert.Equal(0.5, yAxis.Maximum);
    }

    [Fact]
    public void BuildEmptyCreatesPlaceholderPlot()
    {
        var model = TracePlotModelBuilder.BuildEmpty();

        Assert.Equal(OxyColors.White, model.Background);
        Assert.Equal(2, model.Axes.Count);
        Assert.Equal("Elapsed Time", model.Axes[0].Title);
        Assert.Equal("Signals", model.Axes[1].Title);
    }

    private static TraceData CreateTrace() =>
        new()
        {
            FileName = "trace.csv",
            ElapsedSeconds = [0, 1, 2],
            DateTexts = ["", "", ""],
            ClockTimeTexts = ["0", "1", "2"],
            BoolSignals =
            [
                new BoolSignal { Name = "Machine.Start:BOOL", Values = [false, true, true], HasChange = true },
                new BoolSignal { Name = "Machine.Ready:BOOL", Values = [true, null, false], HasChange = true }
            ],
            ValueSignals = []
        };

    private static void AssertPoint(DataPoint point, double expectedX, double expectedY)
    {
        Assert.Equal(expectedX, point.X);
        Assert.Equal(expectedY, point.Y, precision: 8);
    }
}
