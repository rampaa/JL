using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Caching;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal sealed partial class PopupWindow : Window
{
    public PopupWindow? ChildPopupWindow { get; private set; }
    public bool ContextMenuIsOpening { get; private set; } = false;

    private TextBox? _lastTextBox;

    private int _listBoxIndex;

    private int _currentCharPosition;

    private string? _currentText;

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    private List<LookupResult> _lastLookupResults = new();

    public List<Dict> DictsWithResults { get; } = new();

    private Dict? _filteredDict = null;

    public bool UnavoidableMouseEnter { get; private set; } = false;

    public string? LastText { get; set; }

    public bool MiningMode { get; set; }

    private static string? s_primarySpellingOfLastPlayedAudio = null;

    private static string? s_readingOfLastPlayedAudio = null;

    public static Timer PopupAutoHideTimer { get; } = new();

    public static LRUCache<string, StackPanel[]> StackPanelCache { get; } = new(
        Utils.CacheSize, Utils.CacheSize / 8);

    public PopupWindow()
    {
        InitializeComponent();
        Init();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHandle = new WindowInteropHelper(this).Handle;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(WindowHandle);
        }
        else
        {
            WinApi.PreventActivation(WindowHandle);
        }
    }

    private void Init()
    {
        Background = ConfigManager.PopupBackgroundColor;
        Foreground = ConfigManager.DefinitionsColor;
        FontFamily = ConfigManager.PopupFont;

        WindowsUtils.SetSizeToContentForPopup(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, this);

        KeyGestureUtils.SetInputGestureText(AddNameMenuItem, ConfigManager.ShowAddNameWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(AddWordMenuItem, ConfigManager.ShowAddWordWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(SearchMenuItem, ConfigManager.SearchWithBrowserKeyGesture);
        KeyGestureUtils.SetInputGestureText(StatsMenuItem, ConfigManager.ShowStatsKeyGesture);

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder.Text = string.Create(CultureInfo.InvariantCulture,
                $"Click on an entry's main spelling to mine it,\nor press {ConfigManager.ClosePopupKeyGesture.Key} or click on the main window to exit.");
        }
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        ShowAddNameWindow();
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddWordWindow(LastSelectedText);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        WindowsUtils.SearchWithBrowser(LastSelectedText);
    }

    private void ShowStats(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowStatsWindow();
    }

    public async Task LookupOnCharPosition(TextBox tb, int charPosition, bool enableMiningMode)
    {
        _currentText = tb.Text;
        _currentCharPosition = charPosition;

        if (Owner != MainWindow.Instance
                ? ConfigManager.DisableLookupsForNonJapaneseCharsInPopups
                  && !JapaneseUtils.JapaneseRegex.IsMatch(tb.Text[charPosition].ToString())
                : ConfigManager.DisableLookupsForNonJapaneseCharsInMainWindow
                  && !JapaneseUtils.JapaneseRegex.IsMatch(tb.Text[charPosition].ToString()))
        {
            HidePopup();
            return;
        }

        int endPosition = tb.Text.Length - charPosition > ConfigManager.MaxSearchLength
            ? JapaneseUtils.FindExpressionBoundary(tb.Text[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
            : JapaneseUtils.FindExpressionBoundary(tb.Text, charPosition);

        string text = tb.Text[charPosition..endPosition];

        if (text == LastText && IsVisible)
        {
            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
            }

            return;
        }

        LastText = text;

        List<LookupResult>? lookupResults = LookupUtils.LookupText(text);

        if (lookupResults?.Count > 0)
        {
            _lastTextBox = tb;
            LastSelectedText = lookupResults[0].MatchedText;

            if (ConfigManager.HighlightLongestMatch)
            {
                _ = tb.Focus();
                tb.Select(charPosition, lookupResults[0].MatchedText.Length);
            }

            _lastLookupResults = lookupResults;

            if (enableMiningMode)
            {
                EnableMiningMode();
                DisplayResults(true);

                if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
                {
                    PopupWindowUtils.SetPopupAutoHideTimer();
                }
            }

            else
            {
                DisplayResults(false, text);
            }

            Show();

            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
            }

            if (ConfigManager.Focusable && ConfigManager.PopupFocusOnLookup)
            {
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (ConfigManager.AutoPlayAudio)
            {
                await PlayAudio().ConfigureAwait(false);
            }
        }
        else
        {
            HidePopup();
        }
    }

    public async Task LookupOnMouseMoveOrClick(TextBox tb)
    {
        // Set snapToText to the value of HorizontallyCenterMainWindowText
        // This is a dumb workaround for https://github.com/dotnet/wpf/issues/7651
        // Setting snapToText to true creates other problems but it's better than not being able to lookup stuff when the text is centered
        int charPosition = tb.GetCharacterIndexFromPoint(Mouse.GetPosition(tb), MainWindow.Instance.MainTextBox == tb && ConfigManager.HorizontallyCenterMainWindowText);

        if (charPosition is not -1)
        {
            if (charPosition > 0 && char.IsHighSurrogate(tb.Text[charPosition - 1]))
            {
                --charPosition;
            }

            await LookupOnCharPosition(tb, charPosition, ConfigManager.LookupOnMouseClickOnly).ConfigureAwait(false);
        }
        else
        {
            HidePopup();
        }
    }

    public async Task LookupOnSelect(TextBox tb)
    {
        if (string.IsNullOrWhiteSpace(tb.SelectedText))
        {
            return;
        }

        _lastTextBox = tb;
        LastText = tb.SelectedText;
        LastSelectedText = tb.SelectedText;

        List<LookupResult>? lookupResults = LookupUtils.LookupText(tb.SelectedText);

        if (lookupResults is { Count: > 0 })
        {
            _lastLookupResults = lookupResults;
            EnableMiningMode();
            DisplayResults(true);

            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }

            Show();

            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
            }

            _ = tb.Focus();

            if (ConfigManager.Focusable)
            {
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (ConfigManager.AutoPlayAudio)
            {
                await PlayAudio().ConfigureAwait(false);
            }
        }
        else
        {
            HidePopup();
        }
    }

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X / WindowsUtils.Dpi.DpiScaleX;
        double mouseY = cursorPosition.Y / WindowsUtils.Dpi.DpiScaleY;

        bool needsFlipX = ConfigManager.PopupFlipX && mouseX + ActualWidth > WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth;
        bool needsFlipY = ConfigManager.PopupFlipY && mouseY + ActualHeight > WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight;

        double newLeft;
        double newTop;

        UnavoidableMouseEnter = false;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - (ActualWidth + WindowsUtils.DpiAwareXOffset);
            if (newLeft < WindowsUtils.ActiveScreen.Bounds.X)
            {
                newLeft = WindowsUtils.ActiveScreen.Bounds.X;
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
            newTop = mouseY - (ActualHeight + WindowsUtils.DpiAwareYOffset);
            if (newTop < WindowsUtils.ActiveScreen.Bounds.Y)
            {
                newTop = WindowsUtils.ActiveScreen.Bounds.Y;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + WindowsUtils.DpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + ActualWidth > WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth)
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth - ActualWidth;
        }

        if (newTop + ActualHeight > WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight)
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight - ActualHeight;
        }

        if (mouseX >= newLeft && mouseX <= newLeft + ActualWidth && mouseY >= newTop && mouseY <= newTop + ActualHeight)
        {
            UnavoidableMouseEnter = true;
        }

        Left = newLeft;
        Top = newTop;
    }

    private void UpdatePosition(double x, double y)
    {
        Left = x;
        Top = y;
    }

    private void DisplayResults(bool generateAllResults, string? text = null)
    {
        DictsWithResults.Clear();

        PopupListBox.Items.Filter = NoAllDictFilter;

        if (text is not null && !generateAllResults && StackPanelCache.TryGet(text, out StackPanel[] data))
        {
            int resultCount = Math.Min(data.Length, ConfigManager.MaxNumResultsNotInMiningMode);
            StackPanel[] popupItemSource = new StackPanel[resultCount];

            for (int i = 0; i < resultCount; i++)
            {
                StackPanel stackPanel = data[i];

                Dict dict = (Dict)stackPanel.Tag;
                if (!DictsWithResults.Contains(dict))
                {
                    DictsWithResults.Add(dict);
                }

                popupItemSource[i] = stackPanel;
            }

            PopupListBox.ItemsSource = popupItemSource;
            GenerateDictTypeButtons();
            UpdateLayout();

            if (PopupListBox.Items.Count > 0)
            {
                PopupListBox.ScrollIntoView(PopupListBox.Items[0]);
            }
        }

        else
        {
            int resultCount = generateAllResults
                ? _lastLookupResults.Count
                : Math.Min(_lastLookupResults.Count, ConfigManager.MaxNumResultsNotInMiningMode);

            StackPanel[] popupItemSource = new StackPanel[resultCount];

            for (int i = 0; i < resultCount; i++)
            {
                LookupResult lookupResult = _lastLookupResults[i];

                if (!DictsWithResults.Contains(lookupResult.Dict))
                {
                    DictsWithResults.Add(lookupResult.Dict);
                }

                popupItemSource[i] = MakeResultStackPanel(lookupResult, i, resultCount);
            }

            PopupListBox.ItemsSource = popupItemSource;
            GenerateDictTypeButtons();
            UpdateLayout();

            if (PopupListBox.Items.Count > 0)
            {
                PopupListBox.ScrollIntoView(PopupListBox.Items[0]);
            }

            // we might cache incomplete results if we don't wait until all dicts are loaded
            if (text is not null && !generateAllResults && DictUtils.DictsReady && !DictUtils.UpdatingJmdict && !DictUtils.UpdatingJmnedict && !DictUtils.UpdatingKanjidic)
            {
                StackPanelCache.AddReplace(text, popupItemSource);
            }
        }
    }

    public StackPanel MakeResultStackPanel(LookupResult result,
        int index, int resultsCount)
    {
        // top
        WrapPanel top = new();

        TextBlock textBlockMatchedText = new()
        {
            Name = nameof(result.MatchedText),
            Text = result.MatchedText,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _ = top.Children.Add(textBlockMatchedText);

        TextBlock textBlockDeconjugatedMatchedText = new()
        {
            Name = nameof(result.DeconjugatedMatchedText),
            Text = result.DeconjugatedMatchedText,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        _ = top.Children.Add(textBlockDeconjugatedMatchedText);

        if (result.EdictId is not 0)
        {
            TextBlock edictIdTextBlock = new()
            {
                Name = nameof(result.EdictId),
                Text = result.EdictId.ToString(CultureInfo.InvariantCulture),
                Visibility = Visibility.Collapsed
            };
            _ = top.Children.Add(edictIdTextBlock);
        }

        TextBlock primarySpellingTextBlock = new()
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
            //UndoLimit = 0,
            Cursor = Cursors.Arrow,
            //IsInactiveSelectionHighlightEnabled = true,
            ContextMenu = PopupContextMenu
        };
        primarySpellingTextBlock.MouseEnter += PrimarySpelling_MouseEnter; // for audio
        primarySpellingTextBlock.MouseLeave += PrimarySpelling_MouseLeave; // for audio
        primarySpellingTextBlock.PreviewMouseUp += PrimarySpelling_PreviewMouseUp; // for mining

        Dict? pitchDict = DictUtils.Dicts.Values.FirstOrDefault(static dict => dict.Type is DictType.PitchAccentYomichan);
        if (pitchDict?.Active ?? false)
        {
            if (result.Readings is not null)
            {
                _ = top.Children.Add(primarySpellingTextBlock);
            }

            else
            {
                Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                    result.AlternativeSpellings,
                    null,
                    primarySpellingTextBlock.Text.Split(", "),
                    primarySpellingTextBlock.Margin.Left,
                    pitchDict);

                if (pitchAccentGrid.Children.Count is 0)
                {
                    _ = top.Children.Add(primarySpellingTextBlock);
                }

                else
                {
                    _ = pitchAccentGrid.Children.Add(primarySpellingTextBlock);
                    _ = top.Children.Add(pitchAccentGrid);
                }
            }
        }
        else
        {
            _ = top.Children.Add(primarySpellingTextBlock);
        }

        if (result.PrimarySpellingOrthographyInfoList is not null
            && (result.Dict.Options?.POrthographyInfo?.Value ?? true))
        {
            TextBlock textBlockPOrthographyInfo = new()
            {
                Name = nameof(result.PrimarySpellingOrthographyInfoList),
                Text = string.Create(CultureInfo.InvariantCulture, $"({string.Join(", ", result.PrimarySpellingOrthographyInfoList)})"),
                Foreground = DictOptionManager.POrthographyInfoColor,
                FontSize = result.Dict.Options?.POrthographyInfoFontSize?.Value ?? 15,
                Margin = new Thickness(5, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _ = top.Children.Add(textBlockPOrthographyInfo);
        }

        if (result.Readings is not null)
        {
            string readingsText = result.ReadingsOrthographyInfoList?.Count > 0 && (result.Dict.Options?.ROrthographyInfo?.Value ?? true)
                ? PopupWindowUtils.ReadingsToText(result.Readings, result.ReadingsOrthographyInfoList)
                : string.Join(", ", result.Readings);

            if (MiningMode)
            {
                TouchScreenTextBox readingTextBox = new()
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
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = result.KunReadings is null && result.OnReadings is null
                    ? Visibility.Visible
                    : Visibility.Collapsed
                };

                if (pitchDict?.Active ?? false)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBox.Text.Split(", "),
                        readingTextBox.Margin.Left,
                        pitchDict);

                    if (pitchAccentGrid.Children.Count is 0)
                    {
                        _ = top.Children.Add(readingTextBox);
                    }
                    else
                    {
                        _ = pitchAccentGrid.Children.Add(readingTextBox);
                        _ = top.Children.Add(pitchAccentGrid);
                    }
                }

                else
                {
                    _ = top.Children.Add(readingTextBox);
                }

                readingTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                readingTextBox.MouseMove += UiElement_MouseMove;
                readingTextBox.LostFocus += Unselect;
                readingTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                readingTextBox.MouseLeave += OnMouseLeave;
            }

            else
            {
                TextBlock readingTextBlock = new()
                {
                    Name = nameof(result.Readings),
                    Text = readingsText,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(5, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = result.KunReadings is null && result.OnReadings is null
                    ? Visibility.Visible
                    : Visibility.Collapsed
                };

                if (pitchDict?.Active ?? false)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBlock.Text.Split(", "),
                        readingTextBlock.Margin.Left,
                        pitchDict);

                    if (pitchAccentGrid.Children.Count is 0)
                    {
                        _ = top.Children.Add(readingTextBlock);
                    }

                    else
                    {
                        _ = pitchAccentGrid.Children.Add(readingTextBlock);
                        _ = top.Children.Add(pitchAccentGrid);
                    }
                }

                else
                {
                    _ = top.Children.Add(readingTextBlock);
                }
            }
        }

        if (result.AlternativeSpellings is not null)
        {
            string alternativeSpellingsText = result.AlternativeSpellingsOrthographyInfoList?.Count > 0 && (result.Dict.Options?.AOrthographyInfo?.Value ?? true)
                ? PopupWindowUtils.AlternativeSpellingsToText(result.AlternativeSpellings, result.AlternativeSpellingsOrthographyInfoList)
                : string.Create(CultureInfo.InvariantCulture, $"({string.Join(", ", result.AlternativeSpellings)})");

            if (MiningMode)
            {
                TouchScreenTextBox alternativeSpellingsTexBox = new()
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
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                alternativeSpellingsTexBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                alternativeSpellingsTexBox.MouseMove += UiElement_MouseMove;
                alternativeSpellingsTexBox.LostFocus += Unselect;
                alternativeSpellingsTexBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                alternativeSpellingsTexBox.MouseLeave += OnMouseLeave;
                _ = top.Children.Add(alternativeSpellingsTexBox);
            }
            else
            {
                TextBlock alternativeSpellingsTexBlock = new()
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
                _ = top.Children.Add(alternativeSpellingsTexBlock);
            }
        }

        if (result.Process is not null)
        {
            TextBlock processTextBlock = new()
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
            _ = top.Children.Add(processTextBlock);
        }

        if (result.Frequencies is not null)
        {
            string? freqText = PopupWindowUtils.FrequenciesToText(result.Frequencies);

            if (freqText is not null)
            {
                TextBlock frequencyTextBlock = new()
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
                _ = top.Children.Add(frequencyTextBlock);
            }
        }

        TextBlock dictTypeTextBlock = new()
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
        _ = top.Children.Add(dictTypeTextBlock);

        // bottom
        StackPanel bottom = new();

        if (result.FormattedDefinitions is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox definitionsTextBox = new()
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
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };

                definitionsTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                definitionsTextBox.MouseMove += UiElement_MouseMove;
                definitionsTextBox.LostFocus += Unselect;
                definitionsTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                definitionsTextBox.MouseLeave += OnMouseLeave;
                _ = bottom.Children.Add(definitionsTextBox);
            }

            else
            {
                TextBlock definitionsTextBlock = new()
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
                _ = bottom.Children.Add(definitionsTextBlock);
            }
        }

        // KANJIDIC
        if (result.OnReadings is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox onReadingsTextBox = new()
                {
                    Name = nameof(result.OnReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"On: {string.Join(", ", result.OnReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Padding = new Thickness(0),
                    IsReadOnly = true,
                    IsUndoEnabled = false,
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu
                };

                onReadingsTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                onReadingsTextBox.MouseMove += UiElement_MouseMove;
                onReadingsTextBox.LostFocus += Unselect;
                onReadingsTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                onReadingsTextBox.MouseLeave += OnMouseLeave;
                _ = bottom.Children.Add(onReadingsTextBox);
            }

            else
            {
                TextBlock onReadingsTextBlock = new()
                {
                    Name = nameof(result.OnReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"On: {string.Join(", ", result.OnReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = bottom.Children.Add(onReadingsTextBlock);
            }
        }

        if (result.KunReadings is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox kunReadingsTextBox = new()
                {
                    Name = nameof(result.KunReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Kun: {string.Join(", ", result.KunReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Padding = new Thickness(0),
                    IsReadOnly = true,
                    IsUndoEnabled = false,
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu
                };

                kunReadingsTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                kunReadingsTextBox.MouseMove += UiElement_MouseMove;
                kunReadingsTextBox.LostFocus += Unselect;
                kunReadingsTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                kunReadingsTextBox.MouseLeave += OnMouseLeave;
                _ = bottom.Children.Add(kunReadingsTextBox);
            }

            else
            {
                TextBlock kunReadingsTextBlock = new()
                {
                    Name = nameof(result.KunReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Kun: {string.Join(", ", result.KunReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = bottom.Children.Add(kunReadingsTextBlock);
            }
        }

        if (result.NanoriReadings is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox nanoriReadingsTextBox = new()
                {
                    Name = nameof(result.NanoriReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Nanori: {string.Join(", ", result.NanoriReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Padding = new Thickness(0),
                    IsReadOnly = true,
                    IsUndoEnabled = false,
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu
                };

                nanoriReadingsTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                nanoriReadingsTextBox.MouseMove += UiElement_MouseMove;
                nanoriReadingsTextBox.LostFocus += Unselect;
                nanoriReadingsTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                nanoriReadingsTextBox.MouseLeave += OnMouseLeave;
                _ = bottom.Children.Add(nanoriReadingsTextBox);
            }

            else
            {
                TextBlock nanoriReadingsTextBlock = new()
                {
                    Name = nameof(result.NanoriReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Nanori: {string.Join(", ", result.NanoriReadings)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = bottom.Children.Add(nanoriReadingsTextBlock);
            }
        }

        if (result.KanjiGrade > -1)
        {
            string gradeText = PopupWindowUtils.GradeToText(result.KanjiGrade);
            TextBlock gradeTextBlock = new()
            {
                Name = nameof(result.KanjiGrade),
                Text = string.Create(CultureInfo.InvariantCulture, $"{nameof(result.KanjiGrade)}: {gradeText}"),
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _ = bottom.Children.Add(gradeTextBlock);
        }

        if (result.StrokeCount > 0)
        {
            TextBlock strokeCountTextBlock = new()
            {
                Name = nameof(result.StrokeCount),
                Text = string.Create(CultureInfo.InvariantCulture, $"Strokes: {result.StrokeCount}"),
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _ = bottom.Children.Add(strokeCountTextBlock);
        }

        if (result.KanjiComposition is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox compositionTextBox = new()
                {
                    Name = nameof(result.KanjiComposition),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Composition: {result.KanjiComposition}"),
                    Foreground = ConfigManager.DefinitionsColor,
                    FontSize = ConfigManager.DefinitionsFontSize,
                    Margin = new Thickness(2, 2, 2, 2),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    Padding = new Thickness(0),
                    IsReadOnly = true,
                    IsUndoEnabled = false,
                    UndoLimit = 0,
                    Cursor = Cursors.Arrow,
                    SelectionBrush = ConfigManager.HighlightColor,
                    IsInactiveSelectionHighlightEnabled = true,
                    ContextMenu = PopupContextMenu
                };

                compositionTextBox.PreviewMouseUp += UiElement_PreviewMouseUp;
                compositionTextBox.MouseMove += UiElement_MouseMove;
                compositionTextBox.LostFocus += Unselect;
                compositionTextBox.PreviewMouseRightButtonUp += TextBoxPreviewMouseRightButtonUp;
                compositionTextBox.MouseLeave += OnMouseLeave;
                _ = bottom.Children.Add(compositionTextBox);
            }

            else
            {
                TextBlock compositionTextBlock = new()
                {
                    Name = nameof(result.KanjiComposition),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Composition: {result.KanjiComposition}"),
                    Foreground = ConfigManager.DefinitionsColor,
                    FontSize = ConfigManager.DefinitionsFontSize,
                    Margin = new Thickness(2, 2, 2, 2),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = bottom.Children.Add(compositionTextBlock);
            }
        }

        if (result.KanjiStats is not null)
        {
            TextBlock kanjiStatsTextBlock = new()
            {
                Name = nameof(result.KanjiStats),
                Text = string.Create(CultureInfo.InvariantCulture, $"Statistics:\n{result.KanjiStats}"),
                Foreground = ConfigManager.DefinitionsColor,
                FontSize = ConfigManager.DefinitionsFontSize,
                Margin = new Thickness(2, 2, 2, 2),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            _ = bottom.Children.Add(kanjiStatsTextBlock);
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

        return new StackPanel { Margin = new Thickness(4, 2, 4, 2), Tag = result.Dict, Children = { top, bottom } };
    }

    private static void Unselect(object sender, RoutedEventArgs e)
    {
        WindowsUtils.Unselect((TextBox)sender);
    }

    private void TextBoxPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        AddNameMenuItem.IsEnabled = DictUtils.CustomNameDictReady;
        AddWordMenuItem.IsEnabled = DictUtils.CustomWordDictReady;
        LastSelectedText = ((TextBox)sender).SelectedText;
    }

    private async void UiElement_MouseMove(object sender, MouseEventArgs? e)
    {
        if (ConfigManager.InactiveLookupMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnMouseClickOnly
            || PopupContextMenu.IsVisible
            || (ConfigManager.RequireLookupKeyPress
                && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        ChildPopupWindow ??= new PopupWindow { Owner = this };

        if (ChildPopupWindow.MiningMode)
        {
            return;
        }

        if (MiningMode)
        {
            TextBox tb = (TextBox)sender;
            if (JapaneseUtils.JapaneseRegex.IsMatch(tb.Text))
            {
                await ChildPopupWindow.LookupOnMouseMoveOrClick(tb).ConfigureAwait(false);
            }

            else if (ConfigManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(ChildPopupWindow._lastTextBox);
            }
        }
    }

    private void PrimarySpelling_MouseEnter(object sender, MouseEventArgs e)
    {
        TextBlock textBlock = (TextBlock)sender;
        _listBoxIndex = (int)textBlock.Tag;
        LastSelectedText = _lastLookupResults[_listBoxIndex].PrimarySpelling;
    }

    private void PrimarySpelling_MouseLeave(object sender, MouseEventArgs e)
    {
        _listBoxIndex = 0;
    }

    private async Task Mine(Panel top)
    {
        Dictionary<JLField, string> miningParams = new();

        if (_currentText is not null)
        {
            miningParams[JLField.SourceText] = _currentText;
            miningParams[JLField.Sentence] = JapaneseUtils.FindSentence(_currentText, _currentCharPosition);
        }

        HidePopup();

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

        StackPanel innerStackPanel = (StackPanel)top.Parent;
        StackPanel bottom = (StackPanel)innerStackPanel.Children[1];
        foreach (object child in bottom.Children)
        {
            if (child is TextBox textBox)
            {
                switch (textBox.Name)
                {
                    case nameof(LookupResult.FormattedDefinitions):
                        miningParams[JLField.Definitions] = textBox.Text.Replace("\n", "<br/>", StringComparison.Ordinal);
                        break;
                    case nameof(LookupResult.OnReadings):
                        miningParams[JLField.OnReadings] = textBox.Text[4..];
                        break;
                    case nameof(LookupResult.KunReadings):
                        miningParams[JLField.KunReadings] = textBox.Text[5..];
                        break;
                    case nameof(LookupResult.NanoriReadings):
                        miningParams[JLField.NanoriReadings] = textBox.Text[8..];
                        break;
                    case nameof(LookupResult.KanjiComposition):
                        miningParams[JLField.KanjiComposition] = textBox.Text[13..];
                        break;
                }
                continue;
            }

            if (child is not TextBlock tb)
            {
                continue;
            }

            switch (tb.Name)
            {
                case nameof(LookupResult.StrokeCount):
                    miningParams[JLField.StrokeCount] = tb.Text;
                    break;
                case nameof(LookupResult.KanjiGrade):
                    miningParams[JLField.KanjiGrade] = tb.Text;
                    break;
                case nameof(LookupResult.KanjiStats):
                    miningParams[JLField.KanjiStats] = tb.Text;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid LookupResult type");
            }
        }

        miningParams[JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

        bool mined = await Mining.Mine(miningParams).ConfigureAwait(false);

        if (mined)
        {
            Stats.IncrementStat(StatType.CardsMined);
        }
    }

    private async void PrimarySpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == ConfigManager.CopyPrimarySpellingToClipboardMouseButton)
        {
            WindowsUtils.CopyTextToClipboard(((TextBlock)sender).Text);
        }

        if (!MiningMode || e.ChangedButton != ConfigManager.MineMouseButton)
        {
            return;
        }

        TextBlock textBlock = (TextBlock)sender;

        WrapPanel top = textBlock.Parent is Grid primarySpellingGrid
            ? (WrapPanel)primarySpellingGrid.Parent
            : (WrapPanel)textBlock.Parent;

        await Mine(top).ConfigureAwait(false);
    }

    private void ShowAddNameWindow()
    {
        string primarySpelling = _lastLookupResults[_listBoxIndex].PrimarySpelling;

        IList<string>? readingList = _lastLookupResults[_listBoxIndex].Readings;
        string readings = readingList is null
            ? ""
            : string.Join("; ", readingList);

        if (readings == primarySpelling)
        {
            readings = "";
        }

        WindowsUtils.ShowAddNameWindow(LastSelectedText, readings);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    private void SelectNextLookupResult()
    {
        int nextItemIndex = PopupListBox.SelectedIndex + 1 < PopupListBox.Items.Count
            ? PopupListBox.SelectedIndex + 1
            : 0;

        PopupListBox.SelectedIndex = nextItemIndex;

        PopupListBox.ScrollIntoView(PopupListBox.Items.GetItemAt(nextItemIndex));
    }

    private void SelectPreviousLookupResult()
    {
        int nextItemIndex = PopupListBox.SelectedIndex - 1 > -1
            ? PopupListBox.SelectedIndex - 1
            : PopupListBox.Items.Count - 1;

        PopupListBox.SelectedIndex = nextItemIndex;

        PopupListBox.ScrollIntoView(PopupListBox.Items.GetItemAt(nextItemIndex));
    }

    public async Task HandleHotKey(KeyGesture keyGesture)
    {
        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.DisableHotkeysKeyGesture))
        {
            ConfigManager.DisableHotkeys = !ConfigManager.DisableHotkeys;

            if (ConfigManager.GlobalHotKeys)
            {
                if (ConfigManager.DisableHotkeys)
                {
                    WinApi.UnregisterAllHotKeys(MainWindow.Instance.WindowHandle);
                }
                else
                {
                    WinApi.RegisterAllHotKeys(MainWindow.Instance.WindowHandle);
                }
            }
        }

        if (ConfigManager.DisableHotkeys)
        {
            return;
        }

        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MiningModeKeyGesture))
        {
            if (MiningMode)
            {
                return;
            }

            EnableMiningMode();

            if (ConfigManager.Focusable)
            {
                _ = Activate();
            }

            _ = Focus();

            DisplayResults(true);

            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.PlayAudioKeyGesture))
        {
            await PlayAudio().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ClosePopupKeyGesture))
        {
            _ = Owner.Focus();

            HidePopup();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.KanjiModeKeyGesture))
        {
            CoreConfig.KanjiMode = !CoreConfig.KanjiMode;
            LastText = "";
            Utils.Frontend.InvalidateDisplayCache();

            if (Owner != MainWindow.Instance)
            {
                UiElement_MouseMove(_lastTextBox!, null);
            }

            else
            {
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (DictUtils.CustomNameDictReady)
            {
                ShowAddNameWindow();
                PopupAutoHideTimer.Start();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (DictUtils.CustomWordDictReady)
            {
                WindowsUtils.ShowAddWordWindow(LastSelectedText);
                PopupAutoHideTimer.Start();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(LastSelectedText);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MotivationKeyGesture))
        {
            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowStatsKeyGesture))
        {
            WindowsUtils.ShowStatsWindow();
            PopupAutoHideTimer.Start();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.NextDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToNextDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = 0; i < dictCount; i++)
            {
                Button button = (Button)ItemsControlButtons.Items[i];

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

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.PreviousDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToPreviousDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = dictCount - 1; i > 0; i--)
            {
                Button button = (Button)ItemsControlButtons.Items[i];

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
                    Button btn = (Button)ItemsControlButtons.Items[i];
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

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleMinimizedStateKeyGesture))
        {
            MainWindow mainWindow = MainWindow.Instance;
            WindowsUtils.HidePopups(mainWindow.FirstPopupWindow);

            if (ConfigManager.Focusable)
            {
                mainWindow.WindowState = mainWindow.WindowState is WindowState.Minimized
                    ? WindowState.Normal
                    : WindowState.Minimized;
            }

            else
            {
                if (mainWindow.WindowState is WindowState.Minimized)
                {
                    WinApi.RestoreWindow(mainWindow.WindowHandle);
                }

                else
                {
                    WinApi.MinimizeWindow(mainWindow.WindowHandle);
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SelectedTextToSpeechKeyGesture))
        {
            if (MiningMode
                && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null
                && LastSelectedText is not null)
            {
                await SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, LastSelectedText, CoreConfig.AudioVolume).ConfigureAwait(false);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SelectNextLookupResultKeyGesture))
        {
            if (MiningMode)
            {
                SelectNextLookupResult();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SelectPreviousLookupResultKeyGesture))
        {
            if (MiningMode)
            {
                SelectPreviousLookupResult();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MineSelectedLookupResultKeyGesture))
        {
            if (MiningMode && PopupListBox.SelectedItem is not null)
            {
                WrapPanel? top = ((StackPanel)PopupListBox.SelectedItem).Children.OfType<WrapPanel>().FirstOrDefault();

                if (top is not null)
                {
                    await Mine(top).ConfigureAwait(false);
                }
            }
        }
    }

    private void EnableMiningMode()
    {
        MiningMode = true;

        if (ConfigManager.ShowMiningModeReminder && Owner == MainWindow.Instance)
        {
            TextBlockMiningModeReminder.Visibility = Visibility.Visible;
        }

        ItemsControlButtons.Visibility = Visibility.Visible;
    }

    private async Task PlayAudio()
    {
        string? primarySpelling = null;
        string? reading = null;

        List<StackPanel> visibleStackPanels = PopupListBox.Items.Cast<StackPanel>()
            .Where(static stackPanel => stackPanel.Visibility is Visibility.Visible).ToList();

        if (visibleStackPanels.Count is 0)
        {
            return;
        }

        StackPanel innerStackPanel = visibleStackPanels[_listBoxIndex];
        WrapPanel top = (WrapPanel)innerStackPanel.Children[0];

        foreach (UIElement child in top.Children)
        {
            if (child is TextBox chi)
            {
                switch (chi.Name)
                {
                    case nameof(LookupResult.Readings):
                        reading = chi.Text.Split(',')[0];
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
                        reading = ch.Text.Split(',')[0];
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
                                reading = textBlockCg.Text.Split(',')[0];
                                break;
                        }
                    }
                    else if (uiElement is TextBox textBoxCg)
                    {
                        switch (textBoxCg.Name)
                        {
                            case nameof(LookupResult.Readings):
                                reading = textBoxCg.Text.Split(',')[0];
                                break;
                        }
                    }
                }
            }
        }

        if (primarySpelling is not null)
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

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        HidePopup();
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.LookupOnSelectOnly
            && !ConfigManager.LookupOnMouseClickOnly
            && ChildPopupWindow is { IsVisible: true, MiningMode: false })
        {
            ChildPopupWindow.HidePopup();
        }

        if (MiningMode)
        {
            if (!ChildPopupWindow?.IsVisible ?? true)
            {
                PopupAutoHideTimer.Stop();
            }

            return;
        }

        if (UnavoidableMouseEnter
            || (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance))
        {
            return;
        }

        HidePopup();
    }

    private async void UiElement_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        LastSelectedText = ((TextBox)sender).SelectedText;

        if (ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture))
            || ((!ConfigManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!ConfigManager.LookupOnMouseClickOnly || e.ChangedButton != ConfigManager.LookupOnClickMouseButton)))
        {
            return;
        }

        ChildPopupWindow ??= new PopupWindow { Owner = this };

        if (ConfigManager.LookupOnSelectOnly)
        {
            await ChildPopupWindow.LookupOnSelect((TextBox)sender).ConfigureAwait(false);
        }

        else
        {
            await ChildPopupWindow.LookupOnMouseMoveOrClick((TextBox)sender).ConfigureAwait(false);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (ChildPopupWindow is { IsVisible: true, MiningMode: false, UnavoidableMouseEnter: false })
        {
            ChildPopupWindow.HidePopup();
        }

        if (IsMouseOver)
        {
            return;
        }

        if (MiningMode)
        {
            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                if (PopupContextMenu.IsVisible
                    || AddWordWindow.IsItVisible()
                    || AddNameWindow.IsItVisible()
                    || StatsWindow.IsItVisible())
                {
                    PopupAutoHideTimer.Stop();
                }

                else if (!ChildPopupWindow?.IsVisible ?? true)
                {
                    PopupAutoHideTimer.Stop();
                    PopupAutoHideTimer.Start();
                }
            }
        }

        else
        {
            HidePopup();
        }
    }

    public static void ShowMiningModeResults(PopupWindow popupWindow)
    {
        popupWindow.EnableMiningMode();
        WinApi.BringToFront(popupWindow.WindowHandle);
        popupWindow.DisplayResults(true);

        if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
        {
            PopupWindowUtils.SetPopupAutoHideTimer();
        }
    }

    private void GenerateDictTypeButtons()
    {
        List<Button> buttons = new(DictUtils.Dicts.Values.Count);
        Button buttonAll = new() { Content = "All", Margin = new Thickness(1), Background = Brushes.DodgerBlue };
        buttonAll.Click += ButtonAllOnClick;
        buttons.Add(buttonAll);

        foreach (Dict dict in DictUtils.Dicts.Values.OrderBy(static dict => dict.Priority).ToList())
        {
            if (!dict.Active || dict.Type is DictType.PitchAccentYomichan || (ConfigManager.HideDictTabsWithNoResults && !DictsWithResults.Contains(dict)))
            {
                continue;
            }

            Button button = new() { Content = dict.Name, Margin = new Thickness(1), Tag = dict };
            button.Click += DictTypeButtonOnClick;

            if (!DictsWithResults.Contains(dict))
            {
                button.IsEnabled = false;
            }

            buttons.Add(button);
        }

        ItemsControlButtons.ItemsSource = buttons;
    }

    private void ButtonAllOnClick(object sender, RoutedEventArgs e)
    {
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.ClearValue(BackgroundProperty);
        }

        Button button = (Button)sender;
        button.Background = Brushes.DodgerBlue;

        PopupListBox.Items.Filter = NoAllDictFilter;
    }

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.ClearValue(BackgroundProperty);
        }

        Button button = (Button)sender;

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
        if (CoreConfig.KanjiMode)
        {
            return true;
        }

        Dict dict = (Dict)((StackPanel)item).Tag;
        return !dict.Options?.NoAll?.Value ?? true;
    }

    private void PopupContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue
            && !IsMouseOver
            && !AddWordWindow.IsItVisible()
            && !AddNameWindow.IsItVisible()
            && !StatsWindow.IsItVisible())
        {
            PopupAutoHideTimer.Start();
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ChildPopupWindow is { MiningMode: true })
        {
            if (e.ChangedButton is not MouseButton.Right)
            {
                WindowsUtils.HidePopups(ChildPopupWindow);
            }
        }

        else if (e.ChangedButton == ConfigManager.MiningModeMouseButton)
        {
            if (!MiningMode)
            {
                ShowMiningModeResults(this);
            }

            else if (ChildPopupWindow is { IsVisible: true, MiningMode: false })
            {
                ShowMiningModeResults(ChildPopupWindow);
            }
        }
    }

    public void HidePopup()
    {
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopup = Owner == mainWindow;

        if (isFirstPopup
            && (ConfigManager.TextOnlyVisibleOnHover || ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover))
        {
            _ = mainWindow.ChangeVisibility().ConfigureAwait(true);
        }

        if (!IsVisible)
        {
            return;
        }

        MiningMode = false;
        TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;
        ItemsControlButtons.Visibility = Visibility.Collapsed;
        ItemsControlButtons.ItemsSource = null;
        PopupListBox.ItemsSource = null;
        LastText = "";
        PopupAutoHideTimer.Stop();

        Hide();

        if (isFirstPopup)
        {
            WinApi.ActivateWindow(mainWindow.WindowHandle);

            if (ConfigManager.HighlightLongestMatch && !mainWindow.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_lastTextBox);
            }
        }

        else
        {
            PopupWindow previousPopup = (PopupWindow)Owner;
            WinApi.ActivateWindow(previousPopup.WindowHandle);
            if (ConfigManager.HighlightLongestMatch && !previousPopup.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_lastTextBox);
            }
        }
    }

    private void Window_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ContextMenuIsOpening = true;
        WindowsUtils.HidePopups(ChildPopupWindow);
        ContextMenuIsOpening = false;
    }
}
