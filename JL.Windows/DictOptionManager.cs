using System.Windows.Media;
using JL.Core.Dicts;
using JL.Windows.GUI;
using JL.Windows.Utilities;

namespace JL.Windows;

internal static class DictOptionManager
{
    public static Brush POrthographyInfoColor { get; set; } = ConfigManager.Instance.PrimarySpellingColor;
    public static Brush PitchAccentMarkerColor { get; set; } = Brushes.DeepSkyBlue;

    public static void ApplyDictOptions()
    {
        Dict jmdict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        string pOrthographyInfoColorString = jmdict.Options.POrthographyInfoColor!.Value;
        POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(pOrthographyInfoColorString);

        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchAccentDict))
        {
            string pitchAccentMarkerColorString = pitchAccentDict.Options.PitchAccentMarkerColor!.Value;
            PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentMarkerColorString);

            MainWindow.Instance.Dispatcher.Invoke(() => PopupWindowUtils.SetStrokeDashArray(pitchAccentDict.Options.ShowPitchAccentWithDottedLines!.Value));
        }
        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
        }
    }
}
