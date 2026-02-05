using SysmacDataTraceViewer.Models;
using System.Globalization;
using System.IO;

namespace SysmacDataTraceViewer.Services;

public static class CsvTraceParser
{
    public static TraceData Parse(string path)
    {
        using var reader = new StreamReader(path);

        List<string>? headers = null;
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parsed = CsvLineParser.Parse(line);
            if (parsed.Count > 0 &&
                parsed[0].Equals("Index", StringComparison.OrdinalIgnoreCase) &&
                parsed.Any(static c => c.Equals("ClockTime", StringComparison.OrdinalIgnoreCase)))
            {
                headers = parsed;
                break;
            }
        }

        if (headers is null)
        {
            throw new InvalidDataException("Header row was not found. The CSV format is not supported.");
        }

        if (headers.Count < 2)
        {
            throw new InvalidDataException("Header row is invalid.");
        }

        var clockTimeIndex = headers.FindIndex(static h => h.Equals("ClockTime", StringComparison.OrdinalIgnoreCase));
        if (clockTimeIndex < 0)
        {
            throw new InvalidDataException("ClockTime column was not found.");
        }

        var dateIndex = headers.FindIndex(static h => h.Equals("Date", StringComparison.OrdinalIgnoreCase));

        var boolColumns = new List<(int Index, string Name)>();
        var valueColumns = new List<(int Index, string Name, string TypeName)>();
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i].Contains(":BOOL", StringComparison.OrdinalIgnoreCase))
            {
                boolColumns.Add((i, headers[i]));
            }
            else if (headers[i].Contains(':'))
            {
                valueColumns.Add((i, headers[i], ExtractTypeName(headers[i])));
            }
        }

        if (boolColumns.Count == 0 && valueColumns.Count == 0)
        {
            throw new InvalidDataException("No BOOL or value columns were found.");
        }

        var elapsedSeconds = new List<double>();
        var dateTexts = new List<string>();
        var clockTexts = new List<string>();
        var boolData = boolColumns.Select(static _ => new List<bool?>()).ToList();
        var valueData = valueColumns.Select(static _ => new List<string?>()).ToList();

        TimeSpan? previousClock = null;
        string? previousDateText = null;
        var elapsedTotalSeconds = 0.0;
        while (reader.ReadLine() is { } rowLine)
        {
            if (string.IsNullOrWhiteSpace(rowLine))
            {
                continue;
            }

            var cols = CsvLineParser.Parse(rowLine);
            if (cols.Count <= clockTimeIndex)
            {
                continue;
            }

            if (!TryParseClockTime(cols[clockTimeIndex], out var clock))
            {
                continue;
            }

            var currentDateText = dateIndex >= 0 && dateIndex < cols.Count ? cols[dateIndex] : string.Empty;
            if (previousClock.HasValue)
            {
                var delta = (clock - previousClock.Value).TotalSeconds;
                if (delta < 0)
                {
                    var dateChanged = !string.IsNullOrWhiteSpace(currentDateText) &&
                        !string.Equals(previousDateText, currentDateText, StringComparison.Ordinal);
                    var likelyMidnightRollover = delta < -TimeSpan.FromHours(12).TotalSeconds;
                    if (dateChanged || likelyMidnightRollover)
                    {
                        // Handle midnight rollover (23:xx -> 00:xx) as continuous elapsed time.
                        delta += TimeSpan.FromDays(1).TotalSeconds;
                    }
                }

                // Guard against malformed/out-of-order rows.
                elapsedTotalSeconds += Math.Max(delta, 0);
            }

            elapsedSeconds.Add(elapsedTotalSeconds);
            previousClock = clock;
            previousDateText = currentDateText;
            dateTexts.Add(currentDateText);
            clockTexts.Add(cols[clockTimeIndex]);

            for (var i = 0; i < boolColumns.Count; i++)
            {
                var index = boolColumns[i].Index;
                boolData[i].Add(index < cols.Count ? ParseBool(cols[index]) : null);
            }

            for (var i = 0; i < valueColumns.Count; i++)
            {
                var column = valueColumns[i];
                valueData[i].Add(column.Index < cols.Count ? ParseValue(cols[column.Index], column.TypeName) : null);
            }
        }

        if (elapsedSeconds.Count == 0)
        {
            throw new InvalidDataException("No data rows could be parsed.");
        }

        return new TraceData
        {
            FileName = Path.GetFileName(path),
            ElapsedSeconds = elapsedSeconds.ToArray(),
            DateTexts = dateTexts.ToArray(),
            ClockTimeTexts = clockTexts.ToArray(),
            BoolSignals = boolColumns.Select((c, i) => new BoolSignal
            {
                Name = c.Name,
                Values = boolData[i].ToArray(),
                HasChange = HasBoolChange(boolData[i])
            }).ToList(),
            ValueSignals = valueColumns.Select((c, i) => new ValueSignal
            {
                Name = c.Name,
                Values = valueData[i].ToArray(),
                HasChange = HasValueChange(valueData[i])
            }).ToList()
        };
    }

    private static bool HasBoolChange(IReadOnlyList<bool?> values)
    {
        bool? last = null;
        var hasLast = false;
        foreach (var value in values)
        {
            if (!value.HasValue)
            {
                continue;
            }

            if (!hasLast)
            {
                last = value.Value;
                hasLast = true;
                continue;
            }

            if (last != value.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasValueChange(IReadOnlyList<string?> values)
    {
        string? last = null;
        var hasLast = false;
        foreach (var value in values)
        {
            var normalized = NormalizeValue(value);
            if (normalized is null)
            {
                continue;
            }

            if (!hasLast)
            {
                last = normalized;
                hasLast = true;
                continue;
            }

            if (!string.Equals(last, normalized, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool? ParseBool(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        if (trimmed == "0")
        {
            return false;
        }

        if (trimmed == "1")
        {
            return true;
        }

        if (bool.TryParse(trimmed, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string? ParseValue(string text, string typeName)
    {
        var normalized = NormalizeValue(text);
        if (normalized is null)
        {
            return null;
        }

        if (typeName.Equals("REAL", StringComparison.OrdinalIgnoreCase) ||
            typeName.Equals("LREAL", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedInvariant))
            {
                return parsedInvariant.ToString("G17", CultureInfo.InvariantCulture);
            }

            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedCurrent))
            {
                return parsedCurrent.ToString("G17", CultureInfo.InvariantCulture);
            }
        }

        return normalized;
    }

    private static string? NormalizeValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Trim();
    }

    private static string ExtractTypeName(string headerName)
    {
        var typeSeparator = headerName.LastIndexOf(':');
        if (typeSeparator < 0 || typeSeparator == headerName.Length - 1)
        {
            return string.Empty;
        }

        return headerName[(typeSeparator + 1)..];
    }

    private static bool TryParseClockTime(string value, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var parts = trimmed.Split(':');
        if (parts.Length == 3)
        {
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return false;
            }

            result = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            return true;
        }

        // Excel export can drop leading hours and produce mm:ss.s format.
        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
            {
                return false;
            }

            result = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
            return true;
        }

        return false;
    }

}
