using System.Globalization;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Windows.Utilities;

namespace JL.Windows;

internal static class DictOptionManager
{
    public static Brush POrthographyInfoColor { get; set; } = Brushes.Chocolate;
    public static Brush PitchAccentMarkerColor { get; set; } = Brushes.DeepSkyBlue;

    public static void ApplyDictOptions()
    {
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.JMdict, out Dict? jmdict))
        {
            POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(jmdict.Options?.POrthographyInfoColor?.Value
                    ?? ConfigManager.PrimarySpellingColor.ToString(CultureInfo.InvariantCulture))!;
        }

        else
        {
            POrthographyInfoColor = Brushes.Chocolate;
            POrthographyInfoColor.Freeze();
        }

        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchAccentDict))
        {
            PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentDict.Options?.PitchAccentMarkerColor?.Value
                ?? Brushes.DeepSkyBlue.ToString(CultureInfo.InvariantCulture))!;
        }

        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
            PitchAccentMarkerColor.Freeze();
        }
    }
}
