using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.GUI.UserControls;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    public const int MaxPopupWindowsIndex = 40;
    public static PopupWindow?[] PopupWindows { get; } = new PopupWindow?[MaxPopupWindowsIndex + 2];
    private static string? s_primarySpellingOfLastPlayedAudio;
    private static string? s_readingOfLastPlayedAudio;
    private static DoubleCollection StrokeDashArray { get; set; } = [1, 1];
    public static readonly Timer PopupAutoHideTimer = new()
    {
        AutoReset = false
    };

    static PopupWindowUtils()
    {
        PopupAutoHideTimer.Elapsed += PopupAutoHideTimerEvent;
    }

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
            Padding = new Thickness()
        };
    }

    public static TouchScreenTextBox CreateTextBox(string name, string text, Brush foregroundBrush, double fontSize, VerticalAlignment verticalAlignment, Thickness margin, ContextMenu contextMenu)
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
            ContextMenu = contextMenu,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow,
            SelectionBrush = ConfigManager.Instance.HighlightColor,
            IsInactiveSelectionHighlightEnabled = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            IsUndoEnabled = false,
            UndoLimit = 0,
            BorderThickness = new Thickness(),
            Padding = new Thickness(),
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

        ReadOnlySpan<string> expressions = hasReading ? readings : [primarySpelling];

        double horizontalOffsetForReading = leftMargin;

        for (int i = 0; i < expressions.Length; i++)
        {
            if (i > 0)
            {
                Debug.Assert(splitReadingsWithRInfo is not null);
                horizontalOffsetForReading += WindowsUtils.MeasureTextSize($"{splitReadingsWithRInfo[i - 1]}、", fontSize).Width;
            }

            byte pitchPosition = pitchPositions[i];
            if (pitchPosition is byte.MaxValue)
            {
                continue;
            }

            Polyline polyline = new()
            {
                StrokeThickness = 2,
                Stroke = DictOptionManager.PitchAccentMarkerColor,
                StrokeDashArray = StrokeDashArray
            };

            bool lowPitch = false;
            double horizontalOffsetForChar = horizontalOffsetForReading;
            ReadOnlySpan<string> combinedFormList = JapaneseUtils.CreateCombinedForm(expressions[i]);
            for (int j = 0; j < combinedFormList.Length; j++)
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
        PopupAutoHideTimer.Enabled = true;
    }

    private static void PopupAutoHideTimerEvent(object? sender, ElapsedEventArgs e)
    {
        _ = MainWindow.Instance.FirstPopupWindow.Dispatcher.InvokeAsync(static () =>
        {
            PopupWindow? hoveredPopup = null;
            PopupWindow? currentPopupWindow = PopupWindows[0];
            while (currentPopupWindow is not null)
            {
                if (currentPopupWindow.IsMouseOver)
                {
                    hoveredPopup = currentPopupWindow;
                    break;
                }

                currentPopupWindow = PopupWindows[currentPopupWindow.PopupIndex + 1];
            }

            HidePopups(hoveredPopup?.PopupIndex + 1 ?? 0);
        });
    }

    public static void HidePopups(int rootPopupIndex)
    {
        if (rootPopupIndex is 0)
        {
            PopupWindow? popupWindow = PopupWindows[rootPopupIndex];
            Debug.Assert(popupWindow is not null);

            popupWindow.HidePopup();
            MainWindow.Instance.ChangeVisibility();
        }
        else
        {
            PopupWindow? currentPopupWindow = PopupWindows[rootPopupIndex];
            while (currentPopupWindow is not null)
            {
                currentPopupWindow.HidePopup();
                currentPopupWindow = PopupWindows[currentPopupWindow.PopupIndex + 1];
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
        // Ignore "NoAll" option if the lookup category is not "All"
        if (CoreConfigManager.Instance.LookupCategory is not LookupCategory.All)
        {
            return true;
        }

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
