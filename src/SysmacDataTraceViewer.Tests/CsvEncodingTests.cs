using System.IO;
using System.Text;
using SysmacDataTraceViewer.Services;
using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class CsvEncodingTests
{
    [Theory]
    [InlineData(65001, false, "UTF-8")]
    [InlineData(65001, true, "UTF-8")]
    [InlineData(932, false, "Shift_JIS")]
    [InlineData(1200, true, "UTF-16")]
    [InlineData(12000, true, "UTF-32")]
    public void JapaneseTraceAndCommentsUseTheSameNames(int codePage, bool bom, string name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = codePage == 65001 ? new UTF8Encoding(bom) : Encoding.GetEncoding(codePage);
        var directory = Path.Combine(Path.GetTempPath(), "SysmacEncoding_" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var tracePath = Path.Combine(directory, "trace.csv");
        var commentPath = Path.Combine(directory, "comments.csv");
        try
        {
            File.WriteAllText(tracePath, "Index,ClockTime,架空設備.運転中:BOOL\n0,00:00:00,1\n", encoding);
            File.WriteAllText(commentPath, "Type,Name,Comment\nBOOL,架空設備.運転中:BOOL,日本語のコメント\n", encoding);
            var trace = CsvTraceParser.Parse(tracePath);
            var comments = CommentCsvService.Load(commentPath);
            Assert.Equal("架空設備.運転中:BOOL", Assert.Single(trace.BoolSignals).Name);
            var comment = Assert.Single(comments.BoolSignals);
            Assert.Equal(trace.BoolSignals[0].Name, comment.Name);
            Assert.Equal("日本語のコメント", comment.Comment);
            Assert.Equal(name, trace.EncodingName);
            Assert.Equal(name, comments.EncodingName);
        }
        finally
        {
            File.Delete(tracePath);
            File.Delete(commentPath);
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void Utf8BomDoesNotSilentlyReplaceInvalidBytes()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0xef, 0xbb, 0xbf, 0xff]);
            using var reader = CsvTextReader.Open(path, out _);
            Assert.Throws<DecoderFallbackException>(() => reader.ReadToEnd());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
