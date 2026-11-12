using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SysmacDataTraceViewer.Services;

internal sealed record SmcCommentImportResult(Dictionary<string, string> Comments, int MatchedCount, int TotalCount, int NoCommentCount)
{
    public string Summary => $"smc2 comments: {Comments.Count}/{TotalCount}; no comment: {NoCommentCount}; unmatched: {TotalCount - MatchedCount}";
}

// Uses the SLWD tables and member-comment resolution from SysmacVariableBackupViewer.
internal static partial class SmcCommentImporter
{
    private sealed record Symbol(string Type, string Comment, Dictionary<string, string> Elements);

    public static SmcCommentImportResult Load(string path, IReadOnlyList<string> signalNames, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(path);
        SmcCommentImportResult? best = null;
        var tied = false;
        foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".oem", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = entry.Open();
            var doc = ReadXml(stream);
            var folder = entry.FullName[..(entry.FullName.LastIndexOf('/') + 1)];
            foreach (var device in doc.Descendants("Entity").Where(e => (string?)e.Attribute("type") == "Device"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // HMI and Safety devices share the catalogue but do not use PLC SLWD tables.
                if ((string?)device.Attribute("subtype") is "NA" or "NES") continue;
                var metadata = ReadDevice(archive, folder, device, cancellationToken);
                if (metadata is null) continue;
                var candidate = metadata.Match(signalNames, cancellationToken);
                if (best is null || candidate.MatchedCount > best.MatchedCount)
                {
                    best = candidate;
                    tied = false;
                }
                else if (candidate.MatchedCount == best.MatchedCount) tied = true;
            }
        }
        if (best is null || best.MatchedCount == 0)
            throw new InvalidDataException("No project device matches the loaded CSV signals. Existing comments were kept.");
        if (tied)
            throw new InvalidDataException("Multiple project devices match the CSV equally. The target PLC cannot be identified; existing comments were kept.");
        return best;
    }

    private sealed class Metadata
    {
        public Dictionary<string, Dictionary<string, Symbol>> Programs { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, Symbol>> Types { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Symbol> Globals { get; } = new(StringComparer.OrdinalIgnoreCase);

        public SmcCommentImportResult Match(IReadOnlyList<string> signalNames, CancellationToken cancellationToken)
        {
            var comments = new Dictionary<string, string>(StringComparer.Ordinal);
            var matched = 0;
            var noComment = 0;
            foreach (var rawName in signalNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = UiFormattingService.FormatVariableName(rawName, false);
                var separator = path.IndexOfAny(['.', '/']);
                var symbols = Globals;
                if (separator > 0 && Programs.TryGetValue(path[..separator], out var locals))
                {
                    symbols = locals;
                    path = path[(separator + 1)..];
                }
                var (text, resolved) = ResolveComment(symbols, path);
                if (!resolved) continue;
                matched++;
                if (string.IsNullOrWhiteSpace(text)) noComment++;
                else comments.Add(rawName, text);
            }
            return new SmcCommentImportResult(comments, matched, signalNames.Count, noComment);
        }

        private (string Text, bool Resolved) ResolveComment(Dictionary<string, Symbol> symbols, string path)
        {
            var parts = PathParts().Split(path);
            var comments = new List<string>();
            var resolved = false;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var bracket = part.IndexOf('[', StringComparison.Ordinal);
                var name = bracket < 0 ? part : part[..bracket];
                if (!symbols.TryGetValue(name, out var symbol)) break;
                var suffix = bracket < 0 ? "" : part[bracket..];
                var remaining = suffix + (i + 1 < parts.Length
                    ? (suffix.Length > 0 ? "." : "") + string.Join(".", parts.Skip(i + 1)) : "");
                if (symbol.Elements.TryGetValue(remaining, out var exact) && !string.IsNullOrWhiteSpace(exact))
                {
                    comments.Add(exact);
                    resolved = true;
                    break;
                }
                var comment = symbol.Elements.GetValueOrDefault(suffix, symbol.Comment);
                resolved = i == parts.Length - 1;
                if (!string.IsNullOrWhiteSpace(comment)) comments.Add(comment);
                var type = ArrayType().Replace(symbol.Type, "");
                if (!Types.TryGetValue(type, out var members)) break;
                // FB paths may repeat the instance name before its members.
                if (i + 1 < parts.Length && parts[i + 1].Equals(part, StringComparison.OrdinalIgnoreCase)
                    && !members.ContainsKey(name)) i++;
                symbols = members;
            }
            return (string.Join(" / ", comments.Distinct(StringComparer.Ordinal)), resolved);
        }
    }

    private static Metadata? ReadDevice(ZipArchive archive, string folder, XElement device, CancellationToken cancellationToken)
    {
        var result = new Metadata();
        var entities = device.Descendants("Entity")
            .Where(e => e.Ancestors("Entity").FirstOrDefault(a => (string?)a.Attribute("type") == "Device") == device);
        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = (string?)entity.Attribute("type");
            if (kind is "Program" or "FunctionBlock" or "Function")
            {
                var symbols = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                foreach (var table in entity.Element("ChildEntities")?.Elements("Entity")
                    .Where(e => (string?)e.Attribute("type") == "Variables") ?? [])
                    if (!ReadSymbols(archive, folder, table, symbols, cancellationToken)) return null;
                var name = (string?)entity.Attribute("name") ?? "";
                if (kind == "Program") result.Programs.Add(name, symbols);
                else result.Types.Add(name, symbols);
            }
            else if (kind == "Variables" && (string?)entity.Attribute("subtype") == "Global")
            {
                if (!ReadSymbols(archive, folder, entity, result.Globals, cancellationToken)) return null;
            }
            else if (kind == "DataType")
            {
                var entry = archive.GetEntry(folder + (string?)entity.Attribute("id") + ".xml");
                if (entry is null) continue;
                using var stream = entry.Open();
                foreach (var type in ReadXml(stream).Descendants("DataType")
                    .Where(e => e.Elements("DataType").Any() && (string?)e.Attribute("BaseType") == "STRUCT"))
                {
                    var members = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                    foreach (var member in type.Elements("DataType"))
                        members.Add((string?)member.Attribute("Name") ?? "", new Symbol(
                            (string?)member.Attribute("BaseType") ?? "", (string?)member.Attribute("Comment") ?? "", new(StringComparer.OrdinalIgnoreCase)));
                    result.Types.Add((string?)type.Attribute("Name") ?? "", members);
                }
            }
        }
        return result;
    }

    private static bool ReadSymbols(ZipArchive archive, string folder, XElement entity, Dictionary<string, Symbol> symbols, CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(folder + (string?)entity.Attribute("id") + ".xml")
            ?? throw new InvalidDataException("A project variable table is missing.");
        using var reader = new StreamReader(entry.Open());
        // Safety/HMI tables use other formats and are not PLC comment candidates.
        if (reader.ReadLine()?.TrimStart('\uFEFF') != "[SLWD version=1.0]") return false;
        while (reader.ReadLine() is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!line.StartsWith("++D=", StringComparison.Ordinal)) continue;
            var fields = line.Split('\t').Select(f => f.Split('=', 2))
                .Where(f => f.Length == 2).ToDictionary(f => f[0], f => f[1], StringComparer.Ordinal);
            if (!fields.TryGetValue("N", out var name) || fields.GetValueOrDefault("G") == "VAR_EXTERNAL") continue;
            var elements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (fields.TryGetValue("EC", out var xml))
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Decode(xml)));
                foreach (var item in ReadXml(stream).Descendants("EC"))
                    elements.Add((string?)item.Attribute("EK") ?? "", (string?)item.Attribute("C") ?? "");
            }
            symbols.Add(Decode(name), new Symbol(Decode(fields["++D"]), Decode(fields.GetValueOrDefault("Com", "")), elements));
        }
        return true;
    }

    private static XDocument ReadXml(Stream stream)
    {
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader);
    }

    private static string Decode(string value) => Escapes().Replace(value,
        m => m.Groups[1].Value switch { "$" => "$", "t" => "\t", "r" => "\r", "n" => "\n", _ => m.Value });

    [GeneratedRegex(@"\$(\$|t|r|n)", RegexOptions.None, 1000)]
    private static partial Regex Escapes();
    [GeneratedRegex(@"[/.](?![^\[]*\])", RegexOptions.None, 1000)]
    private static partial Regex PathParts();
    [GeneratedRegex(@"^ARRAY\s*\[.*?\]\s+OF\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ArrayType();
}
