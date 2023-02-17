using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Caching;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal sealed partial class PopupWindow : Window
{
    public PopupWindow? ChildPopupWindow { get; private set; }

    private TextBox? _lastTextBox;

    private int _playAudioIndex;

    private int _currentCharPosition;

    private string? _currentText;

    private string? _lastSelectedText;

    private IntPtr _windowHandle;

    private List<LookupResult> _lastLookupResults = new();

    private Dict? _filteredDict = null;

    public bool UnavoidableMouseEnter { get; private set; } = false;

    public string? LastText { get; set; }

    public bool MiningMode { get; set; }

    public ObservableCollection<StackPanel> ResultStackPanels { get; } = new();

    public ObservableCollection<Button> DictTypeButtons { get; } = new();

    public static Timer PopupAutoHideTimer { get; } = new();

    public static LRUCache<string, StackPanel[]> StackPanelCache { get; } = new(
        Storage.CacheSize, Storage.CacheSize / 8);

    public PopupWindow()
    {
        InitializeComponent();
        Init();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
    }
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    private void Init()
    {
        Background = ConfigManager.PopupBackgroundColor;
        Foreground = ConfigManager.DefinitionsColor;
        FontFamily = ConfigManager.PopupFont;

        WindowsUtils.SetSizeToContentForPopup(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, this);

        WindowsUtils.SetInputGestureText(AddNameButton, ConfigManager.ShowAddNameWindowKeyGesture);
        WindowsUtils.SetInputGestureText(AddWordButton, ConfigManager.ShowAddWordWindowKeyGesture);
        WindowsUtils.SetInputGestureText(SearchButton, ConfigManager.SearchWithBrowserKeyGesture);
        WindowsUtils.SetInputGestureText(StatsButton, ConfigManager.ShowStatsKeyGesture);

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder.Text =
                "Click on an entry's main spelling to mine it," + Environment.NewLine +
                $"or press {ConfigManager.ClosePopupKeyGesture.Key} or click on the main window to exit.";
        }
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddNameWindow(_lastSelectedText);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddWordWindow(_lastSelectedText);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        WindowsUtils.SearchWithBrowser(_lastSelectedText);
    }

    private async void ShowStats(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowStatsWindow().ConfigureAwait(false);
    }

    public async void TextBox_MouseMove(TextBox tb)
    {
        if (MiningMode || ConfigManager.InactiveLookupMode
                       || (ConfigManager.RequireLookupKeyPress && !WindowsUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture))
                       || (ConfigManager.FixedPopupPositioning && Owner != MainWindow.Instance)
           )
        {
            return;
        }

        int charPosition = tb.GetCharacterIndexFromPoint(Mouse.GetPosition(tb), false);
        if (charPosition is not -1)
        {
            if (charPosition > 0 && char.IsHighSurrogate(tb.Text[charPosition - 1]))
            {
                --charPosition;
            }

            _currentText = tb.Text;
            _currentCharPosition = charPosition;

            if (Owner != MainWindow.Instance
                ? ConfigManager.DisableLookupsForNonJapaneseCharsInPopups
                    && !Storage.JapaneseRegex.IsMatch(tb.Text[charPosition].ToString())
                : ConfigManager.DisableLookupsForNonJapaneseCharsInMainWindow
                    && !Storage.JapaneseRegex.IsMatch(tb.Text[charPosition].ToString()))
            {
                if (ConfigManager.HighlightLongestMatch)
                {
                    WindowsUtils.Unselect(_lastTextBox);
                }

                Visibility = Visibility.Hidden;
                return;
            }

            int endPosition = tb.Text.Length - charPosition > ConfigManager.MaxSearchLength
                ? Utils.FindWordBoundary(tb.Text[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
                : Utils.FindWordBoundary(tb.Text, charPosition);

            string text = tb.Text[charPosition..endPosition];

            if (text == LastText && IsVisible)
            {
                return;
            }

            LastText = text;

            ResultStackPanels.Clear();
            List<LookupResult>? lookupResults = Lookup.LookupText(text);

            if (lookupResults is { Count: > 0 })
            {
                _lastTextBox = tb;
                _lastSelectedText = lookupResults[0].MatchedText;
                if (ConfigManager.HighlightLongestMatch)
                {
                    _ = tb.Focus();
                    tb.Select(charPosition, lookupResults[0].MatchedText.Length);
                }

                Init();

                _lastLookupResults = lookupResults;

                if (ConfigManager.LookupOnLeftClickOnly)
                {
                    EnableMiningMode();
                    DisplayResults(true, text);
                }

                else
                {
                    DisplayResults(false, text);
                }

                Show();

                if (ConfigManager.Focusable && ConfigManager.PopupFocusOnLookup)
                {
                    _ = Activate();
                }

                _ = Focus();

                WinApi.BringToFront(_windowHandle);

                if (ConfigManager.AutoPlayAudio)
                {
                    await PlayAudio().ConfigureAwait(false);
                }
            }
            else
            {
                LastText = "";
                Visibility = Visibility.Hidden;

                //if (ConfigManager.HighlightLongestMatch)
                //{
                //    //Unselect(tb);
                //}
            }
        }
        else
        {
            LastText = "";
            Visibility = Visibility.Hidden;

            if (ConfigManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(tb);
            }
        }
    }

    public async void LookupOnSelect(TextBox tb)
    {
        if (string.IsNullOrWhiteSpace(tb.SelectedText))
        {
            return;
        }

        _lastTextBox = tb;
        _lastSelectedText = tb.SelectedText;

        PopUpScrollViewer.ScrollToTop();

        List<LookupResult>? lookupResults = Lookup.LookupText(tb.SelectedText);

        if (lookupResults?.Count > 0)
        {
            ResultStackPanels.Clear();

            Init();

            _lastLookupResults = lookupResults;
            EnableMiningMode();
            DisplayResults(true, tb.SelectedText);

            Show();
            _ = tb.Focus();

            if (ConfigManager.Focusable)
            {
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(_windowHandle);

            if (ConfigManager.AutoPlayAudio)
            {
                await PlayAudio().ConfigureAwait(false);
            }
        }
        else
        {
            Visibility = Visibility.Hidden;
        }
    }

    public void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X / WindowsUtils.Dpi.DpiScaleX;
        double mouseY = cursorPosition.Y / WindowsUtils.Dpi.DpiScaleY;

        bool needsFlipX = ConfigManager.PopupFlipX && mouseX + Width > WindowsUtils.DpiAwareWorkAreaWidth;
        bool needsFlipY = ConfigManager.PopupFlipY && mouseY + Height > WindowsUtils.DpiAwareWorkAreaHeight;

        double newLeft;
        double newTop;

        UnavoidableMouseEnter = false;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - (Width + (WindowsUtils.DpiAwareXOffset / 2));
            if (newLeft < 0)
            {
                newLeft = 0;
            }
        }
        else
        {
            // no flip
            newLeft = mouseX + WindowsUtils.DpiAwareXOffset;
        }

        if (needsFlipY)
        {
            // flip Upwards while preventing -OOB
            newTop = mouseY - (Height + (WindowsUtils.DpiAwareYOffset / 2));
            if (newTop < 0)
            {
                newTop = 0;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + WindowsUtils.DpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + Width > WindowsUtils.DpiAwareWorkAreaWidth)
        {
            newLeft = WindowsUtils.DpiAwareWorkAreaWidth - Width;
        }

        if (newTop + Height > WindowsUtils.DpiAwareWorkAreaHeight)
        {
            newTop = WindowsUtils.DpiAwareWorkAreaHeight - Height;
        }

        if (mouseX >= newLeft && mouseX <= newLeft + Width && mouseY >= newTop && mouseY <= newTop + Height)
        {
            UnavoidableMouseEnter = true;
        }

        Left = newLeft;
        Top = newTop;
    }

    public void UpdatePosition(double x, double y)
    {
        Left = x;
        Top = y;
    }

    private void DisplayResults(bool generateAllResults, string? text = null)
    {
        // TODO: Should be configurable
        PopupListBox.Items.Filter = NoAllDictFilter;

        if (text is not null && !generateAllResults && StackPanelCache.TryGet(text, out StackPanel[] data))
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (i > ConfigManager.MaxNumResultsNotInMiningMode)
                {
                    break;
                }

                ResultStackPanels.Add(data[i]);
            }

            GenerateDictTypeButtons();
            UpdateLayout();
        }

        else
        {
            int resultCount = _lastLookupResults.Count;
            for (int index = 0; index < resultCount; index++)
            {
                if (!generateAllResults && index > ConfigManager.MaxNumResultsNotInMiningMode)
                {
                    break;
                }

                ResultStackPanels.Add(MakeResultStackPanel(_lastLookupResults[index], index, resultCount));
            }

            GenerateDictTypeButtons();
            UpdateLayout();

            // we might cache incomplete results if we don't wait until all dicts are loaded
            if (text is not null && Storage.DictsReady && !Storage.UpdatingJMdict && !Storage.UpdatingJMnedict && !Storage.UpdatingKanjidic)
            {
                StackPanelCache.AddReplace(text, ResultStackPanels.ToArray());
            }
        }
    }

    public StackPanel MakeResultStackPanel(LookupResult result,
        int index, int resultsCount)
    {
        var innerStackPanel = new StackPanel { Margin = new Thickness(4, 2, 4, 2), Tag = result.Dict };
        WrapPanel top = new();
        StackPanel bottom = new();

        _ = innerStackPanel.Children.Add(top);
        _ = innerStackPanel.Children.Add(bottom);

        // top
        TextBlock? textBlockPOrthographyInfo = null;
        UIElement? uiElementReadings = null;
        UIElement? uiElementAlternativeSpellings = null;
        TextBlock? textBlockProcess = null;
        TextBlock? textBlockFrequency = null;
        TextBlock? textBlockEdictId = null;

        var textBlockMatchedText = new TextBlock
        {
            Name = nameof(result.MatchedText),
            Text = result.MatchedText,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBlockDeconjugatedMatchedText = new TextBlock
        {
            Name = nameof(result.DeconjugatedMatchedText),
            Text = result.DeconjugatedMatchedText,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBlockPrimarySpelling = new TextBlock
        {
            Name = nameof(result.PrimarySpelling),
            Text = result.PrimarySpelling,
            Tag = index, // for audio
            Foreground = ConfigManager.PrimarySpellingColor,
            Background = Brushes.Transparent,
            //SelectionTextBrush = ConfigManager.HighlightColor,
            FontSize = ConfigManager.PrimarySpellingFontSize,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            //BorderThickness = new Thickness(0, 0, 0, 0),
            Margin = new Thickness(5, 0, 0, 0),
            Padding = new Thickness(0),
            //IsReadOnly = true,
            //IsUndoEnabled = false,
            Cursor = Cursors.Arrow,
            //IsInactiveSelectionHighlightEnabled = true,
            ContextMenu = PopupContextMenu
        };

        // bottom
        UIElement? uiElementDefinitions = null;
        TextBlock? textBlockNanoriReadings = null;
        TextBlock? textBlockOnReadings = null;
        TextBlock? textBlockKunReadings = null;
        TextBlock? textBlockStrokeCount = null;
        TextBlock? textBlockGrade = null;
        TextBlock? textBlockComposition = null;
        TextBlock? textBlockKanjiStats = null;

        if (result.Frequencies?.Count > 0)
        {
            string freqText = PopupWindowUtils.FrequenciesToText(result.Frequencies);

            if (freqText is not "")
            {
                textBlockFrequency = new TextBlock
                {
                    Name = nameof(result.Frequencies),
                    Text = freqText,
                    Foreground = ConfigManager.FrequencyColor,
                    FontSize = ConfigManager.FrequencyFontSize,
                    Margin = new Thickness(5, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
            }
        }

        TextBlock textBlockDictType = new()
        {
            Name = nameof(result.Dict.Name),
            Text = result.Dict.Name,
            Foreground = ConfigManager.DictTypeColor,
            FontSize = ConfigManager.DictTypeFontSize,
            Margin = new Thickness(5, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        textBlockPrimarySpelling.MouseEnter += PrimarySpelling_MouseEnter; // for audio
        textBlockPrimarySpelling.MouseLeave += PrimarySpelling_MouseLeave; // for audio
        textBlockPrimarySpelling.PreviewMouseUp += PrimarySpelling_PreviewMouseUp; // for mining

        if (result.Readings?.Count > 0)
        {
            List<string> rOrthographyInfoList = result.ReadingsOrthographyInfoList ?? new List<string>();
            List<string> readings = result.Readings;
            string readingsText = rOrthographyInfoList.Count > 0 && (result.Dict.Options?.ROrthographyInfo?.Value ?? true)
                ? PopupWindowUtils.ReadingsToText(readings, rOrthographyInfoList)
                : string.Join(", ", result.Readings);

            if (readingsText is not "")
            {
                if (MiningMode)
                {
                    uiElementReadings = new TextBox
                    {
                        Name = nameof(result.Readings),
                        Text = readingsText,
                        TextWrapping = TextWrapping.Wrap,
                        Background = Brushes.Transparent,
                        Foreground = ConfigManager.ReadingsColor,
                        FontSize = ConfigManager.ReadingsFontSize,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        Margin = new Thickness(5, 0, 0, 0),
                        Padding = new Thickness(0),
                        IsReadOnly = true,
                        IsUndoEnabled = false,
                        Cursor = Cursors.Arrow,
                        SelectionBrush = ConfigManager.HighlightColor,
                        IsInactiveSelectionHighlightEnabled = true,
                        ContextMenu = PopupContextMenu,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    uiElementReadings.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;
                    uiElementReadings.PreviewMouseDown += UiElement_PreviewMouseButtonDown;
                    uiElementReadings.MouseMove += PopupMouseMove;
                    uiElementReadings.LostFocus += Unselect;
                    uiElementReadings.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                }
                else
                {
                    uiElementReadings = new TextBlock
                    {
                        Name = nameof(result.Readings),
                        Text = readingsText,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = ConfigManager.ReadingsColor,
                        FontSize = ConfigManager.ReadingsFontSize,
                        Margin = new Thickness(5, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
        }

        if (result.FormattedDefinitions?.Length > 0)
        {
            if (MiningMode)
            {
                uiElementDefinitions = new TextBox
                {
                    Name = nameof(result.FormattedDefinitions),
                    Text = result.FormattedDefinitions,
                    TextWrapping = TextWrapping.Wrap,
                    Background = Brushes.Transparent,
                    Foreground = ConfigManager.DefinitionsColor,
                    FontSize = ConfigManager.DefinitionsFontSize,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Margin = new Thickness(2, 2, 2, 2),
                    Padding = new Thickness(0),
                    IsReadOnly = true,
                    IsUndoEnabled = false,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                uiElementDefinitions.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;
                uiElementDefinitions.PreviewMouseDown += UiElement_PreviewMouseButtonDown;
                uiElementDefinitions.MouseMove += PopupMouseMove;
                uiElementDefinitions.LostFocus += Unselect;
                uiElementDefinitions.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
            }
            else
            {
                uiElementDefinitions = new TextBlock
                {
                    Name = nameof(result.FormattedDefinitions),
                    Text = result.FormattedDefinitions,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = ConfigManager.DefinitionsColor,
                    FontSize = ConfigManager.DefinitionsFontSize,
                    Margin = new Thickness(2, 2, 2, 2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }

        if (result.EdictId is not 0)
        {
            textBlockEdictId = new TextBlock
            {
                Name = nameof(result.EdictId),
                Text = result.EdictId.ToString(CultureInfo.InvariantCulture),
                Visibility = Visibility.Collapsed
            };
        }

        if (result.AlternativeSpellings?.Count > 0)
        {
            List<string> aOrthographyInfoList = result.AlternativeSpellingsOrthographyInfoList ?? new List<string>();
            List<string> alternativeSpellings = result.AlternativeSpellings;
            string alternativeSpellingsText = aOrthographyInfoList.Count > 0 && (result.Dict.Options?.AOrthographyInfo?.Value ?? true)
                ? PopupWindowUtils.AlternativeSpellingsToText(alternativeSpellings, aOrthographyInfoList)
                : "(" + string.Join(", ", alternativeSpellings) + ")";

            if (alternativeSpellingsText is not "")
            {
                if (MiningMode)
                {
                    uiElementAlternativeSpellings = new TextBox
                    {
                        Name = nameof(result.AlternativeSpellings),
                        Text = alternativeSpellingsText,
                        TextWrapping = TextWrapping.Wrap,
                        Background = Brushes.Transparent,
                        Foreground = ConfigManager.AlternativeSpellingsColor,
                        FontSize = ConfigManager.AlternativeSpellingsFontSize,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        Margin = new Thickness(5, 0, 0, 0),
                        Padding = new Thickness(0),
                        IsReadOnly = true,
                        IsUndoEnabled = false,
                        Cursor = Cursors.Arrow,
                        SelectionBrush = ConfigManager.HighlightColor,
                        IsInactiveSelectionHighlightEnabled = true,
                        ContextMenu = PopupContextMenu,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    uiElementAlternativeSpellings.PreviewMouseLeftButtonUp += UiElement_PreviewMouseLeftButtonUp;
                    uiElementAlternativeSpellings.PreviewMouseDown += UiElement_PreviewMouseButtonDown;
                    uiElementAlternativeSpellings.MouseMove += PopupMouseMove;
                    uiElementAlternativeSpellings.LostFocus += Unselect;
                    uiElementAlternativeSpellings.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                }
                else
                {
                    uiElementAlternativeSpellings = new TextBlock
                    {
                        Name = nameof(result.AlternativeSpellings),
                        Text = alternativeSpellingsText,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = ConfigManager.AlternativeSpellingsColor,
                        FontSize = ConfigManager.AlternativeSpellingsFontSize,
                        Margin = new Thickness(5, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                }
            }
        }

        if (result.Process is not null)
        {
            textBlockProcess = new TextBlock
            {
                Name = nameof(result.Process),
                Text = result.Process,
                Foreground = ConfigManager.DeconjugationInfoColor,
                FontSize = ConfigManager.DeconjugationInfoFontSize,
                Margin = new Thickness(5, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
        }

        if (result.PrimarySpellingOrthographyInfoList?.Count > 0
            && (result.Dict.Options?.POrthographyInfo?.Value ?? true))
        {
            textBlockPOrthographyInfo = new TextBlock
            {
                Name = nameof(result.PrimarySpellingOrthographyInfoList),
                Text = $"({string.Join(", ", result.PrimarySpellingOrthographyInfoList)})",
                Foreground = DictOptionManager.POrthographyInfoColor,
                FontSize = result.Dict.Options?.POrthographyInfoFontSize?.Value ?? 15,
                Margin = new Thickness(5, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        // KANJIDIC
        if (result.OnReadings?.Count > 0)
        {
            textBlockOnReadings = new TextBlock
            {
                Name = nameof(result.OnReadings),
                Text = "On" + ": " + string.Join(", ", result.OnReadings),
                Foreground = ConfigManager.ReadingsColor,
                FontSize = ConfigManager.ReadingsFontSize,
                Margin = new Thickness(2, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.KunReadings?.Count > 0)
        {
            textBlockKunReadings = new TextBlock
            {
                Name = nameof(result.KunReadings),
                Text = "Kun" + ": " + string.Join(", ", result.KunReadings),
                Foreground = ConfigManager.ReadingsColor,
                FontSize = ConfigManager.ReadingsFontSize,
                Margin = new Thickness(2, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.NanoriReadings?.Count > 0)
        {
            textBlockNanoriReadings = new TextBlock
            {
                Name = nameof(result.NanoriReadings),
                Text = "Nanori Readings: " + string.Join(", ", result.NanoriReadings),
                Foreground = ConfigManager.ReadingsColor,
                FontSize = ConfigManager.ReadingsFontSize,
                Margin = new Thickness(2, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.StrokeCount > 0)
        {
            textBlockStrokeCount = new TextBlock
            {
                Name = nameof(result.StrokeCount),
                Text = "Strokes" + ": " + result.StrokeCount,
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.KanjiGrade > -1)
        {
            string gradeText = PopupWindowUtils.GradeToText(result.KanjiGrade);
            textBlockGrade = new TextBlock
            {
                Name = nameof(result.KanjiGrade),
                Text = nameof(result.KanjiGrade) + ": " + gradeText,
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.KanjiComposition?.Length > 0)
        {
            textBlockComposition = new TextBlock
            {
                Name = nameof(result.KanjiComposition),
                Text = "Composition: " + result.KanjiComposition,
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        if (result.KanjiStats?.Length > 0)
        {
            textBlockKanjiStats = new TextBlock
            {
                Name = nameof(result.KanjiStats),
                Text = "Statistics:\n" + result.KanjiStats,
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        UIElement?[] babies =
        {
            textBlockPrimarySpelling, textBlockPOrthographyInfo, uiElementReadings, uiElementAlternativeSpellings,
            textBlockProcess, textBlockMatchedText, textBlockDeconjugatedMatchedText, textBlockEdictId, textBlockFrequency, textBlockDictType
        };

        for (int i = 0; i < babies.Length; i++)
        {
            UIElement? baby = babies[i];

            if (baby is null)
            {
                continue;
            }

            if (baby is TextBlock textBlock)
            {
                // common emptiness check
                if (textBlock.Text is "")
                {
                    continue;
                }

                textBlock.MouseLeave += OnMouseLeave;

                if (textBlock.Name is "PrimarySpelling" or "Readings")
                {
                    Dict? pitchDict = Storage.Dicts.Values.FirstOrDefault(static dict => dict.Type is DictType.PitchAccentYomichan);
                    if (pitchDict?.Active ?? false)
                    {
                        List<string>? readings = result.Readings;

                        if (textBlock.Name is "PrimarySpelling" && readings?.Count > 0)
                        {
                            _ = top.Children.Add(textBlock);
                        }

                        else
                        {
                            Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                                result.AlternativeSpellings ?? new List<string>(),
                                readings ?? new List<string>(),
                                textBlock.Text.Split(", ").ToList(),
                                textBlock.Margin.Left,
                                pitchDict);

                            if (pitchAccentGrid.Children.Count is 0)
                            {
                                _ = top.Children.Add(textBlock);
                            }

                            else
                            {
                                _ = pitchAccentGrid.Children.Add(textBlock);
                                _ = top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }

                    else
                    {
                        _ = top.Children.Add(textBlock);
                    }
                }
                else
                {
                    _ = top.Children.Add(textBlock);
                }
            }
            else if (baby is TextBox textBox)
            {
                // common emptiness check
                if (textBox.Text is "")
                {
                    continue;
                }

                textBox.MouseLeave += OnMouseLeave;

                if (textBox.Name is "PrimarySpelling" or "Readings")
                {
                    Dict? pitchDict = Storage.Dicts.Values.FirstOrDefault(static dict => dict.Type is DictType.PitchAccentYomichan);
                    if (pitchDict?.Active ?? false)
                    {
                        List<string>? readings = result.Readings;

                        if (textBox.Name is "PrimarySpelling" && readings?.Count > 0)
                        {
                            _ = top.Children.Add(textBox);
                        }

                        else
                        {
                            Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                                result.AlternativeSpellings ?? new List<string>(),
                                readings ?? new List<string>(),
                                textBox.Text.Split(", ").ToList(),
                                textBox.Margin.Left,
                                pitchDict);

                            if (pitchAccentGrid.Children.Count is 0)
                            {
                                _ = top.Children.Add(textBox);
                            }
                            else
                            {
                                _ = pitchAccentGrid.Children.Add(textBox);
                                _ = top.Children.Add(pitchAccentGrid);
                            }
                        }
                    }
                    else
                    {
                        _ = top.Children.Add(textBox);
                    }
                }
                else
                {
                    _ = top.Children.Add(textBox);
                }
            }
        }

        if (uiElementDefinitions is not null)
        {
            _ = bottom.Children.Add(uiElementDefinitions);
        }

        TextBlock?[] babiesKanji =
        {
            textBlockOnReadings, textBlockKunReadings, textBlockNanoriReadings, textBlockGrade, textBlockStrokeCount,
            textBlockComposition, textBlockKanjiStats
        };

        foreach (TextBlock? baby in babiesKanji)
        {
            if (baby is null)
            {
                continue;
            }

            // common emptiness check
            if (baby.Text is "")
            {
                continue;
            }

            baby.MouseLeave += OnMouseLeave;

            _ = bottom.Children.Add(baby);
        }

        if (index != resultsCount - 1)
        {
            _ = bottom.Children.Add(new Separator
            {
                // TODO: Fix width differing from one separator to another
                Height = 2,
                Background = ConfigManager.SeparatorColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        innerStackPanel.MouseLeave += OnMouseLeave;
        top.MouseLeave += OnMouseLeave;
        bottom.MouseLeave += OnMouseLeave;

        PopupListBox.Items.Filter = NoAllDictFilter;

        return innerStackPanel;
    }

    private static void Unselect(object sender, RoutedEventArgs e)
    {
        WindowsUtils.Unselect((TextBox)sender);
    }

    private void TextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        AddNameButton.IsEnabled = Storage.DictsReady;
        AddWordButton.IsEnabled = Storage.DictsReady;

        _lastSelectedText = ((TextBox)sender).SelectedText;
    }

    private void PopupMouseMove(object sender, MouseEventArgs e)
    {
        if (ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnLeftClickOnly
            || (ConfigManager.RequireLookupKeyPress
                && !WindowsUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        ChildPopupWindow ??= new PopupWindow { Owner = this };

        if (ChildPopupWindow.MiningMode)
        {
            return;
        }

        // prevents stray PopupWindows being created when you move your mouse too fast
        if (MiningMode)
        {
            ChildPopupWindow.Definitions_MouseMove((TextBox)sender);

            if (!ChildPopupWindow.MiningMode)
            {
                if (ConfigManager.FixedPopupPositioning)
                {
                    ChildPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition,
                        WindowsUtils.DpiAwareFixedPopupYPosition);
                }

                else
                {
                    ChildPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
                }
            }
        }
    }

    private void PrimarySpelling_MouseEnter(object sender, MouseEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        _playAudioIndex = (int)textBlock.Tag;
    }

    private void PrimarySpelling_MouseLeave(object sender, MouseEventArgs e)
    {
        _playAudioIndex = 0;
    }

    private async void PrimarySpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.Middle)
        {
            WindowsUtils.CopyTextToClipboard(((TextBlock)sender).Text);
            return;
        }

        if (!MiningMode || e.ChangedButton is MouseButton.Right)
        {
            return;
        }

        MiningMode = false;
        TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;
        ItemsControlButtons.Visibility = Visibility.Collapsed;
        Hide();

        var miningParams = new Dictionary<JLField, string>();

        if (_currentText is not null)
        {
            miningParams[JLField.SourceText] = _currentText;
            miningParams[JLField.Sentence] = Utils.FindSentence(_currentText, _currentCharPosition);
        }

        var textBlock = (TextBlock)sender;

        WrapPanel top = textBlock.Parent is Grid primarySpellingGrid
            ? (WrapPanel)primarySpellingGrid.Parent
            : (WrapPanel)textBlock.Parent;

        foreach (UIElement child in top.Children)
        {
            switch (child)
            {
                case TextBox chi:
                    switch (chi.Name)
                    {
                        case nameof(LookupResult.Readings):
                            miningParams[JLField.Readings] = chi.Text;
                            break;
                        case nameof(LookupResult.AlternativeSpellings):
                            miningParams[JLField.AlternativeSpellings] = chi.Text;
                            break;
                    }

                    break;
                case TextBlock ch:
                    switch (ch.Name)
                    {
                        case nameof(LookupResult.PrimarySpelling):
                            miningParams[JLField.PrimarySpelling] = ch.Text;
                            break;
                        case nameof(LookupResult.MatchedText):
                            miningParams[JLField.MatchedText] = ch.Text;
                            break;
                        case nameof(LookupResult.DeconjugatedMatchedText):
                            miningParams[JLField.DeconjugatedMatchedText] = ch.Text;
                            break;
                        case nameof(LookupResult.EdictId):
                            miningParams[JLField.EdictId] = ch.Text;
                            break;
                        case nameof(LookupResult.Frequencies):
                            miningParams[JLField.Frequencies] = ch.Text;
                            break;
                        case nameof(LookupResult.Dict.Name):
                            miningParams[JLField.DictionaryName] = ch.Text;
                            break;
                        case nameof(LookupResult.Process):
                            miningParams[JLField.DeconjugationProcess] = ch.Text;
                            break;
                    }

                    break;
                case Grid grid:
                    {
                        foreach (UIElement uiElement in grid.Children)
                        {
                            if (uiElement is TextBlock textBlockCg)
                            {
                                switch (textBlockCg.Name)
                                {
                                    case nameof(LookupResult.PrimarySpelling):
                                        miningParams[JLField.PrimarySpelling] = textBlockCg.Text;
                                        break;
                                }
                            }
                            else if (uiElement is TextBox textBoxCg)
                            {
                                switch (textBoxCg.Name)
                                {
                                    case nameof(LookupResult.Readings):
                                        miningParams[JLField.Readings] = textBoxCg.Text;
                                        break;
                                }
                            }
                        }

                        break;
                    }
            }
        }

        var innerStackPanel = (StackPanel)top.Parent;
        var bottom = (StackPanel)innerStackPanel.Children[1];
        foreach (object child in bottom.Children)
        {
            if (child is TextBox textBox)
            {
                miningParams[JLField.Definitions] = textBox.Text.Replace("\n", "<br/>");
                continue;
            }

            if (child is not TextBlock tb)
            {
                continue;
            }

            textBlock = tb;

            switch (textBlock.Name)
            {
                case nameof(LookupResult.OnReadings):
                    miningParams[JLField.OnReadings] = textBlock.Text;
                    break;
                case nameof(LookupResult.KunReadings):
                    miningParams[JLField.KunReadings] = textBlock.Text;
                    break;
                case nameof(LookupResult.NanoriReadings):
                    miningParams[JLField.NanoriReadings] = textBlock.Text;
                    break;
                case nameof(LookupResult.StrokeCount):
                    miningParams[JLField.StrokeCount] = textBlock.Text;
                    break;
                case nameof(LookupResult.KanjiGrade):
                    miningParams[JLField.KanjiGrade] = textBlock.Text;
                    break;
                case nameof(LookupResult.KanjiComposition):
                    miningParams[JLField.KanjiComposition] = textBlock.Text;
                    break;
                case nameof(LookupResult.KanjiStats):
                    miningParams[JLField.KanjiStats] = textBlock.Text;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid LookupResult type");
            }
        }

        miningParams[JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

        bool miningResult = await Mining.Mine(miningParams).ConfigureAwait(false);

        if (miningResult)
        {
            await Stats.IncrementStat(StatType.CardsMined).ConfigureAwait(false);
        }
    }

    private void Definitions_MouseMove(TextBox tb)
    {
        if (Storage.JapaneseRegex.IsMatch(tb.Text))
        {
            TextBox_MouseMove(tb);
        }

        else if (ConfigManager.HighlightLongestMatch)
        {
            WindowsUtils.Unselect(_lastTextBox);
        }
    }

    private void PopupListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        MouseWheelEventArgs e2 = new(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = e.Source
        };
        PopupListBox.RaiseEvent(e2);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        if (WindowsUtils.CompareKeyGesture(e, ConfigManager.DisableHotkeysKeyGesture))
        {
            ConfigManager.DisableHotkeys = !ConfigManager.DisableHotkeys;
        }

        if (ConfigManager.DisableHotkeys)
        {
            return;
        }

        if (WindowsUtils.CompareKeyGesture(e, ConfigManager.MiningModeKeyGesture))
        {
            if (MiningMode)
            {
                return;
            }

            EnableMiningMode();

            _ = Activate();
            _ = Focus();

            ResultStackPanels.Clear();
            DisplayResults(true);
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.PlayAudioKeyGesture))
        {
            await PlayAudio().ConfigureAwait(false);
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ClosePopupKeyGesture))
        {
            MiningMode = false;
            TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;
            ItemsControlButtons.Visibility = Visibility.Collapsed;

            PopUpScrollViewer.ScrollToTop();

            if (ConfigManager.LookupOnSelectOnly)
            {
                WindowsUtils.Unselect(_lastTextBox);
            }

            _ = Owner.Focus();

            Hide();

            PopupAutoHideTimer.Stop();
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.KanjiModeKeyGesture))
        {
            Storage.Frontend.CoreConfig.KanjiMode = !Storage.Frontend.CoreConfig.KanjiMode;
            LastText = "";
            Storage.Frontend.InvalidateDisplayCache();
            if (Owner != MainWindow.Instance)
            {
                TextBox_MouseMove(_lastTextBox!);
            }

            else
            {
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (Storage.DictsReady)
            {
                WindowsUtils.ShowAddNameWindow(_lastSelectedText);
            }
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (Storage.DictsReady)
            {
                WindowsUtils.ShowAddWordWindow(_lastSelectedText);
            }
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(_lastSelectedText);
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.MotivationKeyGesture))
        {
            await WindowsUtils.Motivate().ConfigureAwait(false);
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowStatsKeyGesture))
        {
            await WindowsUtils.ShowStatsWindow().ConfigureAwait(false);
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.NextDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToNextDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = 0; i < dictCount; i++)
            {
                var button = (Button)ItemsControlButtons.Items[i];

                if (button.Background == Brushes.DodgerBlue)
                {
                    button.ClearValue(BackgroundProperty);

                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    _filteredDict = (Dict)button.Tag;
                    movedToNextDict = true;
                    button.Background = Brushes.DodgerBlue;
                    PopupListBox.Items.Filter = DictFilter;
                    break;
                }
            }

            if (!movedToNextDict)
            {
                ((Button)ItemsControlButtons.Items[0]).Background = Brushes.DodgerBlue;
                PopupListBox.Items.Filter = NoAllDictFilter;
            }
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.PreviousDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToPreviousDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = dictCount - 1; i > 0; i--)
            {
                var button = (Button)ItemsControlButtons.Items[i];

                if (button.Background == Brushes.DodgerBlue)
                {
                    button.ClearValue(BackgroundProperty);

                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    _filteredDict = (Dict)button.Tag;
                    button.Background = Brushes.DodgerBlue;
                    movedToPreviousDict = true;
                    PopupListBox.Items.Filter = DictFilter;
                    break;
                }
            }

            if (foundSelectedButton && !movedToPreviousDict)
            {
                ((Button)ItemsControlButtons.Items[0]).Background = Brushes.DodgerBlue;
                PopupListBox.Items.Filter = NoAllDictFilter;
            }

            else if (!foundSelectedButton)
            {
                for (int i = dictCount - 1; i > 0; i--)
                {
                    var btn = (Button)ItemsControlButtons.Items[i];
                    if (btn.IsEnabled)
                    {
                        _filteredDict = (Dict)btn.Tag;
                        btn.Background = Brushes.DodgerBlue;
                        ((Button)ItemsControlButtons.Items[0]).ClearValue(BackgroundProperty);
                        PopupListBox.Items.Filter = DictFilter;
                        break;
                    }
                }
            }
        }
    }

    private void EnableMiningMode()
    {
        MiningMode = true;

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder.Visibility = Visibility.Visible;
        }

        ItemsControlButtons.Visibility = Visibility.Visible;

        PopUpScrollViewer.ScrollToTop();

        if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
        {
            PopupWindowUtils.SetPopupAutoHideTimer();
        }
    }

    private async Task PlayAudio()
    {
        string? primarySpelling = null;
        string? reading = null;

        StackPanel[] visibleStackPanels = PopupListBox.Items.Cast<StackPanel>()
            .Where(static stackPanel => stackPanel.Visibility is Visibility.Visible).ToArray();

        if (visibleStackPanels.Length is 0)
        {
            return;
        }

        StackPanel innerStackPanel = visibleStackPanels[_playAudioIndex];
        var top = (WrapPanel)innerStackPanel.Children[0];

        foreach (UIElement child in top.Children)
        {
            if (child is TextBox chi)
            {
                switch (chi.Name)
                {
                    case nameof(LookupResult.Readings):
                        reading = chi.Text.Split(",")[0];
                        break;
                }
            }

            else if (child is TextBlock ch)
            {
                switch (ch.Name)
                {
                    case nameof(LookupResult.PrimarySpelling):
                        primarySpelling = ch.Text;
                        break;
                    case nameof(LookupResult.Readings):
                        reading = ch.Text.Split(",")[0];
                        break;
                }
            }

            else if (child is Grid grid)
            {
                foreach (UIElement uiElement in grid.Children)
                {
                    if (uiElement is TextBlock textBlockCg)
                    {
                        switch (textBlockCg.Name)
                        {
                            case nameof(LookupResult.PrimarySpelling):
                                primarySpelling = textBlockCg.Text;
                                break;
                            case nameof(LookupResult.Readings):
                                reading = textBlockCg.Text.Split(",")[0];
                                break;
                        }
                    }
                    else if (uiElement is TextBox textBoxCg)
                    {
                        switch (textBoxCg.Name)
                        {
                            case nameof(LookupResult.Readings):
                                reading = textBoxCg.Text.Split(",")[0];
                                break;
                        }
                    }
                }
            }
        }

        if (primarySpelling is not null)
        {
            await Utils.GetAndPlayAudioFromJpod101(primarySpelling, reading, 1).ConfigureAwait(false);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.LookupOnSelectOnly
            && !ConfigManager.LookupOnLeftClickOnly
            && !ConfigManager.FixedPopupPositioning
            && ChildPopupWindow is { MiningMode: false })
        {
            ChildPopupWindow.Hide();
            ChildPopupWindow.LastText = "";
        }

        if (MiningMode)
        {
            if (!ChildPopupWindow?.IsVisible ?? true)
            {
                PopupAutoHideTimer.Stop();
            }

            return;
        }

        if (ConfigManager.FixedPopupPositioning || UnavoidableMouseEnter)
        {
            return;
        }

        Hide();
        LastText = "";
    }

    private void UiElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((!ConfigManager.LookupOnSelectOnly && !ConfigManager.LookupOnLeftClickOnly)
            || Background.Opacity is 0
            || ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress && !WindowsUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture))
            || (ConfigManager.FixedPopupPositioning && Owner != MainWindow.Instance))
        {
            return;
        }

        //if (ConfigManager.RequireLookupKeyPress
        //    && !Keyboard.Modifiers.HasFlag(ConfigManager.LookupKey))
        //    return;

        ChildPopupWindow ??= new PopupWindow { Owner = this };

        if (ConfigManager.LookupOnSelectOnly)
        {
            ChildPopupWindow.LookupOnSelect((TextBox)sender);
        }

        else
        {
            ChildPopupWindow.TextBox_MouseMove((TextBox)sender);
        }

        if (ConfigManager.FixedPopupPositioning)
        {
            ChildPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition,
                WindowsUtils.DpiAwareFixedPopupYPosition);
        }

        else
        {
            ChildPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
        }
    }

    private void UiElement_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton is MouseButtonState.Pressed && ChildPopupWindow is { IsVisible: true, MiningMode: false })
        {
            e.Handled = true;
            PopupWindow_PreviewMouseDown(ChildPopupWindow);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.LookupOnSelectOnly
            && !ConfigManager.LookupOnLeftClickOnly
            && !ConfigManager.FixedPopupPositioning
            && ChildPopupWindow is { MiningMode: false, UnavoidableMouseEnter: false })
        {
            ChildPopupWindow.Hide();
            ChildPopupWindow.LastText = "";
        }

        if (IsMouseOver || ConfigManager.FixedPopupPositioning)
        {
            return;
        }

        if (MiningMode)
        {
            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt && (!ChildPopupWindow?.IsVisible ?? true))
            {
                PopupAutoHideTimer.Stop();
                PopupAutoHideTimer.Start();
            }

            return;
        }

        Hide();
        LastText = "";

        if (ConfigManager.HighlightLongestMatch && !PopupContextMenu.IsVisible)
        {
            WindowsUtils.Unselect(_lastTextBox);
        }
    }

    public static void PopupWindow_PreviewMouseDown(PopupWindow popupWindow)
    {
        popupWindow.EnableMiningMode();
        WinApi.BringToFront(popupWindow._windowHandle);
        popupWindow.ResultStackPanels.Clear();
        popupWindow.DisplayResults(true);
    }

    //private void Window_Deactivated(object sender, EventArgs e)
    //{
    //    if (!IsKeyboardFocusWithin && (!ChildPopupWindow?.IsVisible ?? true))
    //        MainWindow.Instance.FocusEllipse.Fill = Brushes.Transparent;
    //}

    //private void Window_Activated(object sender, EventArgs e)
    //{
    //    MainWindow.Instance.FocusEllipse.Fill = Brushes.Green;
    //}

    private void GenerateDictTypeButtons()
    {
        DictTypeButtons.Clear();

        var buttonAll = new Button { Content = "All", Margin = new Thickness(1), Background = Brushes.DodgerBlue };
        buttonAll.Click += ButtonAllOnClick;
        DictTypeButtons.Add(buttonAll);

        List<Dict> foundDicts = new();

        foreach (StackPanel stackPanel in ResultStackPanels)
        {
            Dict foundDict = (Dict)stackPanel.Tag;
            foundDicts.Add(foundDict);
        }

        foreach (Dict dict in Storage.Dicts.Values.OrderBy(static dict => dict.Priority).ToList())
        {
            if (!dict.Active || dict.Type is DictType.PitchAccentYomichan || (ConfigManager.HideDictTabsWithNoResults && !foundDicts.Contains(dict)))
            {
                continue;
            }

            var button = new Button { Content = dict.Name, Margin = new Thickness(1), Tag = dict };
            button.Click += DictTypeButtonOnClick;

            if (!foundDicts.Contains(dict))
            {
                button.IsEnabled = false;
            }

            DictTypeButtons.Add(button);
        }
    }

    private void ButtonAllOnClick(object sender, RoutedEventArgs e)
    {
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.ClearValue(BackgroundProperty);
        }

        var button = (Button)sender;
        button.Background = Brushes.DodgerBlue;

        PopupListBox.Items.Filter = NoAllDictFilter;
    }

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.ClearValue(BackgroundProperty);
        }

        var button = (Button)sender;

        button.Background = Brushes.DodgerBlue;

        _filteredDict = (Dict)button.Tag;

        PopupListBox.Items.Filter = DictFilter;
    }
    private bool DictFilter(object item)
    {
        StackPanel items = (StackPanel)item;
        return (Dict)items.Tag == _filteredDict;
    }

    private static bool NoAllDictFilter(object item)
    {
        if (Storage.Frontend.CoreConfig.KanjiMode)
        {
            return true;
        }

        var dict = (Dict)((StackPanel)item).Tag;
        return !dict?.Options?.NoAll?.Value ?? true;
    }
}
