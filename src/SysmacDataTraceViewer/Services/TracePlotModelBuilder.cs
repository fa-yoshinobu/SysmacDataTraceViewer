using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SysmacDataTraceViewer.Models;

namespace SysmacDataTraceViewer.Services;

internal static class TracePlotModelBuilder
{
    internal const double PlotLeftMargin = 16;
    internal const double PlotTopMargin = 8;
    internal const double PlotRightMargin = 12;
    internal const double PlotBottomMargin = 50;

    public static PlotModel Build(TraceData traceData, IReadOnlyList<int> visibleSignalIndexes, Func<int, OxyColor> signalColorProvider)
    {
        ArgumentNullException.ThrowIfNull(traceData);
        ArgumentNullException.ThrowIfNull(visibleSignalIndexes);
        ArgumentNullException.ThrowIfNull(signalColorProvider);

        var model = new PlotModel
        {
            PlotMargins = new OxyThickness(PlotLeftMargin, PlotTopMargin, PlotRightMargin, PlotBottomMargin),
            IsLegendVisible = false,
            Background = OxyColors.White
        };

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Elapsed Time [s]",
            StringFormat = "0.000",
            AbsoluteMinimum = traceData.ElapsedSeconds[0],
            AbsoluteMaximum = traceData.ElapsedSeconds[^1],
            IsPanEnabled = false,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot
        };
        model.Axes.Add(xAxis);

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = -0.5,
            Maximum = Math.Max(visibleSignalIndexes.Count - 0.5, 0.5),
            StartPosition = 1,
            EndPosition = 0,
            MajorStep = 1.0,
            MinorStep = 1.0,
            TickStyle = TickStyle.Outside,
            IsZoomEnabled = false,
            IsPanEnabled = false,
            LabelFormatter = static _ => string.Empty
        };
        model.Axes.Add(yAxis);

        for (var laneIndex = 0; laneIndex < visibleSignalIndexes.Count; laneIndex++)
        {
            var signalIndex = visibleSignalIndexes[laneIndex];
            var signal = traceData.BoolSignals[signalIndex];
            var series = new StairStepSeries
            {
                StrokeThickness = 2.4,
                Color = signalColorProvider(signalIndex)
            };

            for (var i = 0; i < traceData.SampleCount; i++)
            {
                var value = signal.Values[i];
                if (!value.HasValue)
                {
                    continue;
                }

                var y = laneIndex + (value.Value ? -0.32 : 0.32);
                series.Points.Add(new DataPoint(traceData.ElapsedSeconds[i], y));
            }

            model.Series.Add(series);
        }

        model.ResetAllAxes();
        return model;
    }

    public static PlotModel BuildEmpty()
    {
        var model = new PlotModel { Background = OxyColors.White };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Elapsed Time" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Signals" });
        return model;
    }
}
