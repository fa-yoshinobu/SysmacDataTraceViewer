using System.IO;
using System.Text;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class CsvTraceParserTests
{
    [Fact]
    public void ParseFindsHeaderClassifiesColumnsAndHandlesMidnightRollover()
    {
        var csv = string.Join(
            Environment.NewLine,
            "Sysmac Studio export",
            "Generated,metadata",
            "Index,Date,ClockTime,input:BOOL,count:INT,ratio:REAL,duration:TIME",
            "0,2026/02/05,23:59:59.500,0,10,1.25,T#1s",
            "1,2026/02/06,00:00:00.250,true,invalid,not-real,T#2s",
            "2,2026/02/06,00:00:01.000,1,12,2.5,");

        using var file = new TempCsvFile(csv);
        var trace = CsvTraceParser.Parse(file.Path);

        Assert.Equal("trace.csv", trace.FileName);
        Assert.Equal([0.0, 0.75, 1.5], trace.ElapsedSeconds);
        Assert.Equal(["2026/02/05", "2026/02/06", "2026/02/06"], trace.DateTexts);
        Assert.Equal(["23:59:59.500", "00:00:00.250", "00:00:01.000"], trace.ClockTimeTexts);

        var boolSignal = Assert.Single(trace.BoolSignals);
        Assert.Equal("input:BOOL", boolSignal.Name);
        Assert.True(boolSignal.HasChange);
        Assert.Equal([false, true, true], boolSignal.Values);

        Assert.Collection(
            trace.ValueSignals,
            signal =>
            {
                Assert.Equal("count:INT", signal.Name);
                Assert.True(signal.HasChange);
                Assert.Equal(["10", null, "12"], signal.Values);
            },
            signal =>
            {
                Assert.Equal("ratio:REAL", signal.Name);
                Assert.True(signal.HasChange);
                Assert.Equal(["1.25", null, "2.5"], signal.Values);
            },
            signal =>
            {
                Assert.Equal("duration:TIME", signal.Name);
                Assert.True(signal.HasChange);
                Assert.Equal(["T#1s", "T#2s", null], signal.Values);
            });
    }

    [Fact]
    public void ParseAcceptsClockTimeWithoutDateAndMinuteSecondFormat()
    {
        var csv = string.Join(
            Environment.NewLine,
            "Index,ClockTime,flag:BOOL,value:INT",
            "0,01:02.500,1,42",
            "1,01:03.000,false,43");

        using var file = new TempCsvFile(csv);
        var trace = CsvTraceParser.Parse(file.Path);

        Assert.Equal([0.0, 0.5], trace.ElapsedSeconds);
        Assert.Equal([string.Empty, string.Empty], trace.DateTexts);
        Assert.Equal(["01:02.500", "01:03.000"], trace.ClockTimeTexts);
        Assert.Equal([true, false], trace.BoolSignals[0].Values);
        Assert.Equal(["42", "43"], trace.ValueSignals[0].Values);
    }

    [Fact]
    public void ParseTreatsEmptyInvalidBoolAndInvalidNumericValuesAsMissing()
    {
        var csv = string.Join(
            Environment.NewLine,
            "Index,Date,ClockTime,flag:BOOL,count:INT,total:UDINT,ratio:LREAL",
            "0,,00:00:00.000,, ,bad,abc",
            "1,,00:00:00.100,maybe,-1,-2,1.0");

        using var file = new TempCsvFile(csv);
        var trace = CsvTraceParser.Parse(file.Path);

        Assert.Equal([null, null], trace.BoolSignals[0].Values);
        Assert.Equal([null, "-1"], trace.ValueSignals[0].Values);
        Assert.Equal([null, null], trace.ValueSignals[1].Values);
        Assert.Equal([null, "1"], trace.ValueSignals[2].Values);
    }

    [Fact]
    public void ParseThrowsWhenHeaderOrSignalColumnsAreMissing()
    {
        using var noHeader = new TempCsvFile("not,index,clock" + Environment.NewLine + "0,00:00:00.000");
        using var noSignals = new TempCsvFile("Index,Date,ClockTime" + Environment.NewLine + "0,,00:00:00.000");

        Assert.Throws<InvalidDataException>(() => CsvTraceParser.Parse(noHeader.Path));
        Assert.Throws<InvalidDataException>(() => CsvTraceParser.Parse(noSignals.Path));
    }

    private sealed class TempCsvFile : IDisposable
    {
        private readonly string _directory;

        public TempCsvFile(string content)
        {
            _directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "SysmacDataTraceViewer.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "trace.csv");
            File.WriteAllText(Path, content, Encoding.UTF8);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
