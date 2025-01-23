using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.GUI.UserControls;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    private static string? s_primarySpellingOfLastPlayedAudio;
    private static string? s_readingOfLastPlayedAudio;
    private static DoubleCollection StrokeDashArray { get; set; } = [1, 1];
    public static readonly Timer PopupAutoHideTimer = new();

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

    public static TextBox CreateTextBox(string name, string text, Brush foregroundBrush, double fontSize, VerticalAlignment verticalAlignment, Thickness margin)
    {
        TouchScreenTextBox textBox = new()
        {
            Name = name,
            Text = text,
            Foreground = foregroundBrush,
            CaretBrush = foregroundBrush,
            FontSize = fontSize,
            VerticalAlignment = verticalAlignment,
            Margin = margin,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow,
            SelectionBrush = ConfigManager.Instance.HighlightColor,
            IsInactiveSelectionHighlightEnabled = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            IsUndoEnabled = false,
            UndoLimit = 0,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        // Scrolling doesn't work when touching a TextBox inside a ListView
        // unless the TextBox's PanningMode is set to None explicitly.
        textBox.SetValue(ScrollViewer.PanningModeProperty, PanningMode.None);

        return textBox;
    }

    public static Grid CreatePitchAccentGrid(string primarySpelling, string[]? readings, string[]? splitReadingsWithRInfo, double leftMargin, ReadOnlySpan<byte> pitchPositions)
    {
        Grid pitchAccentGrid = new();

        bool hasReading = readings is not null;

        ConfigManager configManager = ConfigManager.Instance;
        double fontSize = hasReading
            ? configManager.ReadingsFontSize
            : configManager.PrimarySpellingFontSize;

        string[] expressions = hasReading ? readings! : [primarySpelling];

        double horizontalOffsetForReading = leftMargin;

        for (int i = 0; i < expressions.Length; i++)
        {
            byte pitchPosition = pitchPositions[i];
            if (pitchPosition is byte.MaxValue)
            {
                continue;
            }

            List<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expressions[i]);

            if (i > 0)
            {
                horizontalOffsetForReading +=
                    WindowsUtils.MeasureTextSize($"{splitReadingsWithRInfo![i - 1]}、", fontSize).Width;
            }

            Polyline polyline = new()
            {
                StrokeThickness = 2,
                Stroke = DictOptionManager.PitchAccentMarkerColor,
                StrokeDashArray = StrokeDashArray
            };

            bool lowPitch = false;
            double horizontalOffsetForChar = horizontalOffsetForReading;
            int combinedFormListCount = combinedFormList.Count;
            for (int j = 0; j < combinedFormListCount; j++)
            {
                Size charSize = WindowsUtils.MeasureTextSize(combinedFormList[j], fontSize);

                if (pitchPosition - 1 == j)
                {
                    polyline.Points.Add(new Point(horizontalOffsetForChar, 0));
                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charSize.Height));

                    lowPitch = true;
                }

                else if (j is 0)
                {
                    polyline.Points.Add(new Point(horizontalOffsetForChar, charSize.Height));
                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charSize.Height));
                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, 0));
                }

                else
                {
                    double charHeight = lowPitch ? charSize.Height : 0;
                    polyline.Points.Add(new Point(horizontalOffsetForChar, charHeight));
                    polyline.Points.Add(new Point(horizontalOffsetForChar + charSize.Width, charHeight));
                }

                horizontalOffsetForChar += charSize.Width;
            }

            _ = pitchAccentGrid.Children.Add(polyline);
        }

        pitchAccentGrid.VerticalAlignment = VerticalAlignment.Center;
        pitchAccentGrid.HorizontalAlignment = HorizontalAlignment.Left;

        return pitchAccentGrid;
    }

    public static void SetPopupAutoHideTimer()
    {
        PopupAutoHideTimer.Interval = ConfigManager.Instance.AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;
        PopupAutoHideTimer.Elapsed += PopupAutoHideTimerEvent;
        PopupAutoHideTimer.AutoReset = false;
        PopupAutoHideTimer.Enabled = true;
    }

    private static void PopupAutoHideTimerEvent(object? sender, ElapsedEventArgs e)
    {
        _ = MainWindow.Instance.FirstPopupWindow.Dispatcher.InvokeAsync(static () =>
        {
            MainWindow mainWindow = MainWindow.Instance;
            PopupWindow? hoveredPopup = null;
            PopupWindow currentPopupWindow = mainWindow.FirstPopupWindow;
            while (currentPopupWindow.ChildPopupWindow is not null)
            {
                if (currentPopupWindow.IsMouseOver)
                {
                    hoveredPopup = currentPopupWindow;
                    break;
                }

                currentPopupWindow = currentPopupWindow.ChildPopupWindow;
            }

            HidePopups(hoveredPopup?.ChildPopupWindow ?? mainWindow.FirstPopupWindow);
        });
    }

    public static void HidePopups(PopupWindow? rootPopup)
    {
        if (rootPopup == MainWindow.Instance.FirstPopupWindow)
        {
            rootPopup.HidePopup();
        }
        else
        {
            PopupWindow? currentPopupWindow = rootPopup;
            while (currentPopupWindow is not null)
            {
                currentPopupWindow.HidePopup();
                currentPopupWindow = currentPopupWindow.ChildPopupWindow;
            }
        }
    }

    public static Task PlayAudio(string primarySpelling, string? reading)
    {
        if (WindowsUtils.AudioPlayer?.PlaybackState is PlaybackState.Playing
            && s_primarySpellingOfLastPlayedAudio == primarySpelling
            && s_readingOfLastPlayedAudio == reading)
        {
            return Task.CompletedTask;
        }

        s_primarySpellingOfLastPlayedAudio = primarySpelling;
        s_readingOfLastPlayedAudio = reading;

        return AudioUtils.GetAndPlayAudio(primarySpelling, reading);
    }

    public static void SetStrokeDashArray(bool showPitchAccentWithDottedLines)
    {
        StrokeDashArray = showPitchAccentWithDottedLines ? [1, 1] : [1, 0];
    }

    public static int GetIndexOfListViewItemFromStackPanel(StackPanel stackPanel)
    {
        return (int)((WrapPanel)stackPanel.Children[0]).Tag;
    }

    public static bool NoAllDictFilter(object item)
    {
        Dict dict = (Dict)((StackPanel)item).Tag;
        return !dict.Options.NoAll.Value;
    }

    public static string? GetSelectedDefinitions(TextBox? definitionsTextBox)
    {
        return definitionsTextBox?.SelectionLength > 0
            ? definitionsTextBox.SelectedText
            : null;
    }
}
