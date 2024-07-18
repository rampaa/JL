using System.Windows.Media;
using JL.Core.Dicts;
using JL.Windows.GUI;
using JL.Windows.Utilities;

namespace JL.Windows;

internal static class DictOptionManager
{
    public static Brush POrthographyInfoColor { get; set; } = ConfigManager.PrimarySpellingColor;
    public static Brush PitchAccentMarkerColor { get; set; } = Brushes.DeepSkyBlue;

    public static void ApplyDictOptions()
    {
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.JMdict, out Dict? jmdict))
        {
            string colorString = jmdict.Options.POrthographyInfoColor!.Value;
            POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(colorString);
        }
        else
        {
            POrthographyInfoColor = ConfigManager.PrimarySpellingColor;
        }

        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchAccentDict))
        {
            string colorString = pitchAccentDict.Options.PitchAccentMarkerColor!.Value;
            PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(colorString);

            MainWindow.Instance.Dispatcher.Invoke(() => PopupWindowUtils.SetStrokeDashArray(pitchAccentDict.Options.ShowPitchAccentWithDottedLines!.Value));
        }
        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
        }
    }
}
