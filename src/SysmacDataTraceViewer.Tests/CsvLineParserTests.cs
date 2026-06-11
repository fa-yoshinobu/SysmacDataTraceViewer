using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class CsvLineParserTests
{
    [Fact]
    public void ParseHandlesQuotedEscapedAndEmptyFields()
    {
        var values = CsvLineParser.Parse("alpha,\"bravo, charlie\",\"he said \"\"hi\"\"\",,tail,");

        Assert.Equal(
            ["alpha", "bravo, charlie", "he said \"hi\"", string.Empty, "tail", string.Empty],
            values);
    }
}
