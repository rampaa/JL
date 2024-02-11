using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.GUI.UserControls;
using NAudio.Wave;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    private static string? s_primarySpellingOfLastPlayedAudio = null;

    private static string? s_readingOfLastPlayedAudio = null;
    public static DoubleCollection StrokeDashArray { get; set; } = new() { 1, 1 };

    public static TextBlock CreateTextBlock(string name, string text, Brush foregroundBrush, double fontSize, VerticalAlignment verticalAlignment, Thickness margin)
    {
        return new TextBlock
        {
            Name = name,
            Text = text,
            Foreground = foregroundBrush,
            FontSize = fontSize,
            VerticalAlignment = verticalAlignment,
            Margin = margin,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(0)
        };
    }

    public static TouchScreenTextBox CreateTextBox(string name, string text, Brush foregroundBrush, double fontSize, VerticalAlignment verticalAlignment, Thickness margin)
    {
        return new TouchScreenTextBox
        {
            Name = name,
            Text = text,
            Foreground = foregroundBrush,
            FontSize = fontSize,
            VerticalAlignment = verticalAlignment,
            Margin = margin,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow,
            SelectionBrush = ConfigManager.HighlightColor,
            IsInactiveSelectionHighlightEnabled = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            IsUndoEnabled = false,
            UndoLimit = 0,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };
    }

    public static Grid CreatePitchAccentGrid(string primarySpelling, string[]? alternativeSpellings,
        string[]? readings, string[] splitReadingsWithRInfo, double leftMargin, Dict dict, Dictionary<string, IList<IDictRecord>>? pitchRecordDict)
    {
        Grid pitchAccentGrid = new();

        bool hasReading = readings?.Length > 0;

        int fontSize = hasReading
            ? ConfigManager.ReadingsFontSize
            : ConfigManager.PrimarySpellingFontSize;

        string[] expressions = hasReading ? readings! : new[] { primarySpelling };

        double horizontalOffsetForReading = leftMargin;

        Dictionary<string, IList<IDictRecord>> lookupDict = pitchRecordDict ?? dict.Contents;

        for (int i = 0; i < expressions.Length; i++)
        {
            string normalizedExpression = JapaneseUtils.KatakanaToHiragana(expressions[i]);

            if (lookupDict.TryGetValue(normalizedExpression, out IList<IDictRecord>? pitchAccentDictResultList))
            {
                List<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expressions[i]);

                if (i > 0)
                {
                    horizontalOffsetForReading +=
                        WindowsUtils.MeasureTextSize(string.Create(CultureInfo.InvariantCulture, $"{splitReadingsWithRInfo[i - 1]}, "), fontSize).Width;
                }

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
                        StrokeDashArray = StrokeDashArray
                    };

                    bool lowPitch = false;
                    double horizontalOffsetForChar = horizontalOffsetForReading;
                    for (int j = 0; j < combinedFormList.Count; j++)
                    {
                        Size charSize = WindowsUtils.MeasureTextSize(combinedFormList[j], fontSize);

                        if ((chosenPitchAccentDictResult.Position - 1) == j)
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

        int popupNo = 1;
        while (currentPopupWindow is not null)
        {
            currentPopupWindow.HidePopup();

            PopupWindow? nextPopupWindow = currentPopupWindow.ChildPopupWindow;

            if (popupNo is 2)
            {
                currentPopupWindow.ChildPopupWindow = null;
            }

            if (popupNo > 2)
            {
                currentPopupWindow.Owner = null;
                currentPopupWindow.ChildPopupWindow = null;
                currentPopupWindow.Close();
            }

            currentPopupWindow = nextPopupWindow;

            ++popupNo;
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

    public static async Task PlayAudio(string primarySpelling, string? reading)
    {
        if (WindowsUtils.AudioPlayer?.PlaybackState is PlaybackState.Playing
            && s_primarySpellingOfLastPlayedAudio == primarySpelling
            && s_readingOfLastPlayedAudio == reading)
        {
            return;
        }

        s_primarySpellingOfLastPlayedAudio = primarySpelling;
        s_readingOfLastPlayedAudio = reading;

        await AudioUtils.GetAndPlayAudio(primarySpelling, reading).ConfigureAwait(false);
    }
}
