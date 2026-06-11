using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class SignalOrderingServiceTests
{
    [Fact]
    public void BuildOrderedRowsReturnsNullWhenNoOrderIsProvided()
    {
        var rows = new[] { new NamedRow("a"), new NamedRow("b") };

        var ordered = SignalOrderingService.BuildOrderedRows(rows, [], static row => row.Name);

        Assert.Null(ordered);
    }

    [Fact]
    public void BuildOrderedRowsMovesNamedRowsFirstAndAppendsRemainingRowsInOriginalOrder()
    {
        var rows = new[] { new NamedRow("a"), new NamedRow("b"), new NamedRow("c"), new NamedRow("d") };

        var ordered = SignalOrderingService.BuildOrderedRows(rows, ["c", "missing", "a"], static row => row.Name);

        Assert.NotNull(ordered);
        Assert.Equal(["c", "a", "b", "d"], ordered.Select(static row => row.Name));
    }

    private sealed record NamedRow(string Name);
}
