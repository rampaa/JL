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
        Dict? jmdict = DictUtils.Dicts.Values.FirstOrDefault(static dict => dict.Type is DictType.JMdict);
        if (jmdict is not null)
        {
            POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(jmdict.Options?.POrthographyInfoColor?.Value
                                                                    ?? ConfigManager.PrimarySpellingColor.ToString(CultureInfo.InvariantCulture))!;
        }

        else
        {
            POrthographyInfoColor = Brushes.Chocolate;
            POrthographyInfoColor.Freeze();
        }

        Dict? pitchAccentDict = DictUtils.Dicts.Values.FirstOrDefault(static dict => dict.Type is DictType.PitchAccentYomichan);
        if (pitchAccentDict is not null)
        {
            if (pitchAccentDict.Options?.PitchAccentMarkerColor is not null)
            {
                PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentDict.Options.PitchAccentMarkerColor.Value.Value)!;
            }
        }

        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
            PitchAccentMarkerColor.Freeze();
        }
    }
}
