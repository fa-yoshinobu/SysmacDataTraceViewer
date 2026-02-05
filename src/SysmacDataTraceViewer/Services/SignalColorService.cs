using OxyPlot;

namespace SysmacDataTraceViewer.Services;

public static class SignalColorService
{
    private static readonly OxyColor[] DefaultPalette =
    {
        OxyColor.FromRgb(0x00, 0x72, 0xB2), // blue
        OxyColor.FromRgb(0xD5, 0x5E, 0x00), // vermillion
        OxyColor.FromRgb(0x00, 0x9E, 0x73), // green
        OxyColor.FromRgb(0xCC, 0x79, 0xA7), // magenta
        OxyColor.FromRgb(0x56, 0xB4, 0xE9), // sky blue
        OxyColor.FromRgb(0xE6, 0x9F, 0x00), // orange
        OxyColor.FromRgb(0x00, 0x55, 0x7F), // dark blue
        OxyColor.FromRgb(0x7A, 0x01, 0x7A), // purple
        OxyColor.FromRgb(0x2E, 0x7D, 0x32), // dark green
        OxyColor.FromRgb(0xC6, 0x28, 0x28), // red
    };

    public static OxyColor GetDefaultPaletteColor(int index) =>
        DefaultPalette[index % DefaultPalette.Length];

    public static string ToHex(OxyColor color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryParseHexColor(string text, out OxyColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.StartsWith('#'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length != 6)
        {
            return false;
        }

        if (!byte.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) ||
            !byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) ||
            !byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return false;
        }

        color = OxyColor.FromRgb(r, g, b);
        return true;
    }
}
