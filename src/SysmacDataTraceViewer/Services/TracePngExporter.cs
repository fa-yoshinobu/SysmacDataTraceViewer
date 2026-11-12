using System.IO;
using OxyPlot;
using OxyPlot.Wpf;

namespace SysmacDataTraceViewer.Services;

internal static class TracePngExporter
{
    public static void Export(PlotModel model, string fileName, bool visibleRangeOnly, IReadOnlyList<string> laneLabels, double dataMin, double dataMax)
    {
        var yAxis = model.DefaultYAxis;
        var xAxis = model.DefaultXAxis;
        Func<double, string>? originalFormatter = null;
        var originalMargins = model.PlotMargins;
        var originalMin = xAxis?.ActualMinimum ?? 0;
        var originalMax = xAxis?.ActualMaximum ?? 0;

        if (yAxis is not null && laneLabels.Count > 0)
        {
            originalFormatter = yAxis.LabelFormatter;
            yAxis.LabelFormatter = value =>
            {
                var laneIndex = (int)Math.Round(value);
                if (laneIndex < 0 || laneIndex >= laneLabels.Count)
                {
                    return string.Empty;
                }

                return laneLabels[laneIndex];
            };
        }

        model.PlotMargins = new OxyThickness(
            Math.Max(originalMargins.Left, 280),
            originalMargins.Top,
            originalMargins.Right,
            originalMargins.Bottom);

        if (!visibleRangeOnly && xAxis is not null)
        {
            xAxis.Zoom(dataMin, dataMax);
        }

        try
        {
            using var stream = File.Create(fileName);
            var exporter = new PngExporter { Width = 1800, Height = 900 };
            exporter.Export(model, stream);
        }
        finally
        {
            if (!visibleRangeOnly && xAxis is not null)
            {
                xAxis.Zoom(originalMin, originalMax);
            }

            if (yAxis is not null)
            {
                yAxis.LabelFormatter = originalFormatter;
            }

            model.PlotMargins = originalMargins;
            model.InvalidatePlot(false);
        }
    }
}
