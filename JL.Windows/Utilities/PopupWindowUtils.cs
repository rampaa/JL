using System.Globalization;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core.Anki;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.PitchAccent;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    public static string GradeToText(int grade)
    {
        string gradeText = grade switch
        {
            >= 1 and <= 6 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Kyouiku)"),
            8 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jouyou)"),
            <= 10 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jinmeiyou)"),
            _ => "Hyougai"
        };

        return gradeText;
    }

    public static string? FrequenciesToText(List<LookupFrequencyResult> frequencies, bool forMining)
    {
        if (!forMining && frequencies.Count is 1 && frequencies[0].Freq is > 0 and < int.MaxValue)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        if (frequencies.Count > 0)
        {
            int freqResultCount = 0;
            StringBuilder sb = new();
            foreach (LookupFrequencyResult lookupFreqResult in frequencies)
            {
                if (lookupFreqResult.Freq is > 0 and < int.MaxValue)
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: {lookupFreqResult.Freq}, ");
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

    public static string ReadingsToText(string[] readings, List<string?> rOrthographyInfoList)
    {
        StringBuilder sb = new();

        for (int index = 0; index < readings.Length; index++)
        {
            _ = sb.Append(readings[index]);

            if (index < rOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(rOrthographyInfoList[index]))
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({rOrthographyInfoList[index]})");
                }
            }

            if (index != readings.Length - 1)
            {
                _ = sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public static string AlternativeSpellingsToText(string[] alternativeSpellings, List<string?> aOrthographyInfoList)
    {
        StringBuilder sb = new();

        _ = sb.Append('(');

        for (int index = 0; index < alternativeSpellings.Length; index++)
        {
            _ = sb.Append(alternativeSpellings[index]);

            if (index < aOrthographyInfoList?.Count)
            {
                if (!string.IsNullOrEmpty(aOrthographyInfoList[index]))
                {
                    _ = sb.Append(CultureInfo.InvariantCulture, $" ({aOrthographyInfoList[index]})");
                }
            }

            if (index != alternativeSpellings.Length - 1)
            {
                _ = sb.Append(", ");
            }
        }

        _ = sb.Append(')');

        return sb.ToString();
    }

    public static Grid CreatePitchAccentGrid(string primarySpelling, string[]? alternativeSpellings,
        string[]? readings, string[] splitReadingsWithRInfo, double leftMargin, Dict dict)
    {
        Grid pitchAccentGrid = new();

        bool hasReading = readings?.Length > 0;

        int fontSize = hasReading
            ? ConfigManager.ReadingsFontSize
            : ConfigManager.PrimarySpellingFontSize;

        string[] expressions = hasReading ? readings! : new[] { primarySpelling };

        double horizontalOffsetForReading = leftMargin;

        for (int i = 0; i < expressions.Length; i++)
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

    public static void HidePopups(PopupWindow? rootPopup)
    {
        PopupWindow? currentPopupWindow = rootPopup;

        while (currentPopupWindow?.IsVisible ?? false)
        {
            currentPopupWindow.HidePopup();
            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    public static void ShowMiningModeResults(PopupWindow popupWindow)
    {
        popupWindow.EnableMiningMode();
        WinApi.BringToFront(popupWindow.WindowHandle);
        popupWindow.DisplayResults(true);

        if (ConfigManager.Focusable)
        {
            _ = popupWindow.Activate();
        }

        _ = popupWindow.Focus();

        if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
        {
            SetPopupAutoHideTimer();
        }
    }

    public static async Task Mine(LookupResult lookupResult, string currentText, int currentCharPosition)
    {
        Dictionary<JLField, string> miningParams = new()
        {
            [JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            [JLField.SourceText] = currentText,
            [JLField.Sentence] = JapaneseUtils.FindSentence(currentText, currentCharPosition),
            [JLField.DictionaryName] = lookupResult.Dict.Name,
            [JLField.MatchedText] = lookupResult.MatchedText,
            [JLField.DeconjugatedMatchedText] = lookupResult.DeconjugatedMatchedText,
            [JLField.PrimarySpelling] = lookupResult.PrimarySpelling,
            [JLField.PrimarySpellingWithOrthographyInfo] = lookupResult.PrimarySpellingOrthographyInfoList is not null
                ? string.Create(CultureInfo.InvariantCulture, $"{lookupResult.PrimarySpelling} ({string.Join(", ", lookupResult.PrimarySpellingOrthographyInfoList)})")
                : lookupResult.PrimarySpelling
        };

        if (lookupResult.Readings is not null)
        {
            string readings = string.Join(", ", lookupResult.Readings);
            miningParams[JLField.Readings] = readings;

            miningParams[JLField.ReadingsWithOrthographyInfo] = lookupResult.ReadingsOrthographyInfoList is not null
                ? ReadingsToText(lookupResult.Readings, lookupResult.ReadingsOrthographyInfoList)
                : readings;
        }

        if (lookupResult.AlternativeSpellings is not null)
        {
            string alternativeSpellings = string.Join(", ", lookupResult.AlternativeSpellings);
            miningParams[JLField.AlternativeSpellings] = alternativeSpellings;

            miningParams[JLField.AlternativeSpellingsWithOrthographyInfo] = lookupResult.AlternativeSpellingsOrthographyInfoList is not null
                ? ReadingsToText(lookupResult.AlternativeSpellings, lookupResult.AlternativeSpellingsOrthographyInfoList)
                : alternativeSpellings;
        }

        if (lookupResult.Frequencies is not null)
        {
            string? formattedFreq = FrequenciesToText(lookupResult.Frequencies, true);
            if (formattedFreq is not null)
            {
                miningParams[JLField.Frequencies] = formattedFreq;
                miningParams[JLField.RawFrequencies] = string.Join(", ", lookupResult.Frequencies
                    .Where(static f => f.Freq is > 0 and < int.MaxValue)
                    .Select(static f => f.Freq));
            }
        }

        if (lookupResult.FormattedDefinitions is not null)
        {
            miningParams[JLField.Definitions] = lookupResult.FormattedDefinitions
                .Replace("\n", "<br/>", StringComparison.Ordinal);
        }

        if (lookupResult.EdictId > 0)
        {
            miningParams[JLField.EdictId] = lookupResult.EdictId.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.DeconjugationProcess is not null)
        {
            miningParams[JLField.DeconjugationProcess] = lookupResult.DeconjugationProcess;
        }

        if (lookupResult.KanjiComposition is not null)
        {
            miningParams[JLField.KanjiComposition] = lookupResult.KanjiComposition;
        }

        if (lookupResult.KanjiStats is not null)
        {
            miningParams[JLField.KanjiStats] = lookupResult.KanjiStats;
        }

        if (lookupResult.StrokeCount > 0)
        {
            miningParams[JLField.StrokeCount] = lookupResult.StrokeCount.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.KanjiGrade > -1)
        {
            miningParams[JLField.KanjiGrade] = lookupResult.KanjiGrade.ToString(CultureInfo.InvariantCulture);
        }

        if (lookupResult.OnReadings is not null)
        {
            miningParams[JLField.OnReadings] = string.Join(", ", lookupResult.OnReadings);
        }

        if (lookupResult.KunReadings is not null)
        {
            miningParams[JLField.KunReadings] = string.Join(", ", lookupResult.KunReadings);
        }

        if (lookupResult.NanoriReadings is not null)
        {
            miningParams[JLField.NanoriReadings] = string.Join(", ", lookupResult.NanoriReadings);
        }

        if (lookupResult.RadicalNames is not null)
        {
            miningParams[JLField.RadicalNames] = string.Join(", ", lookupResult.RadicalNames);
        }

        bool mined = await Mining.Mine(miningParams, lookupResult).ConfigureAwait(false);

        if (mined)
        {
            Stats.IncrementStat(StatType.CardsMined);
        }
    }
}
