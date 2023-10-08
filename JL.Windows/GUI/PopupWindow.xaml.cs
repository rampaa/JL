using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Caching;
using JL.Core;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Lookup;
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
    private bool ContextMenuIsOpening { get; set; } = false;

    private TextBox? _previousTextBox;

    private TextBox? _lastInteractedTextBox;

    private int _listBoxIndex = 0;

    private int _firstVisibleListBoxIndex = 0;

    private int _currentCharPosition;

    private string _currentText = "";

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    public List<LookupResult> LastLookupResults { get; private set; } = new();

    public List<Dict> DictsWithResults { get; } = new();

    private Dict? _filteredDict = null;

    public bool UnavoidableMouseEnter { get; private set; } = false;

    public string? LastText { get; set; }

    public bool MiningMode { get; private set; }

    private static string? s_primarySpellingOfLastPlayedAudio = null;

    private static string? s_readingOfLastPlayedAudio = null;

    public static Timer PopupAutoHideTimer { get; } = new();

    public static LRUCache<string, StackPanel[]> StackPanelCache { get; } = new(
        Utils.CacheSize, Utils.CacheSize / 8);

    private ScrollViewer? _popupListViewScrollViewer;

    public PopupWindow()
    {
        InitializeComponent();
        Init();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHandle = new WindowInteropHelper(this).Handle;
        _popupListViewScrollViewer = PopupListView.GetChildOfType<ScrollViewer>();
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
        ShowAddWordWindow();
    }

    private void ShowAddWordWindow()
    {
        string text = _lastInteractedTextBox?.SelectionLength > 0
            ? _lastInteractedTextBox.SelectedText
            : LastLookupResults[_listBoxIndex].PrimarySpelling;

        WindowsUtils.ShowAddWordWindow(text);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        SearchWithBrowser();
    }

    private void SearchWithBrowser()
    {
        string text = _lastInteractedTextBox?.SelectionLength > 0
            ? _lastInteractedTextBox.SelectedText
            : LastLookupResults[_listBoxIndex].PrimarySpelling;

        WindowsUtils.SearchWithBrowser(text);
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
            _previousTextBox = tb;
            LastSelectedText = lookupResults[0].MatchedText;

            if (ConfigManager.HighlightLongestMatch)
            {
                WinApi.ActivateWindow(Owner == MainWindow.Instance
                    ? MainWindow.Instance.WindowHandle
                    : ((PopupWindow)Owner).WindowHandle);

                _ = tb.Focus();
                tb.Select(charPosition, lookupResults[0].MatchedText.Length);
            }

            LastLookupResults = lookupResults;

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

            _firstVisibleListBoxIndex = GetFirstVisibleListBoxItemIndex();
            _listBoxIndex = _firstVisibleListBoxIndex;

            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
            }

            if (ConfigManager.Focusable
                && (enableMiningMode || ConfigManager.PopupFocusOnLookup))
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

        _previousTextBox = tb;
        LastText = tb.SelectedText;
        LastSelectedText = tb.SelectedText;
        _currentCharPosition = tb.SelectionStart;

        List<LookupResult>? lookupResults = LookupUtils.LookupText(tb.SelectedText);

        if (lookupResults?.Count > 0)
        {
            LastLookupResults = lookupResults;
            EnableMiningMode();
            DisplayResults(true);

            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }

            Show();

            _firstVisibleListBoxIndex = GetFirstVisibleListBoxItemIndex();
            _listBoxIndex = _firstVisibleListBoxIndex;

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

    public void DisplayResults(bool generateAllResults, string? text = null)
    {
        DictsWithResults.Clear();

        PopupListView.Items.Filter = NoAllDictFilter;

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

            PopupListView.ItemsSource = popupItemSource;
            GenerateDictTypeButtons();
            UpdateLayout();
        }

        else
        {
            _ = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict);
            bool pitchDictIsActive = pitchDict?.Active ?? false;
            Dict jmdict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
            bool showPOrthographyInfo = jmdict.Options?.POrthographyInfo?.Value ?? true;
            bool showROrthographyInfo = jmdict.Options?.ROrthographyInfo?.Value ?? true;
            bool showAOrthographyInfo = jmdict.Options?.AOrthographyInfo?.Value ?? true;

            int resultCount = generateAllResults
                ? LastLookupResults.Count
                : Math.Min(LastLookupResults.Count, ConfigManager.MaxNumResultsNotInMiningMode);

            StackPanel[] popupItemSource = new StackPanel[resultCount];

            for (int i = 0; i < resultCount; i++)
            {
                LookupResult lookupResult = LastLookupResults[i];

                if (!DictsWithResults.Contains(lookupResult.Dict))
                {
                    DictsWithResults.Add(lookupResult.Dict);
                }

                popupItemSource[i] = MakeResultStackPanel(lookupResult, i, resultCount, pitchDict, pitchDictIsActive, showPOrthographyInfo, showROrthographyInfo, showAOrthographyInfo);
            }

            PopupListView.ItemsSource = popupItemSource;
            GenerateDictTypeButtons();
            UpdateLayout();

            // we might cache incomplete results if we don't wait until all dicts are loaded
            if (text is not null && !generateAllResults && DictUtils.DictsReady && !DictUtils.UpdatingJmdict && !DictUtils.UpdatingJmnedict && !DictUtils.UpdatingKanjidic)
            {
                StackPanelCache.AddReplace(text, popupItemSource);
            }
        }
    }

    public StackPanel MakeResultStackPanel(LookupResult result, int index, int resultsCount, Dict? pitchDict, bool pitchDictIsActive, bool showPOrthographyInfo, bool showROrthographyInfo, bool showAOrthographyInfo)
    {
        // top
        WrapPanel top = new() { Tag = index };

        TextBlock primarySpellingTextBlock = new()
        {
            Name = nameof(result.PrimarySpelling),
            Text = result.PrimarySpelling,
            Foreground = ConfigManager.PrimarySpellingColor,
            Background = Brushes.Transparent,
            FontSize = ConfigManager.PrimarySpellingFontSize,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 0, 0),
            Padding = new Thickness(0),
            Cursor = Cursors.Arrow,
            ContextMenu = PopupContextMenu
        };
        primarySpellingTextBlock.PreviewMouseUp += PrimarySpelling_PreviewMouseUp; // for mining

        if (result.Readings is null && pitchDictIsActive)
        {
            Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                result.AlternativeSpellings,
                null,
                primarySpellingTextBlock.Text.Split(", "),
                primarySpellingTextBlock.Margin.Left,
                pitchDict!);

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
        else
        {
            _ = top.Children.Add(primarySpellingTextBlock);
        }

        if (showPOrthographyInfo && result.PrimarySpellingOrthographyInfoList is not null)
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

        if (result.Readings is not null
            && (pitchDictIsActive || (result.KunReadings is null && result.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && result.ReadingsOrthographyInfoList is not null
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
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (pitchDictIsActive)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBox.Text.Split(", "),
                        readingTextBox.Margin.Left,
                        pitchDict!);

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

                readingTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                readingTextBox.MouseMove += TextBox_MouseMove;
                readingTextBox.LostFocus += Unselect;
                readingTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                readingTextBox.MouseLeave += OnMouseLeave;
                readingTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (pitchDictIsActive)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBlock.Text.Split(", "),
                        readingTextBlock.Margin.Left,
                        pitchDict!);

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
            string alternativeSpellingsText = showAOrthographyInfo && result.AlternativeSpellingsOrthographyInfoList is not null
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

                alternativeSpellingsTexBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                alternativeSpellingsTexBox.MouseMove += TextBox_MouseMove;
                alternativeSpellingsTexBox.LostFocus += Unselect;
                alternativeSpellingsTexBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                alternativeSpellingsTexBox.MouseLeave += OnMouseLeave;
                alternativeSpellingsTexBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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
            string? freqText = PopupWindowUtils.FrequenciesToText(result.Frequencies, false);

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

                definitionsTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                definitionsTextBox.MouseMove += TextBox_MouseMove;
                definitionsTextBox.LostFocus += Unselect;
                definitionsTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                definitionsTextBox.MouseLeave += OnMouseLeave;
                definitionsTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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

                onReadingsTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                onReadingsTextBox.MouseMove += TextBox_MouseMove;
                onReadingsTextBox.LostFocus += Unselect;
                onReadingsTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                onReadingsTextBox.MouseLeave += OnMouseLeave;
                onReadingsTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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

                kunReadingsTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                kunReadingsTextBox.MouseMove += TextBox_MouseMove;
                kunReadingsTextBox.LostFocus += Unselect;
                kunReadingsTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                kunReadingsTextBox.MouseLeave += OnMouseLeave;
                kunReadingsTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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

                nanoriReadingsTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                nanoriReadingsTextBox.MouseMove += TextBox_MouseMove;
                nanoriReadingsTextBox.LostFocus += Unselect;
                nanoriReadingsTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                nanoriReadingsTextBox.MouseLeave += OnMouseLeave;
                nanoriReadingsTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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

        if (result.RadicalNames is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox radicalNameTextBox = new()
                {
                    Name = nameof(result.RadicalNames),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Radical names: {string.Join(", ", result.RadicalNames)}"),
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

                radicalNameTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                radicalNameTextBox.MouseMove += TextBox_MouseMove;
                radicalNameTextBox.LostFocus += Unselect;
                radicalNameTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                radicalNameTextBox.MouseLeave += OnMouseLeave;
                radicalNameTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                _ = bottom.Children.Add(radicalNameTextBox);
            }

            else
            {
                TextBlock radicalNameTextBlock = new()
                {
                    Name = nameof(result.NanoriReadings),
                    Text = string.Create(CultureInfo.InvariantCulture, $"Radical names: {string.Join(", ", result.RadicalNames)}"),
                    Foreground = ConfigManager.ReadingsColor,
                    FontSize = ConfigManager.ReadingsFontSize,
                    Margin = new Thickness(2, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _ = bottom.Children.Add(radicalNameTextBlock);
            }
        }

        if (result.KanjiGrade > -1)
        {
            TextBlock gradeTextBlock = new()
            {
                Name = nameof(result.KanjiGrade),
                Text = string.Create(CultureInfo.InvariantCulture, $"Grade: {PopupWindowUtils.GradeToText(result.KanjiGrade)}"),
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
                Text = string.Create(CultureInfo.InvariantCulture, $"Stroke count: {result.StrokeCount}"),
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

                compositionTextBox.PreviewMouseUp += TextBox_PreviewMouseUp;
                compositionTextBox.MouseMove += TextBox_MouseMove;
                compositionTextBox.LostFocus += Unselect;
                compositionTextBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
                compositionTextBox.MouseLeave += OnMouseLeave;
                compositionTextBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
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
                Height = 2,
                Background = ConfigManager.SeparatorColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        StackPanel stackPanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(4, 2, 4, 2),
            Background = Brushes.Transparent,
            Tag = result.Dict,
            Children = { top, bottom }
        };

        stackPanel.MouseEnter += ListBoxItem_MouseEnter;

        return stackPanel;
    }

    private static int GetIndexOfListBoxItemFromStackPanel(StackPanel stackPanel)
    {
        return (int)((WrapPanel)stackPanel.Children[0]).Tag;
    }

    private int GetFirstVisibleListBoxItemIndex()
    {
        StackPanel? firstVisibleStackPanel = PopupListView.Items.Cast<StackPanel>()
            .FirstOrDefault(static stackPanel => stackPanel.Visibility is Visibility.Visible);

        return firstVisibleStackPanel is not null
            ? GetIndexOfListBoxItemFromStackPanel(firstVisibleStackPanel)
            : 0;
    }

    private void ListBoxItem_MouseEnter(object sender, MouseEventArgs e)
    {
        _listBoxIndex = GetIndexOfListBoxItemFromStackPanel((StackPanel)sender);
        LastSelectedText = LastLookupResults[_listBoxIndex].PrimarySpelling;
    }

    private static void Unselect(object sender, RoutedEventArgs e)
    {
        WindowsUtils.Unselect((TextBox)sender);
    }

    private void TextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        AddNameMenuItem.IsEnabled = DictUtils.CustomNameDictReady && DictUtils.ProfileCustomNameDictReady;
        AddWordMenuItem.IsEnabled = DictUtils.CustomWordDictReady && DictUtils.ProfileCustomWordDictReady;
        _lastInteractedTextBox = (TextBox)sender;
        LastSelectedText = _lastInteractedTextBox.SelectedText;
    }

    private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastInteractedTextBox = (TextBox)sender;
    }

    private async void TextBox_MouseMove(object sender, MouseEventArgs? e)
    {
        if (ConfigManager.InactiveLookupMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnMouseClickOnly
            || e?.LeftButton is MouseButtonState.Pressed
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
            _lastInteractedTextBox = tb;
            if (JapaneseUtils.JapaneseRegex.IsMatch(tb.Text))
            {
                await ChildPopupWindow.LookupOnMouseMoveOrClick(tb).ConfigureAwait(false);
            }

            else if (ConfigManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(ChildPopupWindow._previousTextBox);
            }
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

        HidePopup();

        await PopupWindowUtils.Mine(LastLookupResults[_listBoxIndex], _currentText, _currentCharPosition).ConfigureAwait(false);
    }

    private void ShowAddNameWindow()
    {
        string text;
        string reading = "";
        if (_lastInteractedTextBox?.SelectionLength > 0)
        {
            text = _lastInteractedTextBox.SelectedText;
            if (text == ChildPopupWindow?.LastSelectedText)
            {
                string[]? readings = ChildPopupWindow.LastLookupResults[0].Readings;
                reading = readings?.Length is 1
                    ? readings[0]
                    : "";
            }
        }
        else
        {
            text = LastLookupResults[_listBoxIndex].PrimarySpelling;

            string[]? readings = LastLookupResults[_listBoxIndex].Readings;
            reading = readings?.Length is 1
                ? readings[0]
                : "";
        }

        if (reading == text)
        {
            reading = "";
        }

        WindowsUtils.ShowAddNameWindow(text, reading);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    private void SelectNextLookupResult()
    {
        int nextItemIndex = PopupListView.SelectedIndex + 1 < PopupListView.Items.Count
            ? PopupListView.SelectedIndex + 1
            : 0;

        PopupListView.SelectedIndex = nextItemIndex;

        PopupListView.ScrollIntoView(PopupListView.Items.GetItemAt(nextItemIndex));
    }

    private void SelectPreviousLookupResult()
    {
        int nextItemIndex = PopupListView.SelectedIndex - 1 > -1
            ? PopupListView.SelectedIndex - 1
            : PopupListView.Items.Count - 1;

        PopupListView.SelectedIndex = nextItemIndex;

        PopupListView.ScrollIntoView(PopupListView.Items.GetItemAt(nextItemIndex));
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
                TextBox_MouseMove(_previousTextBox!, null);
            }

            else
            {
                MainWindow.Instance.MainTextBox_MouseMove(null, null);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (DictUtils.CustomNameDictReady && DictUtils.ProfileCustomNameDictReady)
            {
                if (!MiningMode)
                {
                    if (Owner is PopupWindow previousPopupWindow)
                    {
                        previousPopupWindow.ShowAddNameWindow();
                    }

                    else
                    {
                        MainWindow.Instance.ShowAddNameWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddNameWindow();
                }

                PopupAutoHideTimer.Start();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (DictUtils.CustomWordDictReady && DictUtils.ProfileCustomWordDictReady)
            {
                if (!MiningMode)
                {
                    if (Owner is PopupWindow previousPopupWindow)
                    {
                        previousPopupWindow.ShowAddWordWindow();
                    }

                    else
                    {
                        MainWindow.Instance.ShowAddWordWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddWordWindow();
                }

                PopupAutoHideTimer.Start();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SearchWithBrowserKeyGesture))
        {
            if (!MiningMode)
            {
                if (Owner is PopupWindow previousPopupWindow)
                {
                    previousPopupWindow.SearchWithBrowser();
                }

                else
                {
                    MainWindow.Instance.SearchWithBrowser();
                }

                HidePopup();
            }

            else
            {
                SearchWithBrowser();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MotivationKeyGesture))
        {
            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.NextDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToNextDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = 0; i < dictCount; i++)
            {
                Button button = (Button)ItemsControlButtons.Items[i]!;

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
                    PopupListView.Items.Filter = DictFilter;
                    break;
                }
            }

            if (!movedToNextDict)
            {
                ((Button)ItemsControlButtons.Items[0]!).Background = Brushes.DodgerBlue;
                PopupListView.Items.Filter = NoAllDictFilter;
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.PreviousDictKeyGesture))
        {
            bool foundSelectedButton = false;
            bool movedToPreviousDict = false;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = dictCount - 1; i > 0; i--)
            {
                Button button = (Button)ItemsControlButtons.Items[i]!;

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
                    PopupListView.Items.Filter = DictFilter;
                    break;
                }
            }

            if (foundSelectedButton && !movedToPreviousDict)
            {
                ((Button)ItemsControlButtons.Items[0]!).Background = Brushes.DodgerBlue;
                PopupListView.Items.Filter = NoAllDictFilter;
            }

            else if (!foundSelectedButton)
            {
                for (int i = dictCount - 1; i > 0; i--)
                {
                    Button btn = (Button)ItemsControlButtons.Items[i]!;
                    if (btn.IsEnabled)
                    {
                        _filteredDict = (Dict)btn.Tag;
                        btn.Background = Brushes.DodgerBlue;
                        ((Button)ItemsControlButtons.Items[0]!).ClearValue(BackgroundProperty);
                        PopupListView.Items.Filter = DictFilter;
                        break;
                    }
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleMinimizedStateKeyGesture))
        {
            MainWindow mainWindow = MainWindow.Instance;
            PopupWindowUtils.HidePopups(mainWindow.FirstPopupWindow);

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
                && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string text = _lastInteractedTextBox?.SelectionLength > 0
                    ? _lastInteractedTextBox.SelectedText
                    : LastLookupResults[_listBoxIndex].PrimarySpelling;

                await SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, text, CoreConfig.AudioVolume).ConfigureAwait(false);
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
            if (MiningMode && PopupListView.SelectedItem is not null)
            {
                HidePopup();

                int index = GetIndexOfListBoxItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
                await PopupWindowUtils.Mine(LastLookupResults[index], _currentText, _currentCharPosition).ConfigureAwait(false);
            }
        }
    }

    public void EnableMiningMode()
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
        if (LastLookupResults.Count is 0)
        {
            return;
        }

        LookupResult lastLookupResult = LastLookupResults[_listBoxIndex];
        string primarySpelling = lastLookupResult.PrimarySpelling;
        string? reading = lastLookupResult.Readings?[0];

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

    private async void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _lastInteractedTextBox = (TextBox)sender;
        LastSelectedText = _lastInteractedTextBox.SelectedText;

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
                    || AddNameWindow.IsItVisible())
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

        PopupListView.Items.Filter = NoAllDictFilter;
        _firstVisibleListBoxIndex = GetFirstVisibleListBoxItemIndex();
        _listBoxIndex = _firstVisibleListBoxIndex;
        LastSelectedText = LastLookupResults[_listBoxIndex].PrimarySpelling;

        WindowsUtils.Unselect(_lastInteractedTextBox);
        _lastInteractedTextBox = null;
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

        PopupListView.Items.Filter = DictFilter;
        _firstVisibleListBoxIndex = GetFirstVisibleListBoxItemIndex();
        _listBoxIndex = _firstVisibleListBoxIndex;
        LastSelectedText = LastLookupResults[_listBoxIndex].PrimarySpelling;

        WindowsUtils.Unselect(_lastInteractedTextBox);
        _lastInteractedTextBox = null;
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
            && !AddNameWindow.IsItVisible())
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
                PopupWindowUtils.HidePopups(ChildPopupWindow);
            }
        }

        else if (e.ChangedButton == ConfigManager.MiningModeMouseButton)
        {
            if (!MiningMode)
            {
                PopupWindowUtils.ShowMiningModeResults(this);
            }

            else if (ChildPopupWindow is { IsVisible: true, MiningMode: false })
            {
                PopupWindowUtils.ShowMiningModeResults(ChildPopupWindow);
            }
        }
    }

    public void HidePopup()
    {
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopup = Owner == mainWindow;

        if (isFirstPopup
            && (ConfigManager.TextOnlyVisibleOnHover || ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible())
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

        if (_popupListViewScrollViewer is not null)
        {
            _popupListViewScrollViewer.ScrollToTop();
            PopupListView.UpdateLayout();
        }

        PopupListView.ItemsSource = null;
        LastText = "";
        _listBoxIndex = 0;
        _firstVisibleListBoxIndex = 0;
        _lastInteractedTextBox = null;

        PopupAutoHideTimer.Stop();

        UpdateLayout();
        Hide();

        if (AddNameWindow.IsItVisible() || AddWordWindow.IsItVisible())
        {
            return;
        }

        if (isFirstPopup)
        {
            WinApi.ActivateWindow(mainWindow.WindowHandle);

            if (ConfigManager.HighlightLongestMatch && !mainWindow.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_previousTextBox);
            }
        }

        else
        {
            PopupWindow previousPopup = (PopupWindow)Owner;
            if (previousPopup.IsVisible)
            {
                WinApi.ActivateWindow(previousPopup.WindowHandle);
            }

            if (ConfigManager.HighlightLongestMatch && !previousPopup.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_previousTextBox);
            }
        }
    }

    private void Window_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ContextMenuIsOpening = true;
        PopupWindowUtils.HidePopups(ChildPopupWindow);
        ContextMenuIsOpening = false;
    }

    private void PopupListView_MouseLeave(object sender, MouseEventArgs e)
    {
        _listBoxIndex = _firstVisibleListBoxIndex;
        LastSelectedText = LastLookupResults[_listBoxIndex].PrimarySpelling;
    }
}
