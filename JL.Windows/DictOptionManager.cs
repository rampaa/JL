using System.Diagnostics;
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

        Debug.Assert(jmdict.Options.POrthographyInfoColor is not null);
        string pOrthographyInfoColorString = jmdict.Options.POrthographyInfoColor.Value;
        POrthographyInfoColor = WindowsUtils.FrozenBrushFromHex(pOrthographyInfoColorString);

        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchAccentDict))
        {
            Debug.Assert(pitchAccentDict.Options.PitchAccentMarkerColor is not null);
            string pitchAccentMarkerColorString = pitchAccentDict.Options.PitchAccentMarkerColor.Value;
            PitchAccentMarkerColor = WindowsUtils.FrozenBrushFromHex(pitchAccentMarkerColorString);

            Debug.Assert(pitchAccentDict.Options.ShowPitchAccentWithDottedLines is not null);
            MainWindow.Instance.Dispatcher.Invoke(() => PopupWindowUtils.SetPitchAccentMarkerPen(pitchAccentDict.Options.ShowPitchAccentWithDottedLines.Value, PitchAccentMarkerColor));
        }
        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
        }
    }
}
