using System.IO;
using System.Text;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class CommentCsvServiceTests
{
    [Fact]
    public void SaveAndLoadRoundTripsCommentCsvFields()
    {
        using var file = new TempFile();
        var boolSignals = new[]
        {
            new CommentSignalState("Machine,Start:BOOL", "Operator said \"go\"", true, "#112233", 2)
        };
        var valueSignals = new[]
        {
            new CommentSignalState("Counter:DINT", "Part count, line 1", false, string.Empty, 4)
        };

        CommentCsvService.Save(file.Path, boolSignals, valueSignals);
        var loaded = CommentCsvService.Load(file.Path);

        var loadedBool = Assert.Single(loaded.BoolSignals);
        Assert.Equal("Machine,Start:BOOL", loadedBool.Name);
        Assert.Equal("Operator said \"go\"", loadedBool.Comment);
        Assert.True(loadedBool.IsVisible);
        Assert.Equal("#112233", loadedBool.ColorHex);
        Assert.Equal(2, loadedBool.Order);

        var loadedValue = Assert.Single(loaded.ValueSignals);
        Assert.Equal("Counter:DINT", loadedValue.Name);
        Assert.Equal("Part count, line 1", loadedValue.Comment);
        Assert.False(loadedValue.IsVisible);
        Assert.Equal(string.Empty, loadedValue.ColorHex);
        Assert.Equal(4, loadedValue.Order);
    }

    [Fact]
    public void LoadUsesFallbacksForMissingOrInvalidFieldsAndIgnoresUnknownTypes()
    {
        using var file = new TempFile();
        File.WriteAllLines(
            file.Path,
            [
                "Type,Name,Comment,IsVisible,ColorHex,Order",
                "BOOL,flag:BOOL,Flag,false,#ABCDEF,",
                "VALUE,count:INT,Count,maybe,,not-an-int",
                "OTHER,ignored,Ignored,1,#000000,1"
            ],
            Encoding.UTF8);

        var loaded = CommentCsvService.Load(file.Path);

        var boolState = Assert.Single(loaded.BoolSignals);
        Assert.Equal("flag:BOOL", boolState.Name);
        Assert.False(boolState.IsVisible);
        Assert.Equal("#ABCDEF", boolState.ColorHex);
        Assert.Equal(int.MaxValue, boolState.Order);

        var valueState = Assert.Single(loaded.ValueSignals);
        Assert.Equal("count:INT", valueState.Name);
        Assert.True(valueState.IsVisible);
        Assert.Equal(string.Empty, valueState.ColorHex);
        Assert.Equal(int.MaxValue, valueState.Order);
    }

    private sealed class TempFile : IDisposable
    {
        private readonly string _directory;

        public TempFile()
        {
            _directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "SysmacDataTraceViewer.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "comments.csv");
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
