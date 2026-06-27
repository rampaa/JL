using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;
using JL.Core.Dicts;
using JL.Windows.GUI;
using JL.Windows.Utilities;

namespace JL.Windows.Config;

internal static class DictOptionManager
{
    public static Brush POrthographyInfoColor { get; set; } = ConfigManager.Instance.PrimarySpellingColor;
    public static Brush PitchAccentMarkerColor { get; set; } = Brushes.DeepSkyBlue;

    public static async Task ApplyDictOptions()
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
            await MainWindow.Instance.Dispatcher.BeginInvoke(() => PopupWindowUtils.SetPitchAccentMarkerPen(pitchAccentDict.Options.ShowPitchAccentWithDottedLines.Value, PitchAccentMarkerColor), DispatcherPriority.Send).Task.ConfigureAwait(false);
        }
        else
        {
            PitchAccentMarkerColor = Brushes.DeepSkyBlue;
        }
    }
}
