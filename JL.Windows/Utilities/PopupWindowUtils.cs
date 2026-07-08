using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

namespace JL.Windows.Utilities;

internal static class PopupWindowUtils
{
    public const int MaxPopupWindowsIndex = 40;

    public static readonly PopupWindow?[] PopupWindows = new PopupWindow?[MaxPopupWindowsIndex + 2];

    private static string? s_primarySpellingOfLastPlayedAudio;
    private static string? s_readingOfLastPlayedAudio;

    public static bool TransparentDueToAutoLookup { get; set; } // = false;

    public static Pen PitchAccentMarkerPen { get; private set; } = new();

    private static readonly object s_boxedTextWrappingWrap = TextWrapping.Wrap;
    public static readonly object BoxedHorizontalAlignmentLeft = HorizontalAlignment.Left;
    public static readonly object BoxedHorizontalAlignmentCenter = HorizontalAlignment.Center;
    private static readonly object s_boxedTrue = true;
    private static readonly object s_boxedIntZero = 0;
    private static readonly object s_boxedPanningModeNone = PanningMode.None;
    private static readonly object s_boxedScrollBarVisibilityDisabled = ScrollBarVisibility.Disabled;
    public static readonly object BoxedDefaultThickness = new Thickness();
    public static readonly object BoxedThickness0222 = new Thickness(0, 2, 2, 2);
    public static readonly object BoxedThickness2000 = new Thickness(2, 0, 0, 0);
    public static readonly object BoxedThickness2222 = new Thickness(2, 2, 2, 2);
    public static readonly object BoxedThickness3000 = new Thickness(3, 0, 0, 0);
    public static readonly object BoxedThickness5000 = new Thickness(5, 0, 0, 0);
    public static readonly object BoxedThickness5353 = new Thickness(5, 3, 5, 3);
    public static readonly object BoxedThickness7000 = new Thickness(7, 0, 0, 0);
    public static readonly object BoxedVerticalAlignmentCenter = VerticalAlignment.Center;
    public static readonly object BoxedVerticalAlignmentTop = VerticalAlignment.Top;
    public static readonly object BoxedDoubleNotANumber = double.NaN;
    public static readonly object BoxedFalse = false;

    public static TextBlock CreateTextBlock(string name, string text, Brush foregroundBrush, object fontSize, object verticalAlignment, object margin)
    {
        TextBlock textBlock = new()
        {
            Name = name,
            Text = text,
            Foreground = foregroundBrush,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow
        };

        textBlock.SetValue(FrameworkElement.MarginProperty, margin);
        textBlock.SetValue(FrameworkElement.HorizontalAlignmentProperty, BoxedHorizontalAlignmentLeft);
        textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, verticalAlignment);
        textBlock.SetValue(TextBlock.PaddingProperty, BoxedDefaultThickness);
        textBlock.SetValue(TextBlock.TextWrappingProperty, s_boxedTextWrappingWrap);
        textBlock.SetValue(TextBlock.FontSizeProperty, fontSize);

        return textBlock;
    }

    public static TextBox CreateTextBox(string name, string text, Brush foregroundBrush, object fontSize, object verticalAlignment, object margin, ContextMenu contextMenu)
    {
        TextBox textBox = new()
        {
            Name = name,
            Text = text,
            Foreground = foregroundBrush,
            CaretBrush = foregroundBrush,
            ContextMenu = contextMenu,
            Background = Brushes.Transparent,
            Cursor = Cursors.Arrow,
            SelectionBrush = ConfigManager.Instance.HighlightColor
        };

        textBox.SetValue(FrameworkElement.MarginProperty, margin);
        textBox.SetValue(FrameworkElement.HorizontalAlignmentProperty, BoxedHorizontalAlignmentLeft);
        textBox.SetValue(FrameworkElement.VerticalAlignmentProperty, verticalAlignment);
        textBox.SetValue(Control.PaddingProperty, BoxedDefaultThickness);
        textBox.SetValue(Control.BorderThicknessProperty, BoxedDefaultThickness);
        textBox.SetValue(TextBoxBase.IsReadOnlyProperty, s_boxedTrue);
        textBox.SetValue(TextBoxBase.IsInactiveSelectionHighlightEnabledProperty, s_boxedTrue);
        textBox.SetValue(TextBoxBase.UndoLimitProperty, s_boxedIntZero);
        textBox.SetValue(TextBoxBase.IsUndoEnabledProperty, BoxedFalse);
        textBox.SetValue(TextBoxBase.HorizontalScrollBarVisibilityProperty, s_boxedScrollBarVisibilityDisabled);
        textBox.SetValue(TextBoxBase.VerticalScrollBarVisibilityProperty, s_boxedScrollBarVisibilityDisabled);
        textBox.SetValue(TextBox.TextWrappingProperty, s_boxedTextWrappingWrap);
        textBox.SetValue(Control.FontSizeProperty, fontSize);

        // Scrolling doesn't work when touching a TextBox inside a ListView
        // unless the TextBox's PanningMode is set to None explicitly.
        textBox.SetValue(ScrollViewer.PanningModeProperty, s_boxedPanningModeNone);

        return textBox;
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

    public static void PopupDictTypeButtonsNeedUpdating()
    {
        foreach (PopupWindow? popupWindow in PopupWindows)
        {
            if (popupWindow is null)
            {
                break;
            }

            popupWindow.NeedToUpdateDictTypeButtons.SetTrue();
        }
    }
}
