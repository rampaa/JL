using System.Windows.Media;

namespace JL.Windows.Utilities;

internal readonly record struct FontInfo
{
    public FontFamily FontFamily { get; }
    public bool SupportsJapanese { get; }

    public FontInfo(FontFamily fontFamily, bool supportsJapanese)
    {
        FontFamily = fontFamily;
        SupportsJapanese = supportsJapanese;
    }
}
