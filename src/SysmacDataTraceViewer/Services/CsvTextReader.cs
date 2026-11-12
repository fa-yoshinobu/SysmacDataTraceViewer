using System.IO;
using System.Text;

namespace SysmacDataTraceViewer.Services;

internal static class CsvTextReader
{
    static CsvTextReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static StreamReader Open(string path, out string encodingName, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var stream = File.OpenRead(path);
        try
        {
            Span<byte> prefix = stackalloc byte[4];
            var count = stream.ReadAtLeast(prefix, 4, throwOnEndOfStream: false);
            var bytes = prefix[..count];
            Encoding encoding;
            var skip = 0;
            if (bytes.StartsWith(new byte[] { 0xff, 0xfe, 0, 0 }))
            {
                encoding = new UTF32Encoding(false, true, true);
                skip = 4;
            }
            else if (bytes.StartsWith(new byte[] { 0, 0, 0xfe, 0xff }))
            {
                encoding = new UTF32Encoding(true, true, true);
                skip = 4;
            }
            else if (bytes.StartsWith(new byte[] { 0xff, 0xfe }))
            {
                encoding = new UnicodeEncoding(false, true, true);
                skip = 2;
            }
            else if (bytes.StartsWith(new byte[] { 0xfe, 0xff }))
            {
                encoding = new UnicodeEncoding(true, true, true);
                skip = 2;
            }
            else
            {
                encoding = new UTF8Encoding(false, true);
                if (bytes.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
                {
                    skip = 3;
                }
                else
                {
                    stream.Position = 0;
                    try
                    {
                        using var validation = new StreamReader(stream, encoding, false, 4096, leaveOpen: true);
                        var buffer = new char[4096];
                        var lastPercent = -1;
                        while (validation.Read(buffer, 0, buffer.Length) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var percent = (int)(stream.Position * 10 / Math.Max(1, stream.Length));
                            if (percent != lastPercent)
                            {
                                progress?.Report(percent);
                                lastPercent = percent;
                            }
                        }
                    }
                    catch (DecoderFallbackException)
                    {
                        encoding = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                    }
                }
            }

            stream.Position = skip;
            cancellationToken.ThrowIfCancellationRequested();
            encodingName = encoding.CodePage == 932 ? "Shift_JIS" : encoding.WebName.ToUpperInvariant();
            return new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
}
