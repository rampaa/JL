using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Windows.Config;
using JL.Windows.GUI;
using JL.Windows.GUI.Popup;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    public const int MaxPopupWindowsIndex = 40;
    public static PopupWindow?[] PopupWindows { get; } = new PopupWindow?[MaxPopupWindowsIndex + 2];
    private static string? s_primarySpellingOfLastPlayedAudio;
    private static string? s_readingOfLastPlayedAudio;
    public static Pen PitchAccentMarkerPen { get; private set; } = new();
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

    public static TextBox CreateTextBox(string name, string text, Brush foregroundBrush, double fontSize, VerticalAlignment verticalAlignment, Thickness margin, ContextMenu contextMenu)
    {
        TextBox textBox = new()
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

            int index = hoveredPopup?.PopupIndex + 1 ?? 0;
            PopupWindow? popupWindow = PopupWindows[index];
            if (popupWindow?.IsVisible ?? false)
            {
                HidePopups(index);
            }
        });
    }

    public static void HidePopups(int rootPopupIndex)
    {
        if (rootPopupIndex is 0)
        {
            PopupWindow? popupWindow = PopupWindows[rootPopupIndex];
            Debug.Assert(popupWindow is not null);

            MainWindow mainWindow = MainWindow.Instance;
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                mainWindow.MouseEnterDueToFirstPopupHide = mainWindow.IsMouseWithinWindowBounds();
            }

            popupWindow.HidePopup();
            mainWindow.ChangeVisibility();
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

    public static void SetPitchAccentMarkerPen(bool showPitchAccentWithDottedLines, Brush pitchAccentMarkerColor)
    {
        PitchAccentMarkerPen = new Pen(pitchAccentMarkerColor, 1.5) { DashStyle = new DashStyle(showPitchAccentWithDottedLines ? [0.5, 1.5] : [1, 1], 0) };
        PitchAccentMarkerPen.Freeze();
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

        Dict dict = ((LookupDisplayResult)item).LookupResult.Dict;
        return !dict.Options.NoAll.Value;
    }

    public static string? GetSelectedDefinitions(TextBox? definitionsTextBox)
    {
        return definitionsTextBox?.SelectionLength > 0
            ? definitionsTextBox.SelectedText
            : null;
    }
}
