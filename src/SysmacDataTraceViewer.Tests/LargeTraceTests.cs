using System.IO;
using OxyPlot;
using OxyPlot.Series;
using SysmacDataTraceViewer.Models;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class LargeTraceTests
{
    [Fact]
    public void ReducedStepsMatchFullSampleGeometryIncludingMissingValues()
    {
        for (var pattern = 0; pattern < 729; pattern++)
        {
            var code = pattern;
            var values = new bool?[6];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = (code % 3) switch { 0 => (bool?)null, 1 => false, _ => true };
                code /= 3;
            }

            var trace = new TraceData
            {
                FileName = "fictional.csv",
                ElapsedSeconds = [0, 1, 2, 3, 4, 5],
                DateTexts = [],
                ClockTimeTexts = [],
                ValueSignals = [],
                BoolSignals = [new BoolSignal { Name = "Fictional:BOOL", Values = values, HasChange = true }]
            };
            var model = TracePlotModelBuilder.Build(trace, [0], _ => OxyColors.Blue);
            var reduced = Assert.IsType<StairStepSeries>(Assert.Single(model.Series)).Points;
            var full = values.Select((v, i) => v.HasValue ? new DataPoint(i, v.Value ? -0.32 : 0.32) : DataPoint.Undefined)
                .Where(p => !double.IsNaN(p.X)).ToList();
            for (var x = -0.5; x <= 5.5; x += 0.5)
            {
                Assert.Equal(StepValue(full, x), StepValue(reduced, x));
            }
        }
    }

    [Fact]
    public void ConstantSignalKeepsOnlyBothEndpoints()
    {
        var trace = new TraceData
        {
            FileName = "fictional.csv",
            ElapsedSeconds = Enumerable.Range(0, 10000).Select(i => (double)i).ToArray(),
            DateTexts = [],
            ClockTimeTexts = [],
            ValueSignals = [],
            BoolSignals = [new BoolSignal { Name = "Fictional:BOOL", Values = Enumerable.Repeat<bool?>(true, 10000).ToArray(), HasChange = false }]
        };
        var model = TracePlotModelBuilder.Build(trace, [0], _ => OxyColors.Blue);
        Assert.Equal(2, Assert.IsType<StairStepSeries>(Assert.Single(model.Series)).Points.Count);
    }

    [Fact]
    public void ParsingCanBeCanceledAfterProgressAndSharesRepeatedStrings()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "Index,Date,ClockTime,Fictional:STRING\n"
                + string.Join('\n', Enumerable.Range(0, 3000).Select(i => $"{i},2026/01/01,00:00:00,Repeated value")));
            using var cancellation = new CancellationTokenSource();
            var progress = new CallbackProgress(p => { if (p > 10) cancellation.Cancel(); });
            Assert.Throws<OperationCanceledException>(() => CsvTraceParser.Parse(path, progress, cancellation.Token));
            var trace = CsvTraceParser.Parse(path);
            Assert.Same(trace.DateTexts[0], trace.DateTexts[1]);
            Assert.Same(trace.ClockTimeTexts[0], trace.ClockTimeTexts[1]);
            Assert.Same(trace.ValueSignals[0].Values[0], trace.ValueSignals[0].Values[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static double StepValue(List<DataPoint> points, double x)
    {
        if (points.Count == 0 || x < points[0].X || x > points[^1].X)
        {
            return double.NaN;
        }

        return points.Last(p => p.X <= x).Y;
    }

    private sealed class CallbackProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}
