using System.Diagnostics;
using System.IO;
using System.Text.Json;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;
using SysmacDataTraceViewer.Services;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var timer = Stopwatch.StartNew();
        var trace = CsvTraceParser.Parse(args[0]);
        var parseMs = timer.ElapsedMilliseconds;
        var parsedBytes = GC.GetTotalMemory(true);
        timer.Restart();
        var model = TracePlotModelBuilder.Build(trace, Enumerable.Range(0, trace.BoolSignals.Count).ToArray(), _ => OxyColors.Blue);
        var modelMs = timer.ElapsedMilliseconds;
        var modelBytes = GC.GetTotalMemory(true);
        timer.Restart();
        using (var png = File.Create(args[1]))
        {
            new PngExporter { Width = 1800, Height = 900 }.Export(model, png);
        }
        var renderMs = timer.ElapsedMilliseconds;
        using var process = Process.GetCurrentProcess();
        var result = new { trace.SampleCount, BoolCount = trace.BoolSignals.Count, ValueCount = trace.ValueSignals.Count,
            ParseMs = parseMs, ModelMs = modelMs, RenderMs = renderMs, ParsedBytes = parsedBytes, ModelBytes = modelBytes,
            Points = model.Series.Cast<StairStepSeries>().Sum(s => (long)s.Points.Count), PeakWorkingSetBytes = process.PeakWorkingSet64 };
        Console.WriteLine(JsonSerializer.Serialize(result));
    }
}
