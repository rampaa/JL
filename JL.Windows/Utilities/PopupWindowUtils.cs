using System.Globalization;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.PitchAccent;
using JL.Core.Utilities;
using JL.Windows.GUI;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    public static string GradeToText(int grade)
    {
        string gradeText = grade switch
        {
            0 => "Hyougai",
            <= 6 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Kyouiku)"),
            8 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jouyou)"),
            <= 10 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jinmeiyou)"),
            _ => ""
        };

        return gradeText;
    }

    public static string? FrequenciesToText(List<LookupFrequencyResult> frequencies)
    {
        if (frequencies.Count is 1 && frequencies[0].Freq is > 0 and < int.MaxValue)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        if (frequencies.Count > 1)
        {
            int freqResultCount = 0;
            StringBuilder sb = new();
            foreach (LookupFrequencyResult lookupFreqResult in frequencies)
            {
                if (lookupFreqResult.Freq is > 0 and < int.MaxValue)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: #{lookupFreqResult.Freq}, ");
                    ++freqResultCount;
                }
            }

            if (freqResultCount > 0)
            {
                return sb.Remove(sb.Length - 2, 2).ToString();
            }
        }

        return null;
    }

    public static string ReadingsToText(IList<string> readings, IReadOnlyList<string?> rOrthographyInfoList)
    {
        StringBuilder sb = new();

        for (int index = 0; index < readings.Count; index++)
        {
            _ = sb.Append(readings[index]);

            if (index < rOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(rOrthographyInfoList[index]))
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({rOrthographyInfoList[index]})");
                }
            }

            if (index != readings.Count - 1)
            {
                _ = sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public static string AlternativeSpellingsToText(IReadOnlyList<string> alternativeSpellings, IReadOnlyList<string?> aOrthographyInfoList)
    {
        StringBuilder sb = new();

        _ = sb.Append('(');

        for (int index = 0; index < alternativeSpellings.Count; index++)
        {
            _ = sb.Append(alternativeSpellings[index]);

            if (index < aOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(aOrthographyInfoList[index]))
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({aOrthographyInfoList[index]})");
                }
            }

            if (index != alternativeSpellings.Count - 1)
            {
                _ = sb.Append(", ");
            }
        }

        _ = sb.Append(')');

        return sb.ToString();
    }

    public static Grid CreatePitchAccentGrid(string primarySpelling, IReadOnlyList<string>? alternativeSpellings,
        IList<string>? readings, string[] splitReadingsWithRInfo, double leftMargin, Dict dict)
    {
        Grid pitchAccentGrid = new();

        bool hasReading = readings?.Count > 0;

        int fontSize = hasReading
            ? ConfigManager.ReadingsFontSize
            : ConfigManager.PrimarySpellingFontSize;

        IList<string> expressions = hasReading ? readings! : new List<string> { primarySpelling };

        double horizontalOffsetForReading = leftMargin;

        for (int i = 0; i < expressions.Count; i++)
        {
            string normalizedExpression = JapaneseUtils.KatakanaToHiragana(expressions[i]);
            List<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expressions[i]);

            if (i > 0)
            {
                horizontalOffsetForReading +=
                    WindowsUtils.MeasureTextSize(string.Create(CultureInfo.InvariantCulture, $"{splitReadingsWithRInfo[i - 1]}, "), fontSize).Width;
            }

            if (dict.Contents.TryGetValue(normalizedExpression, out IList<IDictRecord>? pitchAccentDictResultList))
            {
                PitchAccentRecord? chosenPitchAccentDictResult = null;

                for (int j = 0; j < pitchAccentDictResultList.Count; j++)
                {
                    PitchAccentRecord pitchAccentDictResult = (PitchAccentRecord)pitchAccentDictResultList[j];

                    if ((!hasReading && pitchAccentDictResult.Reading is null)
                        || (pitchAccentDictResult.Reading is not null
                            && normalizedExpression == JapaneseUtils.KatakanaToHiragana(pitchAccentDictResult.Reading)))
                    {
                        if (primarySpelling == pitchAccentDictResult.Spelling)
                        {
                            chosenPitchAccentDictResult = pitchAccentDictResult;
                            break;
                        }

                        if (alternativeSpellings?.Contains(pitchAccentDictResult.Spelling) ?? false)
                        {
                            chosenPitchAccentDictResult ??= pitchAccentDictResult;
                        }
                    }
                }

                if (chosenPitchAccentDictResult is not null)
                {
                    Polyline polyline = new()
                    {
                        StrokeThickness = 2,
                        Stroke = DictOptionManager.PitchAccentMarkerColor,
                        StrokeDashArray = new DoubleCollection { 1, 1 }
                    };

                    bool lowPitch = false;
                    double horizontalOffsetForChar = horizontalOffsetForReading;
                    for (int j = 0; j < combinedFormList.Count; j++)
                    {
                        Size charSize = WindowsUtils.MeasureTextSize(combinedFormList[j], fontSize);

                        if (chosenPitchAccentDictResult.Position - 1 == j)
                        {
                            polyline.Points.Add(new Point(horizontalOffsetForChar, 0));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charSize.Height));

                            lowPitch = true;
                        }

                        else if (j is 0)
                        {
                            polyline.Points.Add(new Point(horizontalOffsetForChar, charSize.Height));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charSize.Height));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                        }

                        else
                        {
                            double charHeight = lowPitch ? charSize.Height : 0;
                            polyline.Points.Add(new Point(horizontalOffsetForChar, charHeight));
                            polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width,
                                charHeight));
                        }

                        horizontalOffsetForChar += charSize.Width;
                    }

                    _ = pitchAccentGrid.Children.Add(polyline);
                }
            }
        }

        pitchAccentGrid.VerticalAlignment = VerticalAlignment.Center;
        pitchAccentGrid.HorizontalAlignment = HorizontalAlignment.Left;

        return pitchAccentGrid;
    }

    public static void SetPopupAutoHideTimer()
    {
        PopupWindow.PopupAutoHideTimer.Interval = ConfigManager.AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;
        PopupWindow.PopupAutoHideTimer.Elapsed += PopupAutoHideTimerEvent;
        PopupWindow.PopupAutoHideTimer.AutoReset = false;
        PopupWindow.PopupAutoHideTimer.Enabled = true;
    }

    private static void PopupAutoHideTimerEvent(object? sender, ElapsedEventArgs e)
    {
        _ = MainWindow.Instance.FirstPopupWindow.Dispatcher.InvokeAsync(static () =>
        {
            PopupWindow lastPopupWindow = MainWindow.Instance.FirstPopupWindow;
            while (lastPopupWindow.ChildPopupWindow?.IsVisible ?? false)
            {
                lastPopupWindow = lastPopupWindow.ChildPopupWindow;
            }

            while (!lastPopupWindow.IsMouseOver)
            {
                lastPopupWindow.HidePopup();

                if (lastPopupWindow.Owner is not PopupWindow parentPopupWindow)
                {
                    break;
                }

                lastPopupWindow = parentPopupWindow;
            }
        });
    }
}
