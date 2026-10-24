using System.Windows.Media;

namespace JL.Windows.Utilities;

internal static class ColorUtils
{
    public static Brush BrushFromHex(ReadOnlySpan<char> hex, Brush defaultBrush)
    {
        return TryParseColor(hex, out Color color) ? new SolidColorBrush(color) : defaultBrush;
    }

    public static Brush FrozenBrushFromHex(ReadOnlySpan<char> hex, Brush defaultBrush)
    {
        if (!TryParseColor(hex, out Color color))
        {
            return defaultBrush;
        }

        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    public static Color ColorFromHex(ReadOnlySpan<char> hex, Color defaultColor)
    {
        return TryParseColor(hex, out Color color) ? color : defaultColor;
    }

    public static bool TryParseColor(ReadOnlySpan<char> hex, out Color color)
    {
        color = default;
        if (hex.Length is not 9 || hex[0] is not '#')
        {
            return false;
        }

        int b = HexToByte(hex[7], hex[8]);
        int g = HexToByte(hex[5], hex[6]);
        int r = HexToByte(hex[3], hex[4]);
        int a = HexToByte(hex[1], hex[2]);
        if ((a | r | g | b) < 0)
        {
            return false;
        }

        color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
        return true;
    }

    private static int HexToByte(char highNibble, char lowNibble)
    {
        return (HexDigitToInt(highNibble) << 4) | HexDigitToInt(lowNibble);
    }

    private static int HexDigitToInt(char c)
    {
        return c is >= '0' and <= '9'
            ? c - '0'
            : c is >= 'A' and <= 'F'
                ? c - 'A' + 10
                : c is >= 'a' and <= 'f'
                    ? c - 'a' + 10
                    : -1;
    }
}
