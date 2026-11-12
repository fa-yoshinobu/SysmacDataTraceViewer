using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class TraceFileInputTests
{
    [Fact]
    public void AcceptsOneCsvAndRejectsMultipleFilesAndOtherExtensionsWithReasons()
    {
        Assert.True(TraceFileInput.TryGetDropPath(["fictional trace.CSV"], out var path, out var reason));
        Assert.Equal("fictional trace.CSV", path);
        Assert.Empty(reason);
        Assert.False(TraceFileInput.TryGetDropPath(["a.csv", "b.csv"], out path, out reason));
        Assert.Empty(path);
        Assert.Equal("Drop one CSV file at a time.", reason);
        Assert.False(TraceFileInput.TryGetDropPath(["a.exe"], out _, out reason));
        Assert.Equal("Only CSV files can be opened.", reason);
        Assert.False(TraceFileInput.TryGetDropPath([], out _, out _));
    }
}
