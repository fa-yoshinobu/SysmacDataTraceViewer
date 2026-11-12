using System.IO;
using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace SysmacDataTraceViewer.Services;

internal static class AppErrorLog
{
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysmacDataTraceViewer", "logs");
    private static readonly BoundedErrorLog Writer = new(Path.Combine(DirectoryPath, "errors.log"));
    public static void Record(string operation, Exception error) => Writer.Record(operation, error);
}

internal sealed class BoundedErrorLog(string path, int maxBytes = 1024 * 1024)
{
    private readonly object _gate = new();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Last-resort logging must not throw while processing another failure.")]
    public void Record(string operation, Exception error)
    {
        try
        {
            var text = new StringBuilder($"{DateTimeOffset.Now:O} {operation}\n");
            // Exception messages can contain CSV names and values. Keep types, codes and stacks only.
            for (Exception? current = error; current is not null; current = current.InnerException)
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"{current.GetType().FullName} (0x{current.HResult:X8})");
                text.AppendLine(current.StackTrace);
                if (text.Length >= maxBytes)
                {
                    break;
                }
            }

            var bytes = new byte[Math.Min(maxBytes, Encoding.UTF8.GetByteCount(text.ToString()) + 1)];
            Encoding.UTF8.GetEncoder().Convert(text.ToString().AsSpan(), bytes.AsSpan(0, bytes.Length - 1), true, out _, out var written, out _);
            bytes[written++] = (byte)'\n';
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length + written > maxBytes)
                {
                    File.Move(path, path + ".1", true);
                }

                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                stream.Write(bytes, 0, written);
            }
        }
        catch (Exception)
        {
            // Logging must not turn an error into another unhandled error.
        }
    }
}
