namespace SysmacDataTraceViewer.Services;

internal static class CsvLineParser
{
    public static IEnumerable<string> ReadRecords(System.IO.TextReader reader)
    {
        var record = new System.Text.StringBuilder();
        var inQuotes = false;
        while (reader.Read() is var next && next >= 0)
        {
            var c = (char)next;
            if (c == '"')
            {
                record.Append(c);
                if (inQuotes && reader.Peek() == '"') record.Append((char)reader.Read());
                else inQuotes = !inQuotes;
            }
            else if (!inQuotes && c is '\r' or '\n')
            {
                if (c == '\r' && reader.Peek() == '\n') reader.Read();
                yield return record.ToString();
                record.Clear();
            }
            else record.Append(c);
        }
        if (inQuotes) throw new System.IO.InvalidDataException("Comment CSV contains an unterminated quoted field.");
        if (record.Length > 0) yield return record.ToString();
    }

    public static List<string> Parse(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }
}
