using OxyPlot;
using OxyPlot.Annotations;
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

    // Shared with the name lane so its rows line up with the chart lanes.
    internal static readonly OxyColor LaneStripeColor = OxyColor.FromRgb(0xF3, 0xF5, 0xF8);
    private static readonly OxyColor AxisTextColor = OxyColor.FromRgb(0x4B, 0x55, 0x63);
    private static readonly OxyColor AxisLineColor = OxyColor.FromRgb(0xB8, 0xC0, 0xCA);
    private static readonly OxyColor MajorGridColor = OxyColor.FromRgb(0xE1, 0xE5, 0xEA);
    private static readonly OxyColor MinorGridColor = OxyColor.FromRgb(0xEE, 0xF1, 0xF4);

    public static PlotModel Build(TraceData traceData, IReadOnlyList<int> visibleSignalIndexes, Func<int, OxyColor> signalColorProvider)
    {
        ArgumentNullException.ThrowIfNull(traceData);
        ArgumentNullException.ThrowIfNull(visibleSignalIndexes);
        ArgumentNullException.ThrowIfNull(signalColorProvider);

        var model = new PlotModel
        {
            PlotMargins = new OxyThickness(PlotLeftMargin, PlotTopMargin, PlotRightMargin, PlotBottomMargin),
            IsLegendVisible = false,
            Background = OxyColors.White,
            PlotAreaBorderColor = AxisLineColor,
            TextColor = AxisTextColor
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
            MinorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = MajorGridColor,
            MinorGridlineColor = MinorGridColor,
            TicklineColor = AxisLineColor,
            AxislineColor = AxisLineColor
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
            LabelFormatter = static _ => string.Empty,
            TicklineColor = AxisLineColor
        };
        model.Axes.Add(yAxis);

        // Light stripes on every other lane make long traces easier to follow across the chart.
        for (var laneIndex = 1; laneIndex < visibleSignalIndexes.Count; laneIndex += 2)
        {
            model.Annotations.Add(new RectangleAnnotation
            {
                MinimumY = laneIndex - 0.5,
                MaximumY = laneIndex + 0.5,
                Fill = LaneStripeColor,
                Stroke = OxyColors.Transparent,
                StrokeThickness = 0,
                Layer = AnnotationLayer.BelowSeries
            });
        }

        for (var laneIndex = 0; laneIndex < visibleSignalIndexes.Count; laneIndex++)
        {
            var signalIndex = visibleSignalIndexes[laneIndex];
            var signal = traceData.BoolSignals[signalIndex];
            var series = new StairStepSeries
            {
                StrokeThickness = 2.4,
                Color = signalColorProvider(signalIndex)
            };

            var lastValidIndex = -1;
            var lastAddedIndex = -1;
            bool? previous = null;
            for (var i = 0; i < traceData.SampleCount; i++)
            {
                var value = signal.Values[i];
                if (!value.HasValue)
                {
                    continue;
                }

                lastValidIndex = i;
                if (previous != value)
                {
                    var y = laneIndex + (value.Value ? -0.32 : 0.32);
                    series.Points.Add(new DataPoint(traceData.ElapsedSeconds[i], y));
                    lastAddedIndex = i;
                    previous = value;
                }
            }

            // Missing samples were previously skipped; preserve that interpolation and the final endpoint.
            if (lastValidIndex > lastAddedIndex)
            {
                series.Points.Add(new DataPoint(traceData.ElapsedSeconds[lastValidIndex], laneIndex + (previous == true ? -0.32 : 0.32)));
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
