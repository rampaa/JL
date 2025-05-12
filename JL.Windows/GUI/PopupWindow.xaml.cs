using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Mining;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal sealed partial class PopupWindow
{
    private bool _contextMenuIsOpening; // = false;

    private TextBox? _previousTextBox;

    private TextBox? _lastInteractedTextBox;

    private int _listViewItemIndex; // 0

    private int _listViewItemIndexAfterContextMenuIsClosed; // 0

    private int _firstVisibleListViewItemIndex; // 0

    private int _currentSourceTextCharPosition;

    private string _currentSourceText = "";

    public Button AllDictionaryTabButton { get; } = new()
    {
        Name = nameof(AllDictionaryTabButton),
        Content = "All",
        Margin = new Thickness(),
        Background = Brushes.DodgerBlue,
        Cursor = Cursors.Arrow,
        VerticalAlignment = VerticalAlignment.Top,
        VerticalContentAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Left,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        FontSize = ConfigManager.Instance.PopupDictionaryTabFontSize,
        Padding = new Thickness(5, 3, 5, 3),
        Height = double.NaN,
        Width = double.NaN
    };

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    public LookupResult[] LastLookupResults { get; private set; } = [];

    private readonly List<Dict> _dictsWithResults = [];

    private Dict? _filteredDict;

    public bool UnavoidableMouseEnter { get; private set; } // = false;

    private string? _lastLookedUpText;

    public bool MiningMode { get; private set; }

    private ScrollViewer? _popupListViewScrollViewer;

    private readonly ContextMenu _editableTextBoxContextMenu = new();
    public int PopupIndex { get; }

    public PopupWindow(int popupIndex)
    {
        InitializeComponent();
        PopupIndex = popupIndex;
        PopupWindowUtils.PopupWindows[popupIndex] = this;
        Init();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHandle = new WindowInteropHelper(this).Handle;
        _popupListViewScrollViewer = PopupListView.GetChildOfType<ScrollViewer>();
        Debug.Assert(_popupListViewScrollViewer is not null);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Instance.Focusable)
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
        ConfigManager configManager = ConfigManager.Instance;
        Background = configManager.PopupBackgroundColor;
        Foreground = configManager.DefinitionsColor;
        FontFamily = configManager.PopupFont;

        SetSizeToContent(configManager.PopupDynamicWidth, configManager.PopupDynamicHeight, configManager.PopupMaxWidth, configManager.PopupMaxHeight, configManager.PopupMinWidth, configManager.PopupMinHeight);

        AddNameMenuItem.SetInputGestureText(configManager.ShowAddNameWindowKeyGesture);
        AddWordMenuItem.SetInputGestureText(configManager.ShowAddWordWindowKeyGesture);
        SearchMenuItem.SetInputGestureText(configManager.SearchWithBrowserKeyGesture);

        AllDictionaryTabButton.Click += DictTypeButtonOnClick;

        AddMenuItemsToEditableTextBoxContextMenu();
    }

    private void AddMenuItemsToEditableTextBoxContextMenu()
    {
        // ReSharper disable BadExpressionBracesLineBreaks
        MenuItem addNameMenuItem = new() { Name = "AddNameMenuItem", Header = "Add name", Padding = new Thickness() };
        addNameMenuItem.Click += AddName;
        _ = _editableTextBoxContextMenu.Items.Add(addNameMenuItem);

        MenuItem addWordMenuItem = new() { Name = "AddWordMenuItem", Header = "Add word", Padding = new Thickness() };
        addWordMenuItem.Click += AddWord;
        _ = _editableTextBoxContextMenu.Items.Add(addWordMenuItem);

        MenuItem copyMenuItem = new() { Header = "Copy", Command = ApplicationCommands.Copy, Padding = new Thickness() };
        _ = _editableTextBoxContextMenu.Items.Add(copyMenuItem);

        MenuItem cutMenuItem = new() { Header = "Cut", Command = ApplicationCommands.Cut, Padding = new Thickness() };
        _ = _editableTextBoxContextMenu.Items.Add(cutMenuItem);

        MenuItem deleteMenuItem = new() { Name = "DeleteMenuItem", Header = "Delete", InputGestureText = "Backspace", Padding = new Thickness() };
        deleteMenuItem.Click += PressBackSpace;
        _ = _editableTextBoxContextMenu.Items.Add(deleteMenuItem);

        MenuItem searchMenuItem = new() { Name = "SearchMenuItem", Header = "Search", Padding = new Thickness() };
        searchMenuItem.Click += SearchWithBrowser;
        _ = _editableTextBoxContextMenu.Items.Add(searchMenuItem);
        // ReSharper restore BadExpressionBracesLineBreaks
    }

    private void PressBackSpace(object sender, RoutedEventArgs e)
    {
        Debug.Assert(_lastInteractedTextBox is not null);

        PresentationSource? lastInteractedTextBoxSource = PresentationSource.FromVisual(_lastInteractedTextBox);
        Debug.Assert(lastInteractedTextBoxSource is not null);

        _lastInteractedTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, lastInteractedTextBoxSource, 0, Key.Back)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        ShowAddNameWindow(false);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        ShowAddWordWindow(false);
    }

    private void ShowAddWordWindow(bool useSelectedListViewItemIfItExists)
    {
        string text;
        if (PopupListView.SelectedItem is not null && useSelectedListViewItemIfItExists)
        {
            int index = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
            text = LastLookupResults[index].PrimarySpelling;
        }
        else
        {
            TextBox? lastInteractedTextBox = _lastInteractedTextBox;
            text = lastInteractedTextBox is not null && lastInteractedTextBox.SelectionLength > 0
                ? lastInteractedTextBox.SelectedText
                : LastLookupResults[_listViewItemIndex].PrimarySpelling;
        }

        WindowsUtils.ShowAddWordWindow(this, text);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        SearchWithBrowser(false);
    }

    private void SearchWithBrowser(bool useSelectedListViewItemIfItExists)
    {
        string text;
        if (useSelectedListViewItemIfItExists)
        {
            int listViewItemIndex = PopupListView.SelectedItem is not null
                ? PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem)
                : _listViewItemIndex;

            text = LastLookupResults[listViewItemIndex].PrimarySpelling;
        }
        else
        {
            TextBox? lastInteractedTextBox = _lastInteractedTextBox;
            text = lastInteractedTextBox is not null && lastInteractedTextBox.SelectionLength > 0
                ? lastInteractedTextBox.SelectedText
                : LastLookupResults[_listViewItemIndex].PrimarySpelling;
        }

        WindowsUtils.SearchWithBrowser(text);
    }

    public Task LookupOnCharPosition(TextBox textBox, int charPosition, bool enableMiningMode)
    {
        string textBoxText = textBox.Text;

        _currentSourceText = textBoxText;

        if (char.IsLowSurrogate(textBox.Text[charPosition]))
        {
            --charPosition;
        }

        _currentSourceTextCharPosition = charPosition;

        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopupWindow = PopupIndex is 0;
        if (isFirstPopupWindow
                ? configManager.DisableLookupsForNonJapaneseCharsInMainWindow
                  && !JapaneseUtils.ContainsJapaneseCharacters(textBoxText[charPosition])
                : configManager.DisableLookupsForNonJapaneseCharsInPopups
                  && !JapaneseUtils.ContainsJapaneseCharacters(textBoxText[charPosition]))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        string textToLookUp = textBoxText;
        if (textBoxText.Length - charPosition > configManager.MaxSearchLength)
        {
            int newLength = charPosition + configManager.MaxSearchLength;
            if (char.IsLowSurrogate(textBoxText[newLength - 1]))
            {
                --newLength;
            }

            textToLookUp = textBoxText[..newLength];
        }

        int endPosition = JapaneseUtils.FindExpressionBoundary(textToLookUp, charPosition);
        textToLookUp = textToLookUp[charPosition..endPosition];

        if (string.IsNullOrEmpty(textToLookUp) || TextUtils.StartsWithWhiteSpace(textToLookUp))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        if (textToLookUp == _lastLookedUpText && IsVisible)
        {
            UpdatePosition();
            return Task.CompletedTask;
        }

        _lastLookedUpText = textToLookUp;

        LookupResult[]? lookupResults = LookupUtils.LookupText(textToLookUp);

        if (lookupResults is not null && lookupResults.Length > 0)
        {
            _previousTextBox = textBox;
            LookupResult firstLookupResult = lookupResults[0];
            LastSelectedText = firstLookupResult.MatchedText;

            StatsUtils.IncrementStat(StatType.NumberOfLookups);
            if (CoreConfigManager.Instance.TrackTermLookupCounts)
            {
                StatsUtils.IncrementTermLookupCount(firstLookupResult.DeconjugatedMatchedText ?? firstLookupResult.MatchedText);
            }

            if (configManager.HighlightLongestMatch)
            {
                Debug.Assert(isFirstPopupWindow || PopupWindowUtils.PopupWindows[PopupIndex - 1] is not null);
                WinApi.ActivateWindow(isFirstPopupWindow
                    ? mainWindow.WindowHandle
                    : PopupWindowUtils.PopupWindows[PopupIndex - 1]!.WindowHandle);

                _ = textBox.Focus();
                textBox.Select(charPosition, lookupResults[0].MatchedText.Length);
            }

            LastLookupResults = lookupResults;

            if (enableMiningMode)
            {
                EnableMiningMode();
                DisplayResults();

                if (configManager.AutoHidePopupIfMouseIsNotOverIt)
                {
                    PopupWindowUtils.SetPopupAutoHideTimer();
                }
            }

            else
            {
                DisplayResults();
            }

            Show();

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            UpdatePosition();

            if (configManager.Focusable
                && (enableMiningMode || configManager.PopupFocusOnLookup))
            {
                if (configManager.RestoreFocusToPreviouslyActiveWindow && isFirstPopupWindow)
                {
                    nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                    if (previousWindowHandle != mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
                    {
                        WindowsUtils.LastActiveWindowHandle = previousWindowHandle;
                    }
                }

                if (!Activate())
                {
                    WinApi.StealFocus(WindowHandle);
                }
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (configManager.AutoPlayAudio)
            {
                return PlayAudio(false);
            }
        }
        else
        {
            HidePopup();
        }

        return Task.CompletedTask;
    }

    public Task LookupOnMouseMoveOrClick(TextBox textBox, bool enableMiningMode)
    {
        int charPosition = textBox.GetCharacterIndexFromPoint(Mouse.GetPosition(textBox), false);
        if (charPosition < 0)
        {
            HidePopup();
            return Task.CompletedTask;
        }

        return LookupOnCharPosition(textBox, charPosition, enableMiningMode);
    }

    public Task LookupOnSelect(TextBox textBox)
    {
        _currentSourceText = textBox.Text;
        _currentSourceTextCharPosition = textBox.SelectionStart;

        string selectedText = textBox.SelectedText;
        if (string.IsNullOrEmpty(selectedText) || TextUtils.StartsWithWhiteSpace(selectedText))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (selectedText == _lastLookedUpText && IsVisible)
        {
            UpdatePosition();
            return Task.CompletedTask;
        }

        _lastLookedUpText = selectedText;

        LookupResult[]? lookupResults = LookupUtils.LookupText(textBox.SelectedText);

        if (lookupResults is not null && lookupResults.Length > 0)
        {
            _previousTextBox = textBox;
            LookupResult firstLookupResult = lookupResults[0];
            LastSelectedText = firstLookupResult.MatchedText;
            LastLookupResults = lookupResults;

            StatsUtils.IncrementStat(StatType.NumberOfLookups);
            if (CoreConfigManager.Instance.TrackTermLookupCounts)
            {
                StatsUtils.IncrementTermLookupCount(firstLookupResult.DeconjugatedMatchedText ?? firstLookupResult.MatchedText);
            }

            EnableMiningMode();
            DisplayResults();

            if (configManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }

            Show();

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            UpdatePosition();

            _ = textBox.Focus();

            if (configManager.Focusable)
            {
                MainWindow mainWindow = MainWindow.Instance;
                if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
                {
                    nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                    if (previousWindowHandle != mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
                    {
                        WindowsUtils.LastActiveWindowHandle = previousWindowHandle;
                    }
                }

                if (!Activate())
                {
                    WinApi.StealFocus(WindowHandle);
                }
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (configManager.AutoPlayAudio)
            {
                return PlayAudio(false);
            }
        }
        else
        {
            HidePopup();
        }

        return Task.CompletedTask;
    }

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X;
        double mouseY = cursorPosition.Y;

        DpiScale dpi = WindowsUtils.Dpi;
        double currentWidth = ActualWidth * dpi.DpiScaleX;
        double currentHeight = ActualHeight * dpi.DpiScaleY;

        ConfigManager configManager = ConfigManager.Instance;

        double newLeft = configManager.PositionPopupLeftOfCursor
            ? mouseX - (currentWidth + WindowsUtils.DpiAwareXOffset)
            : mouseX + WindowsUtils.DpiAwareXOffset;

        double newTop = configManager.PositionPopupAboveCursor
            ? mouseY - (currentHeight + WindowsUtils.DpiAwareYOffset)
            : mouseY + WindowsUtils.DpiAwareYOffset;

        Rectangle screenBounds = WindowsUtils.ActiveScreen.Bounds;

        if (configManager.PopupFlipX)
        {
            if (configManager.PositionPopupLeftOfCursor && newLeft < screenBounds.Left)
            {
                newLeft = mouseX + WindowsUtils.DpiAwareXOffset;
            }
            else if (!configManager.PositionPopupLeftOfCursor && newLeft + currentWidth > screenBounds.Right)
            {
                newLeft = mouseX - (currentWidth + WindowsUtils.DpiAwareXOffset);
            }
        }

        newLeft = Math.Max(screenBounds.Left, Math.Min(newLeft, screenBounds.Right - currentWidth));

        if (configManager.PopupFlipY)
        {
            if (configManager.PositionPopupAboveCursor && newTop < screenBounds.Top)
            {
                newTop = mouseY + WindowsUtils.DpiAwareYOffset;
            }
            else if (!configManager.PositionPopupAboveCursor && newTop + currentHeight > screenBounds.Bottom)
            {
                newTop = mouseY - (currentHeight + WindowsUtils.DpiAwareYOffset);
            }
        }

        newTop = Math.Max(screenBounds.Top, Math.Min(newTop, screenBounds.Bottom - currentHeight));

        UnavoidableMouseEnter = mouseX >= newLeft
            && mouseX <= newLeft + currentWidth
            && mouseY >= newTop
            && mouseY <= newTop + currentHeight;

        WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);
    }

    private void UpdatePositionToFixedPosition()
    {
        Screen activeScreen = WindowsUtils.ActiveScreen;
        ConfigManager configManager = ConfigManager.Instance;

        double x = configManager.FixedPopupXPosition;
        if (configManager.FixedPopupRightPositioning)
        {
            double currentWidth = ActualWidth * WindowsUtils.Dpi.DpiScaleX;
            if (x is 0)
            {
                x = (activeScreen.Bounds.Left + activeScreen.Bounds.Right + currentWidth) / 2;
            }
            else if (x is -1)
            {
                x = activeScreen.WorkingArea.Right;
            }
            else if (x is -2)
            {
                x = activeScreen.Bounds.Right;
            }

            x = Math.Max(x is -1 ? activeScreen.WorkingArea.Left : activeScreen.Bounds.Left, x - currentWidth);
        }

        double y = configManager.FixedPopupYPosition;
        if (configManager.FixedPopupBottomPositioning)
        {
            double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;
            if (y is -2)
            {
                y = activeScreen.Bounds.Bottom;
            }
            else if (y is -1)
            {
                y = activeScreen.WorkingArea.Bottom;
            }
            else if (y is 0)
            {
                y = (activeScreen.Bounds.Top + activeScreen.Bounds.Bottom + currentHeight) / 2;
            }

            y = Math.Max(y is -1 ? activeScreen.WorkingArea.Top : activeScreen.Bounds.Top, y - currentHeight);
        }

        WinApi.MoveWindowToPosition(WindowHandle, x, y);
    }

    private void UpdatePosition()
    {
        if (ConfigManager.Instance.FixedPopupPositioning && PopupIndex is 0)
        {
            UpdatePositionToFixedPosition();
        }

        else
        {
            UpdatePosition(WinApi.GetMousePosition());
        }
    }

    private void DisplayResults()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        _dictsWithResults.Clear();

        PopupListView.Items.Filter = PopupWindowUtils.NoAllDictFilter;

        Dict jmdict = DictUtils.SingleDictTypeDicts[DictType.JMdict];

        Debug.Assert(jmdict.Options.POrthographyInfo is not null);
        bool showPOrthographyInfo = jmdict.Options.POrthographyInfo.Value;

        Debug.Assert(jmdict.Options.ROrthographyInfo is not null);
        bool showROrthographyInfo = jmdict.Options.ROrthographyInfo.Value;

        Debug.Assert(jmdict.Options.AOrthographyInfo is not null);
        bool showAOrthographyInfo = jmdict.Options.AOrthographyInfo.Value;

        Debug.Assert(jmdict.Options.POrthographyInfoFontSize is not null);
        double pOrthographyInfoFontSize = jmdict.Options.POrthographyInfoFontSize.Value;

        Button[]? duplicateIcons;
        int resultCount;
        bool checkForDuplicateCards;
        if (MiningMode)
        {
            resultCount = LastLookupResults.Length;
            checkForDuplicateCards = coreConfigManager is { CheckForDuplicateCards: true, AnkiIntegration: true } && !configManager.MineToFileInsteadOfAnki;
            duplicateIcons = checkForDuplicateCards
                ? new Button[LastLookupResults.Length]
                : null;
        }
        else
        {
            resultCount = Math.Min(LastLookupResults.Length, ConfigManager.Instance.MaxNumResultsNotInMiningMode);
            checkForDuplicateCards = false;
            duplicateIcons = null;
        }

        StackPanel[] popupItemSource = new StackPanel[resultCount];

        for (int i = 0; i < resultCount; i++)
        {
            LookupResult lookupResult = LastLookupResults[i];

            if (!_dictsWithResults.Contains(lookupResult.Dict))
            {
                _dictsWithResults.Add(lookupResult.Dict);
            }

            popupItemSource[i] = PrepareResultStackPanel(lookupResult, i, resultCount, showPOrthographyInfo, showROrthographyInfo, showAOrthographyInfo, pOrthographyInfoFontSize, duplicateIcons);
        }

        PopupListView.ItemsSource = popupItemSource;

        if (checkForDuplicateCards)
        {
            Debug.Assert(duplicateIcons is not null);
            _ = CheckResultForDuplicates(duplicateIcons);
        }

        GenerateDictTypeButtons();

        UpdateLayout();
    }

    private void AddEventHandlersToTextBox(TextBox textBox)
    {
        textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
    }

    private void AddEventHandlersToDefinitionsTextBox(TextBox textBox)
    {
        textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += DefinitionsTextBox_PreviewMouseLeftButtonDown;
    }

    private void AddEventHandlersToPrimarySpellingTextBox(FrameworkElement textBox)
    {
        textBox.PreviewMouseUp += PrimarySpellingTextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
    }

    private StackPanel PrepareResultStackPanel(LookupResult result, int index, int resultCount, bool showPOrthographyInfo, bool showROrthographyInfo, bool showAOrthographyInfo, double pOrthographyInfoFontSize, Button[]? miningButtons)
    {
        // top
        WrapPanel top = new()
        {
            Tag = index
        };

        ConfigManager configManager = ConfigManager.Instance;

        FrameworkElement primarySpellingFrameworkElement;
        if (MiningMode)
        {
            primarySpellingFrameworkElement = PopupWindowUtils.CreateTextBox(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            configManager.PrimarySpellingColor,
            configManager.PrimarySpellingFontSize,
            VerticalAlignment.Center,
            new Thickness(),
            PopupContextMenu);

            AddEventHandlersToPrimarySpellingTextBox(primarySpellingFrameworkElement);
        }
        else
        {
            primarySpellingFrameworkElement = PopupWindowUtils.CreateTextBlock(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            configManager.PrimarySpellingColor,
            configManager.PrimarySpellingFontSize,
            VerticalAlignment.Center,
            new Thickness(2, 0, 0, 0));
        }

        bool pitchPositionsExist = result.PitchPositions is not null;

        if (result.Readings is null && pitchPositionsExist)
        {
            Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                null,
                null,
                primarySpellingFrameworkElement.Margin.Left,
                result.PitchPositions);

            if (pitchAccentGrid.Children.Count is 0)
            {
                _ = top.Children.Add(primarySpellingFrameworkElement);
            }

            else
            {
                _ = pitchAccentGrid.Children.Add(primarySpellingFrameworkElement);
                _ = top.Children.Add(pitchAccentGrid);
            }
        }
        else
        {
            _ = top.Children.Add(primarySpellingFrameworkElement);
        }

        JmdictLookupResult? jmdictLookupResult = result.JmdictLookupResult;
        bool jmdictLookupResultExist = jmdictLookupResult is not null;
        if (showPOrthographyInfo && jmdictLookupResultExist)
        {
            Debug.Assert(jmdictLookupResult is not null);
            if (jmdictLookupResult.PrimarySpellingOrthographyInfoList is not null)
            {
                TextBlock textBlockPOrthographyInfo = PopupWindowUtils.CreateTextBlock(nameof(jmdictLookupResult.PrimarySpellingOrthographyInfoList),
                    $"[{string.Join(", ", jmdictLookupResult.PrimarySpellingOrthographyInfoList)}]",
                    DictOptionManager.POrthographyInfoColor,
                    pOrthographyInfoFontSize,
                    VerticalAlignment.Center,
                    new Thickness(3, 0, 0, 0));

                _ = top.Children.Add(textBlockPOrthographyInfo);
            }
        }

        if (result.Readings is not null && configManager.ReadingsFontSize > 0
                                        && (pitchPositionsExist || result.KanjiLookupResult is null || (result.KanjiLookupResult.KunReadings is null && result.KanjiLookupResult.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && jmdictLookupResultExist && jmdictLookupResult!.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(result.Readings, jmdictLookupResult.ReadingsOrthographyInfoList)
                : string.Join('、', result.Readings);

            if (MiningMode)
            {
                TextBox readingTextBox = PopupWindowUtils.CreateTextBox(nameof(result.Readings),
                    readingsText, configManager.ReadingsColor,
                    configManager.ReadingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0),
                    PopupContextMenu);

                if (pitchPositionsExist)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.Readings,
                        readingTextBox.Text.Split('、'),
                        readingTextBox.Margin.Left,
                        result.PitchPositions);

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

                AddEventHandlersToTextBox(readingTextBox);
            }

            else
            {
                TextBlock readingTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Readings),
                    readingsText,
                    configManager.ReadingsColor,
                    configManager.ReadingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                if (pitchPositionsExist)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.Readings,
                        readingTextBlock.Text.Split('、'),
                        readingTextBlock.Margin.Left,
                        result.PitchPositions);

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

        if (MiningMode && configManager.AudioButtonFontSize > 0)
        {
            Button audioButton = new()
            {
                Name = "AudioButton",
                Content = "🔊",
                Foreground = configManager.AudioButtonColor,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(),
                Padding = new Thickness(),
                FontSize = configManager.AudioButtonFontSize,
                Focusable = false,
                Height = double.NaN,
                Width = double.NaN
            };

            audioButton.PreviewMouseUp += AudioButton_Click;

            _ = top.Children.Add(audioButton);
        }

        if (result.AlternativeSpellings is not null && configManager.AlternativeSpellingsFontSize > 0)
        {
            string alternativeSpellingsText = showAOrthographyInfo && jmdictLookupResultExist && jmdictLookupResult!.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToTextWithParentheses(result.AlternativeSpellings, jmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
                : $"[{string.Join('、', result.AlternativeSpellings)}]";

            if (MiningMode)
            {
                TextBox alternativeSpellingsTexBox = PopupWindowUtils.CreateTextBox(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    configManager.AlternativeSpellingsColor,
                    configManager.AlternativeSpellingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0),
                    PopupContextMenu);

                AddEventHandlersToTextBox(alternativeSpellingsTexBox);

                _ = top.Children.Add(alternativeSpellingsTexBox);
            }
            else
            {
                TextBlock alternativeSpellingsTexBlock = PopupWindowUtils.CreateTextBlock(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    configManager.AlternativeSpellingsColor,
                    configManager.AlternativeSpellingsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(alternativeSpellingsTexBlock);
            }
        }

        if (configManager.DeconjugationInfoFontSize > 0)
        {
            if (result.DeconjugationProcess is not null)
            {
                if (MiningMode)
                {
                    TextBox deconjugationProcessTextBox = PopupWindowUtils.CreateTextBox(nameof(result.DeconjugationProcess),
                        $"{result.MatchedText} {result.DeconjugationProcess}",
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(5, 0, 0, 0),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(deconjugationProcessTextBox);

                    _ = top.Children.Add(deconjugationProcessTextBox);
                }
                else
                {
                    TextBlock deconjugationProcessTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.DeconjugationProcess),
                        $"{result.MatchedText} {result.DeconjugationProcess}",
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(7, 0, 0, 0));

                    _ = top.Children.Add(deconjugationProcessTextBlock);
                }
            }
            else if (result.PrimarySpelling != result.MatchedText && (result.Readings is null || !result.Readings.AsReadOnlySpan().Contains(result.MatchedText)))
            {
                if (MiningMode)
                {
                    TextBox matchedTextTextBox = PopupWindowUtils.CreateTextBox(nameof(result.MatchedText),
                        result.MatchedText,
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(5, 0, 0, 0),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(matchedTextTextBox);

                    _ = top.Children.Add(matchedTextTextBox);
                }
                else
                {
                    TextBlock matchedTextTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.MatchedText),
                        result.MatchedText,
                        configManager.DeconjugationInfoColor,
                        configManager.DeconjugationInfoFontSize,
                        VerticalAlignment.Top,
                        new Thickness(7, 0, 0, 0));

                    _ = top.Children.Add(matchedTextTextBlock);
                }
            }
        }


        if (result.Frequencies is not null)
        {
            ReadOnlySpan<LookupFrequencyResult> allFrequencies = result.Frequencies.AsReadOnlySpan();
            List<LookupFrequencyResult> filteredFrequencies = new(allFrequencies.Length);
            foreach (ref readonly LookupFrequencyResult frequency in allFrequencies)
            {
                if (frequency.Freq is > 0 and < int.MaxValue)
                {
                    filteredFrequencies.Add(frequency);
                }
            }

            ReadOnlySpan<LookupFrequencyResult> validFrequencies = filteredFrequencies.AsReadOnlySpan();

            if (validFrequencies.Length > 0)
            {
                TextBlock frequencyTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Frequencies),
                    LookupResultUtils.FrequenciesToText(validFrequencies, false, result.Frequencies.Count is 1),
                    configManager.FrequencyColor,
                    configManager.FrequencyFontSize,
                    VerticalAlignment.Top,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(frequencyTextBlock);
            }
        }

        if (configManager.DictTypeFontSize > 0)
        {
            TextBlock dictTypeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Dict.Name),
                result.Dict.Name,
                configManager.DictTypeColor,
                configManager.DictTypeFontSize,
                VerticalAlignment.Top,
                new Thickness(7, 0, 0, 0));

            _ = top.Children.Add(dictTypeTextBlock);
        }

        if (MiningMode && configManager.MiningButtonFontSize > 0)
        {
            Button miningButton = new()
            {
                Name = "MiningButton",
                Content = '➕',
                ToolTip = "Mine",
                Foreground = configManager.MiningButtonColor,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(),
                Padding = new Thickness(),
                FontSize = configManager.MiningButtonFontSize,
                Focusable = false,
                Height = double.NaN,
                Width = double.NaN
            };

            miningButton.PreviewMouseUp += MiningButton_PreviewMouseUp;
            _ = top.Children.Add(miningButton);

            if (miningButtons is not null)
            {
                miningButtons[index] = miningButton;
            }
        }

        // bottom
        StackPanel bottom = new();

        if (result.FormattedDefinitions is not null)
        {
            if (MiningMode)
            {
                TextBox definitionsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2),
                    PopupContextMenu);

                AddEventHandlersToDefinitionsTextBox(definitionsTextBox);
                _ = bottom.Children.Add(definitionsTextBox);
            }

            else
            {
                TextBlock definitionsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(definitionsTextBlock);
            }
        }

        KanjiLookupResult? kanjiLookupResult = result.KanjiLookupResult;
        if (kanjiLookupResult is not null)
        {
            if (kanjiLookupResult.OnReadings is not null)
            {
                if (MiningMode)
                {
                    TextBox onReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.OnReadings),
                        $"On: {string.Join('、', kanjiLookupResult.OnReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(onReadingsTextBox);

                    _ = bottom.Children.Add(onReadingsTextBox);
                }

                else
                {
                    TextBlock onReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.OnReadings),
                        $"On: {string.Join('、', kanjiLookupResult.OnReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(onReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.KunReadings is not null)
            {
                if (MiningMode)
                {
                    TextBox kunReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KunReadings),
                        $"Kun: {string.Join('、', kanjiLookupResult.KunReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(kunReadingsTextBox);

                    _ = bottom.Children.Add(kunReadingsTextBox);
                }

                else
                {
                    TextBlock kunReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KunReadings),
                        $"Kun: {string.Join('、', kanjiLookupResult.KunReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(kunReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.NanoriReadings is not null)
            {
                if (MiningMode)
                {
                    TextBox nanoriReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.NanoriReadings),
                        $"Nanori: {string.Join('、', kanjiLookupResult.NanoriReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(nanoriReadingsTextBox);

                    _ = bottom.Children.Add(nanoriReadingsTextBox);
                }

                else
                {
                    TextBlock nanoriReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.NanoriReadings),
                        $"Nanori: {string.Join('、', kanjiLookupResult.NanoriReadings)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(nanoriReadingsTextBlock);
                }
            }

            if (kanjiLookupResult.RadicalNames is not null)
            {
                if (MiningMode)
                {
                    TextBox radicalNameTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.RadicalNames),
                        $"Radical names: {string.Join('、', kanjiLookupResult.RadicalNames)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(radicalNameTextBox);

                    _ = bottom.Children.Add(radicalNameTextBox);
                }

                else
                {
                    TextBlock radicalNameTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.RadicalNames),
                        $"Radical names: {string.Join('、', kanjiLookupResult.RadicalNames)}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(radicalNameTextBlock);
                }
            }

            if (kanjiLookupResult.KanjiGrade is not byte.MaxValue)
            {
                TextBlock gradeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiGrade),
                    $"Grade: {LookupResultUtils.GradeToText(kanjiLookupResult.KanjiGrade)}",
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(gradeTextBlock);
            }

            if (kanjiLookupResult.StrokeCount > 0)
            {
                TextBlock strokeCountTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.StrokeCount),
                    string.Create(CultureInfo.InvariantCulture, $"Stroke count: {kanjiLookupResult.StrokeCount}"),
                    configManager.DefinitionsColor,
                    configManager.DefinitionsFontSize,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(strokeCountTextBlock);
            }

            if (kanjiLookupResult.KanjiComposition is not null)
            {
                string composition = $"Composition: {string.Join('、', kanjiLookupResult.KanjiComposition)}";
                if (MiningMode)
                {
                    TextBox compositionTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KanjiComposition),
                        composition,
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(compositionTextBox);

                    _ = bottom.Children.Add(compositionTextBox);
                }

                else
                {
                    TextBlock compositionTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiComposition),
                        composition,
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(compositionTextBlock);
                }
            }

            if (kanjiLookupResult.KanjiStats is not null)
            {
                if (MiningMode)
                {
                    TextBox kanjiStatsTextBlock = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KanjiStats),
                        $"Statistics:\n{kanjiLookupResult.KanjiStats}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(0, 2, 2, 2),
                        PopupContextMenu);

                    AddEventHandlersToTextBox(kanjiStatsTextBlock);

                    _ = bottom.Children.Add(kanjiStatsTextBlock);
                }

                else
                {
                    TextBlock kanjiStatsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(kanjiLookupResult.KanjiStats),
                        $"Statistics:\n{kanjiLookupResult.KanjiStats}",
                        configManager.DefinitionsColor,
                        configManager.DefinitionsFontSize,
                        VerticalAlignment.Center,
                        new Thickness(2));

                    _ = bottom.Children.Add(kanjiStatsTextBlock);
                }
            }
        }

        if (index != resultCount - 1)
        {
            _ = bottom.Children.Add(new Separator
            {
                Height = 2,
                Background = configManager.SeparatorColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        StackPanel stackPanel = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(2),
            Background = Brushes.Transparent,
            Tag = result.Dict,
            Children =
            {
                top, bottom
            }
        };

        stackPanel.MouseEnter += ListViewItem_MouseEnter;

        return stackPanel;
    }

    private async Task CheckResultForDuplicates(Button[] miningButtons)
    {
        bool[]? duplicateCard = await MiningUtils.CheckDuplicates(LastLookupResults, _currentSourceText, _currentSourceTextCharPosition).ConfigureAwait(true);
        if (duplicateCard is not null)
        {
            for (int i = 0; i < duplicateCard.Length; i++)
            {
                if (duplicateCard[i])
                {
                    Button miningButton = miningButtons[i];
                    miningButton.Foreground = Brushes.OrangeRed;
                    miningButton.ToolTip = "Duplicate note";
                }
            }
        }
    }

    private int GetFirstVisibleListViewItemIndex()
    {
        StackPanel? firstVisibleStackPanel = PopupListView.Items.Cast<StackPanel>()
            .FirstOrDefault(static stackPanel => stackPanel.Visibility is Visibility.Visible);

        return firstVisibleStackPanel is not null
            ? PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(firstVisibleStackPanel)
            : 0;
    }

    private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
        StackPanel stackPanel = (StackPanel)sender;
        if (PopupContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || DictTabButtonsItemsControlContextMenu.IsVisible)
        {
            _listViewItemIndexAfterContextMenuIsClosed = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(stackPanel);
        }
        else
        {
            _listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(stackPanel);
            LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
        }
    }

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void Unselect(object sender, RoutedEventArgs e)
    {
        WindowsUtils.Unselect((TextBox)sender);
    }

    private void TextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        AddNameMenuItem.IsEnabled = DictUtils.SingleDictTypeDicts[DictType.CustomNameDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomNameDictionary].Ready;
        AddWordMenuItem.IsEnabled = DictUtils.SingleDictTypeDicts[DictType.CustomWordDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomWordDictionary].Ready;
        _lastInteractedTextBox = (TextBox)sender;
        LastSelectedText = _lastInteractedTextBox.SelectedText;
    }

    private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastInteractedTextBox = (TextBox)sender;
    }

    private void DefinitionsTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TextBox definitionsTextBox = (TextBox)sender;
        _lastInteractedTextBox = definitionsTextBox;

        if (!Keyboard.IsKeyDown(Key.Insert))
        {
            return;
        }

        bool isEditable = definitionsTextBox.IsReadOnly;
        definitionsTextBox.IsReadOnly = !isEditable;
        definitionsTextBox.IsUndoEnabled = isEditable;
        definitionsTextBox.AcceptsReturn = isEditable;
        definitionsTextBox.AcceptsTab = isEditable;

        if (isEditable)
        {
            definitionsTextBox.ContextMenu = _editableTextBoxContextMenu;
            definitionsTextBox.UndoLimit = -1;
        }
        else
        {
            definitionsTextBox.ContextMenu = PopupContextMenu;
            definitionsTextBox.UndoLimit = 0;
        }
    }

    private Task HandleTextBoxMouseMove(TextBox textBox, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || configManager.LookupOnSelectOnly
            || configManager.LookupOnMouseClickOnly
            || e.LeftButton is MouseButtonState.Pressed
            || PopupContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || DictTabButtonsItemsControlContextMenu.IsVisible
            || ReadingSelectionWindow.IsItVisible()
            || MiningSelectionWindow.IsItVisible()
            || (configManager.RequireLookupKeyPress
                && !configManager.LookupKeyKeyGesture.IsPressed()))
        {
            return Task.CompletedTask;
        }

        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (childPopupWindow is null)
        {
            childPopupWindow = new PopupWindow(PopupIndex + 1)
            {
                Owner = this
            };

            PopupWindowUtils.PopupWindows[PopupIndex + 1] = childPopupWindow;
        }

        if (childPopupWindow.MiningMode)
        {
            return Task.CompletedTask;
        }

        if (MiningMode)
        {
            _lastInteractedTextBox = textBox;
            if (JapaneseUtils.ContainsJapaneseCharacters(textBox.Text))
            {
                return childPopupWindow.LookupOnMouseMoveOrClick(textBox, false);
            }

            if (configManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(childPopupWindow._previousTextBox);
            }
        }

        return Task.CompletedTask;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TextBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (PopupIndex < PopupWindowUtils.MaxPopupWindowsIndex)
        {
            await HandleTextBoxMouseMove((TextBox)sender, e).ConfigureAwait(false);
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void AudioButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is not MouseButton.Right)
        {
            await HandleAudioButtonClick(false).ConfigureAwait(false);
        }
    }

    private Task HandleAudioButtonClick(bool useSelectedListViewItemIfItExists)
    {
        if (LastLookupResults.Length is 0)
        {
            return Task.CompletedTask;
        }

        StackPanel? popupListViewItem;
        int listViewItemIndex;
        if (useSelectedListViewItemIfItExists && PopupListView.SelectedItem is not null)
        {
            popupListViewItem = (StackPanel?)PopupListView.SelectedItem;
            Debug.Assert(popupListViewItem is not null);

            listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(popupListViewItem);
        }
        else
        {
            listViewItemIndex = _listViewItemIndex;
            popupListViewItem = (StackPanel?)PopupListView.Items[listViewItemIndex];
            Debug.Assert(popupListViewItem is not null);
        }

        LookupResult lookupResult = LastLookupResults[listViewItemIndex];
        if (lookupResult.Readings is null || lookupResult.Readings.Length is 1)
        {
            return PopupWindowUtils.PlayAudio(lookupResult.PrimarySpelling, lookupResult.Readings?[0]);
        }

        Point position;
        if (useSelectedListViewItemIfItExists)
        {
            Button? audioButton = popupListViewItem.GetChildByName<Button>("AudioButton");
            if (audioButton is not null)
            {
                position = audioButton.PointToScreen(default);
                position.Y += 7;
                position.X += 7;
            }
            else
            {
                position = WinApi.GetMousePosition();
            }
        }
        else
        {
            position = WinApi.GetMousePosition();
        }

        ReadingSelectionWindow.Show(this, lookupResult.PrimarySpelling, lookupResult.Readings, position);
        return Task.CompletedTask;
    }

    private Task HandleMining(bool minePrimarySpelling, bool useSelectedListViewItemIfItExists)
    {
        if (LastLookupResults.Length is 0 || PopupListView.Items.Count is 0)
        {
            return Task.CompletedTask;
        }

        StackPanel? popupListViewItem;
        int listViewItemIndex;
        if (useSelectedListViewItemIfItExists && PopupListView.SelectedItem is not null)
        {
            popupListViewItem = (StackPanel)PopupListView.SelectedItem;
            listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(popupListViewItem);
        }
        else
        {
            listViewItemIndex = _listViewItemIndex;
            popupListViewItem = (StackPanel?)PopupListView.Items[listViewItemIndex];
        }

        LookupResult[] lookupResults = LastLookupResults;
        LookupResult lookupResult = lookupResults[listViewItemIndex];
        string currentSourceText = _currentSourceText;
        int currentSourceTextCharPosition = _currentSourceTextCharPosition;

        if (minePrimarySpelling
            || lookupResult.Readings is null
            || DictUtils.KanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            TextBox? definitionsTextBox = GetDefinitionTextBox(listViewItemIndex);
            string? formattedDefinitions = null;
            if (definitionsTextBox is not null)
            {
                formattedDefinitions = definitionsTextBox.Text;
            }

            string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

            HidePopup();

            return ConfigManager.Instance.MineToFileInsteadOfAnki
                ? MiningUtils.MineToFile(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling)
                : MiningUtils.Mine(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling);
        }

        Point position;
        if (useSelectedListViewItemIfItExists)
        {
            Debug.Assert(popupListViewItem is not null);
            Button? audioButton = popupListViewItem.GetChildByName<Button>("MiningButton");
            if (audioButton is not null)
            {
                position = audioButton.PointToScreen(default);
                position.Y += 5;
                position.X += 7;
            }
            else
            {
                position = WinApi.GetMousePosition();
            }
        }
        else
        {
            position = WinApi.GetMousePosition();
        }

        MiningSelectionWindow.Show(this, lookupResults, listViewItemIndex, currentSourceText, currentSourceTextCharPosition, position);
        return Task.CompletedTask;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MiningButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        bool minePrimarySpelling = e.ChangedButton == configManager.MinePrimarySpellingMouseButton;
        if (!MiningMode
            || (!minePrimarySpelling && e.ChangedButton != configManager.MineMouseButton))
        {
            return;
        }

        await HandleMining(minePrimarySpelling, false).ConfigureAwait(false);
    }

    private void ShowAddNameWindow(bool useSelectedListViewItemIfItExists)
    {
        string text;
        string reading = "";

        bool useSelectedItem = useSelectedListViewItemIfItExists && PopupListView.SelectedItem is not null;
        if (useSelectedItem
            || _lastInteractedTextBox is null
            || _lastInteractedTextBox.SelectionLength is 0)
        {
            int listViewItemIndex;
            if (useSelectedItem)
            {
                StackPanel? mainStackPanel = (StackPanel?)PopupListView.SelectedItem;
                Debug.Assert(mainStackPanel is not null);
                listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel(mainStackPanel);
            }
            else
            {
                listViewItemIndex = _listViewItemIndex;
            }

            LookupResult lookupResult = LastLookupResults[listViewItemIndex];

            text = lookupResult.PrimarySpelling;
            string[]? readings = lookupResult.Readings;
            reading = readings?.Length is 1
                ? readings[0]
                : "";
        }
        else
        {
            text = _lastInteractedTextBox.SelectedText;
            PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
            if (childPopupWindow is not null && text == childPopupWindow.LastSelectedText)
            {
                string[]? readings = childPopupWindow.LastLookupResults[0].Readings;
                reading = readings?.Length is 1
                    ? readings[0]
                    : "";
            }
        }

        if (reading == text)
        {
            reading = "";
        }

        WindowsUtils.ShowAddNameWindow(this, text, reading);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    public Task HandleHotKey(KeyGesture keyGesture)
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        if (keyGesture.IsEqual(configManager.DisableHotkeysKeyGesture))
        {
            configManager.DisableHotkeys = !configManager.DisableHotkeys;
            if (configManager.GlobalHotKeys)
            {
                if (configManager.DisableHotkeys)
                {
                    WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
                }
                else
                {
                    WinApi.RegisterAllGlobalHotKeys(mainWindow.WindowHandle);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.MiningModeKeyGesture))
        {
            if (MiningMode)
            {
                return Task.CompletedTask;
            }

            EnableMiningMode();
            DisplayResults();

            if (configManager.Focusable)
            {
                if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
                {
                    nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                    if (previousWindowHandle != mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
                    {
                        WindowsUtils.LastActiveWindowHandle = previousWindowHandle;
                    }
                }

                if (!Activate())
                {
                    WinApi.StealFocus(WindowHandle);
                }
            }

            _ = Focus();

            if (configManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }
        }

        else if (keyGesture.IsEqual(configManager.PlayAudioKeyGesture))
        {
            return PlayAudio(true);
        }

        else if (keyGesture.IsEqual(configManager.ClickAudioButtonKeyGesture))
        {
            if (MiningMode)
            {
                return HandleAudioButtonClick(true);
            }
        }

        else if (keyGesture.IsEqual(configManager.ClosePopupKeyGesture))
        {
            HidePopup();
        }

        else if (keyGesture.IsEqual(configManager.KanjiModeKeyGesture))
        {
            return HandleLookupCategoryKeyGesture(LookupCategory.Kanji);
        }

        else if (keyGesture.IsEqual(configManager.NameModeKeyGesture))
        {
            return HandleLookupCategoryKeyGesture(LookupCategory.Name);
        }

        else if (keyGesture.IsEqual(configManager.WordModeKeyGesture))
        {
            return HandleLookupCategoryKeyGesture(LookupCategory.Word);
        }

        else if (keyGesture.IsEqual(configManager.OtherModeKeyGesture))
        {
            return HandleLookupCategoryKeyGesture(LookupCategory.Other);
        }

        else if (keyGesture.IsEqual(configManager.AllModeKeyGesture))
        {
            return HandleLookupCategoryKeyGesture(LookupCategory.All);
        }

        else if (keyGesture.IsEqual(configManager.ShowAddNameWindowKeyGesture))
        {
            if (DictUtils.SingleDictTypeDicts[DictType.CustomNameDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomNameDictionary].Ready)
            {
                if (!MiningMode)
                {
                    if (PopupIndex > 0)
                    {
                        PopupWindow? popupWindow = PopupWindowUtils.PopupWindows[PopupIndex - 1];
                        Debug.Assert(popupWindow is not null);
                        popupWindow.ShowAddNameWindow(true);
                    }

                    else
                    {
                        mainWindow.ShowAddNameWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddNameWindow(false);
                }

                PopupWindowUtils.PopupAutoHideTimer.Start();
            }
        }

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
        {
            if (DictUtils.SingleDictTypeDicts[DictType.CustomWordDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomWordDictionary].Ready)
            {
                if (!MiningMode)
                {
                    if (PopupIndex > 0)
                    {
                        PopupWindow? popupWindow = PopupWindowUtils.PopupWindows[PopupIndex - 1];
                        Debug.Assert(popupWindow is not null);
                        popupWindow.ShowAddWordWindow(true);
                    }

                    else
                    {
                        mainWindow.ShowAddWordWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddWordWindow(false);
                }

                PopupWindowUtils.PopupAutoHideTimer.Start();
            }
        }

        else if (keyGesture.IsEqual(configManager.SearchWithBrowserKeyGesture))
        {
            if (!MiningMode)
            {
                if (PopupIndex > 0)
                {
                    PopupWindow? popupWindow = PopupWindowUtils.PopupWindows[PopupIndex - 1];
                    Debug.Assert(popupWindow is not null);
                    popupWindow.SearchWithBrowser(true);
                }

                else
                {
                    mainWindow.SearchWithBrowser();
                }

                HidePopup();
            }

            else
            {
                SearchWithBrowser(false);
            }
        }

        else if (keyGesture.IsEqual(configManager.InactiveLookupModeKeyGesture))
        {
            configManager.InactiveLookupMode = !configManager.InactiveLookupMode;
        }

        else if (keyGesture.IsEqual(configManager.MotivationKeyGesture))
        {
            return WindowsUtils.Motivate();
        }

        else if (keyGesture.IsEqual(configManager.NextDictKeyGesture))
        {
            bool foundSelectedButton = false;

            Button? nextButton = null;
            ItemCollection dictButtons = DictTabButtonsItemsControl.Items;
            int buttonCount = dictButtons.Count;
            for (int i = 0; i < buttonCount; i++)
            {
                Button? button = (Button?)dictButtons[i];
                Debug.Assert(button is not null);

                if (button.Background == Brushes.DodgerBlue)
                {
                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    nextButton = button;
                    break;
                }
            }

            ClickDictTypeButton(nextButton ?? AllDictionaryTabButton);
        }

        else if (keyGesture.IsEqual(configManager.PreviousDictKeyGesture))
        {
            bool foundSelectedButton = false;
            Button? previousButton = null;

            ItemCollection dictButtons = DictTabButtonsItemsControl.Items;
            int dictCount = dictButtons.Count;
            for (int i = dictCount - 1; i >= 0; i--)
            {
                Button? button = (Button?)dictButtons[i];
                Debug.Assert(button is not null);

                if (button.Background == Brushes.DodgerBlue)
                {
                    foundSelectedButton = true;
                    continue;
                }

                if (foundSelectedButton && button.IsEnabled)
                {
                    previousButton = button;
                    break;
                }
            }

            if (previousButton is not null)
            {
                ClickDictTypeButton(previousButton);
            }

            else
            {
                for (int i = dictCount - 1; i > 0; i--)
                {
                    Button? button = (Button?)dictButtons[i];
                    Debug.Assert(button is not null);

                    if (button.IsEnabled)
                    {
                        ClickDictTypeButton(button);
                        break;
                    }
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.ToggleVisibilityOfDictionaryTabsInMiningModeKeyGesture))
        {
            ToggleVisibilityOfDictTabs();
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            PopupWindowUtils.HidePopups(0);

            if (configManager.Focusable)
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

        else if (keyGesture.IsEqual(configManager.SelectedTextToSpeechKeyGesture))
        {
            if (MiningMode && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string text;
                if (PopupListView.SelectedItem is not null)
                {
                    int listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
                    text = LastLookupResults[listViewItemIndex].PrimarySpelling;
                }
                else
                {
                    TextBox? lastInteractedTextBox = _lastInteractedTextBox;
                    text = lastInteractedTextBox is not null && lastInteractedTextBox.SelectionLength > 0
                        ? lastInteractedTextBox.SelectedText
                        : LastLookupResults[_listViewItemIndex].PrimarySpelling;
                }

                return SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, text);
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectNextItemKeyGesture))
        {
            if (MiningMode)
            {
                WindowsUtils.SelectNextListViewItem(PopupListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectPreviousItemKeyGesture))
        {
            if (MiningMode)
            {
                WindowsUtils.SelectPreviousListViewItem(PopupListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.ConfirmItemSelectionKeyGesture))
        {
            if (MiningMode && PopupListView.SelectedItem is not null)
            {
                return HandleMining(true, true);
            }
        }

        else if (keyGesture.IsEqual(configManager.ClickMiningButtonKeyGesture))
        {
            if (MiningMode)
            {
                return HandleMining(false, true);
            }
        }

        else if (keyGesture.IsEqual(configManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            configManager.AlwaysShowMainTextBoxCaret = !configManager.AlwaysShowMainTextBoxCaret;
            mainWindow.MainTextBox.IsReadOnlyCaretVisible = configManager.AlwaysShowMainTextBoxCaret;
        }

        else if (keyGesture.IsEqual(configManager.LookupSelectedTextKeyGesture))
        {
            if (MiningMode)
            {
                TextBox? lastInteractedTextBox = _lastInteractedTextBox;
                if (lastInteractedTextBox is not null && lastInteractedTextBox.SelectionLength > 0 && PopupIndex < PopupWindowUtils.MaxPopupWindowsIndex)
                {
                    PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
                    if (childPopupWindow is null)
                    {
                        childPopupWindow = new PopupWindow(PopupIndex + 1)
                        {
                            Owner = this
                        };

                        PopupWindowUtils.PopupWindows[PopupIndex + 1] = childPopupWindow;
                    }

                    return childPopupWindow.LookupOnSelect(lastInteractedTextBox);
                }
            }

            else if (PopupIndex > 0)
            {
                PopupWindow? popupWindow = PopupWindowUtils.PopupWindows[PopupIndex - 1];
                Debug.Assert(popupWindow is not null);

                TextBox? lastInteractedTextBox = popupWindow._lastInteractedTextBox;
                if (lastInteractedTextBox is not null && lastInteractedTextBox.SelectionLength > 0)
                {
                    return LookupOnSelect(lastInteractedTextBox);
                }
            }

            else
            {
                return mainWindow.FirstPopupWindow.LookupOnSelect(mainWindow.MainTextBox);
            }
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            HidePopup();
        }

        return Task.CompletedTask;
    }

    private Task HandleLookupCategoryKeyGesture(LookupCategory lookupCategory)
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.LookupCategory = coreConfigManager.LookupCategory == lookupCategory
            ? LookupCategory.All
            : lookupCategory;

        if (IsVisible && _previousTextBox is not null)
        {
            HidePopup();

            return configManager.LookupOnSelectOnly
                ? LookupOnSelect(_previousTextBox)
                : LookupOnMouseMoveOrClick(_previousTextBox, configManager.LookupOnMouseClickOnly);
        }

        return Task.CompletedTask;
    }

    private void EnableMiningMode()
    {
        MiningMode = true;

        TitleBarGrid.Visibility = Visibility.Visible;
        if (ConfigManager.Instance.ShowDictionaryTabsInMiningMode)
        {
            DictTabButtonsItemsControl.Visibility = Visibility.Visible;
        }
    }

    private void ToggleVisibilityOfDictTabs()
    {
        if (!MiningMode)
        {
            return;
        }

        DictTabButtonsItemsControl.Visibility = DictTabButtonsItemsControl.Visibility is Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private Task PlayAudio(bool useSelectedListViewItemIfItExists)
    {
        if (LastLookupResults.Length is 0)
        {
            return Task.CompletedTask;
        }

        int listViewItemIndex = PopupListView.SelectedItem is not null && useSelectedListViewItemIfItExists
            ? PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem)
            : _listViewItemIndex;

        LookupResult lastLookupResult = LastLookupResults[listViewItemIndex];
        string primarySpelling = lastLookupResult.PrimarySpelling;
        string? reading = lastLookupResult.Readings?[0];

        return PopupWindowUtils.PlayAudio(primarySpelling, reading);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (configManager is { LookupOnSelectOnly: false, LookupOnMouseClickOnly: false }
            && childPopupWindow is { IsVisible: true, MiningMode: false })
        {
            childPopupWindow.HidePopup();
        }

        if (MiningMode)
        {
            if (childPopupWindow is null || !childPopupWindow.IsVisible)
            {
                PopupWindowUtils.PopupAutoHideTimer.Stop();
            }

            return;
        }

        if (UnavoidableMouseEnter
            || (configManager.FixedPopupPositioning && PopupIndex is 0))
        {
            return;
        }

        HidePopup();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        _lastInteractedTextBox = textBox;
        LastSelectedText = _lastInteractedTextBox.SelectedText;

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            || ((!configManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!configManager.LookupOnMouseClickOnly || e.ChangedButton != configManager.LookupOnClickMouseButton)))
        {
            return;
        }

        await HandleTextBoxMouseUp(textBox).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void PrimarySpellingTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        _lastInteractedTextBox = textBox;
        LastSelectedText = _lastInteractedTextBox.SelectedText;

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            || ((!configManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!configManager.LookupOnMouseClickOnly || e.ChangedButton != configManager.LookupOnClickMouseButton)))
        {
            if ((configManager.LookupOnMouseClickOnly
                || configManager.LookupOnSelectOnly
                || e.ChangedButton != configManager.MiningModeMouseButton)
                    && e.ChangedButton == configManager.MinePrimarySpellingMouseButton)
            {
                await HandleMining(true, false).ConfigureAwait(false);
            }
        }
        else
        {
            await HandleTextBoxMouseUp(textBox).ConfigureAwait(false);
        }
    }

    private Task HandleTextBoxMouseUp(TextBox textBox)
    {
        if (PopupIndex >= PopupWindowUtils.MaxPopupWindowsIndex)
        {
            return Task.CompletedTask;
        }

        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (childPopupWindow is null)
        {
            childPopupWindow = new PopupWindow(PopupIndex + 1)
            {
                Owner = this
            };

            PopupWindowUtils.PopupWindows[PopupIndex + 1] = childPopupWindow;
        }

        return ConfigManager.Instance.LookupOnSelectOnly
            ? childPopupWindow.LookupOnSelect(textBox)
            : childPopupWindow.LookupOnMouseMoveOrClick(textBox, true);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (childPopupWindow is { IsVisible: true, MiningMode: false, UnavoidableMouseEnter: false })
        {
            childPopupWindow.HidePopup();
        }

        if (IsMouseOver)
        {
            return;
        }

        if (MiningMode)
        {
            if (ConfigManager.Instance.AutoHidePopupIfMouseIsNotOverIt)
            {
                if (PopupContextMenu.IsVisible
                    || TitleBarContextMenu.IsVisible
                    || DictTabButtonsItemsControlContextMenu.IsVisible
                    || ReadingSelectionWindow.IsItVisible()
                    || MiningSelectionWindow.IsItVisible()
                    || AddWordWindow.IsItVisible()
                    || AddNameWindow.IsItVisible())
                {
                    PopupWindowUtils.PopupAutoHideTimer.Stop();
                }

                else if (childPopupWindow is null || !childPopupWindow.IsVisible)
                {
                    PopupWindowUtils.PopupAutoHideTimer.Stop();
                    PopupWindowUtils.PopupAutoHideTimer.Start();
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
        List<Button> buttons = new(DictUtils.Dicts.Values.Count + 1);
        AllDictionaryTabButton.Background = Brushes.DodgerBlue;
        buttons.Add(AllDictionaryTabButton);


        double buttonFontSize = ConfigManager.Instance.PopupDictionaryTabFontSize;
        IOrderedEnumerable<Dict> dicts = DictUtils.Dicts.Values.OrderBy(static dict => dict.Priority);
        foreach (Dict dict in dicts)
        {
            if (!dict.Active || dict.Type is DictType.PitchAccentYomichan || (ConfigManager.Instance.HideDictTabsWithNoResults && !_dictsWithResults.Contains(dict)))
            {
                continue;
            }

            Button button = new()
            {
                Content = dict.Name,
                Margin = new Thickness(2, 0, 0, 0),
                Tag = dict,
                Cursor = Cursors.Arrow,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                FontSize = buttonFontSize,
                Padding = new Thickness(5, 3, 5, 3),
                Height = double.NaN,
                Width = double.NaN
            };

            button.Click += DictTypeButtonOnClick;

            if (!_dictsWithResults.Contains(dict))
            {
                button.IsEnabled = false;
            }

            buttons.Add(button);
        }

        DictTabButtonsItemsControl.ItemsSource = buttons;
    }

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        ClickDictTypeButton((Button)sender);
    }

    private void ClickDictTypeButton(Button button)
    {
        foreach (Button btn in DictTabButtonsItemsControl.Items.Cast<Button>())
        {
            btn.ClearValue(BackgroundProperty);
        }

        button.Background = Brushes.DodgerBlue;

        bool isAllButton = button == AllDictionaryTabButton;
        if (isAllButton)
        {
            PopupListView.Items.Filter = PopupWindowUtils.NoAllDictFilter;
        }

        else
        {
            _filteredDict = (Dict)button.Tag;
            PopupListView.Items.Filter = DictFilter;
        }

        Debug.Assert(_popupListViewScrollViewer is not null);
        _popupListViewScrollViewer.ScrollToTop();
        _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
        _listViewItemIndex = _firstVisibleListViewItemIndex;
        LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;

        WindowsUtils.Unselect(_lastInteractedTextBox);
        _lastInteractedTextBox = null;
    }

    private bool DictFilter(object item)
    {
        StackPanel items = (StackPanel)item;
        return (Dict)items.Tag == _filteredDict;
    }

    private void ContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        bool contextMenuBecameInvisible = !(bool)e.NewValue;
        if (contextMenuBecameInvisible)
        {
            _listViewItemIndex = _listViewItemIndexAfterContextMenuIsClosed;
            LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;

            if (!IsMouseOver
                && !PopupContextMenu.IsVisible
                && !TitleBarContextMenu.IsVisible
                && !DictTabButtonsItemsControlContextMenu.IsVisible
                && !AddWordWindow.IsItVisible()
                && !AddNameWindow.IsItVisible())
            {
                PopupWindowUtils.PopupAutoHideTimer.Start();
            }
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ReadingSelectionWindow.HideWindow();
        MiningSelectionWindow.CloseWindow();

        ConfigManager configManager = ConfigManager.Instance;
        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (childPopupWindow is not null
            && e.ChangedButton is not MouseButton.Right
            && e.ChangedButton != configManager.MiningModeMouseButton)
        {
            PopupWindowUtils.HidePopups(childPopupWindow.PopupIndex);
        }
        else if (e.ChangedButton == configManager.MiningModeMouseButton)
        {
            if (!MiningMode)
            {
                ShowMiningModeResults();
            }

            else if (childPopupWindow is not null && childPopupWindow.IsVisible)
            {
                if (!childPopupWindow.MiningMode)
                {
                    childPopupWindow.ShowMiningModeResults();
                }
                else
                {
                    PopupWindowUtils.HidePopups(childPopupWindow.PopupIndex);
                }
            }
        }
    }

    private void ToggleVisibilityOfDictTabs(object sender, RoutedEventArgs e)
    {
        ToggleVisibilityOfDictTabs();
    }

    private void HidePopup(object sender, RoutedEventArgs e)
    {
        HidePopup();
    }

    public void HidePopup()
    {
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopup = PopupIndex is 0;

        ConfigManager configManager = ConfigManager.Instance;
        if (isFirstPopup
            && (configManager.TextOnlyVisibleOnHover || configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible())
        {
            _ = mainWindow.ChangeVisibility().ConfigureAwait(true);
        }

        if (!IsVisible)
        {
            return;
        }

        ReadingSelectionWindow.HideWindow();
        MiningSelectionWindow.CloseWindow();

        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (isFirstPopup && childPopupWindow is not null)
        {
            childPopupWindow.Close();
            PopupWindowUtils.PopupWindows[PopupIndex + 1] = null;
        }

        MiningMode = false;
        TitleBarGrid.Visibility = Visibility.Collapsed;
        DictTabButtonsItemsControl.Visibility = Visibility.Collapsed;
        DictTabButtonsItemsControl.ItemsSource = null;

        Debug.Assert(_popupListViewScrollViewer is not null);
        _popupListViewScrollViewer.ScrollToTop();
        PopupListView.UpdateLayout();

        PopupListView.ItemsSource = null;
        _lastLookedUpText = "";
        _currentSourceText = "";
        _listViewItemIndex = 0;
        _firstVisibleListViewItemIndex = 0;
        _currentSourceTextCharPosition = 0;
        _lastInteractedTextBox = null;

        PopupWindowUtils.PopupAutoHideTimer.Stop();

        UpdateLayout();
        Hide();

        if (AddNameWindow.IsItVisible() || AddWordWindow.IsItVisible())
        {
            return;
        }

        if (isFirstPopup)
        {
            WinApi.ActivateWindow(mainWindow.WindowHandle);

            if (configManager.HighlightLongestMatch && !mainWindow.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_previousTextBox);
            }

            nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
            if (configManager.RestoreFocusToPreviouslyActiveWindow
                && lastActiveWindowHandle is not 0
                && lastActiveWindowHandle != mainWindow.WindowHandle
                && ((configManager.PopupFocusOnLookup && mainWindow.WindowState is WindowState.Minimized)
                    || (configManager.MainWindowFocusOnHover && !mainWindow.IsMouseOver)))
            {
                WinApi.GiveFocusToWindow(lastActiveWindowHandle);
            }
        }

        else
        {
            PopupWindow? previousPopup = PopupWindowUtils.PopupWindows[PopupIndex - 1];
            Debug.Assert(previousPopup is not null);

            if (previousPopup.IsVisible)
            {
                WinApi.ActivateWindow(previousPopup.WindowHandle);
            }

            if (configManager.HighlightLongestMatch && !previousPopup._contextMenuIsOpening)
            {
                WindowsUtils.Unselect(_previousTextBox);
            }
        }
    }

    private void Window_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextMenuIsOpening = true;
        PopupWindowUtils.HidePopups(PopupIndex + 1);
        _contextMenuIsOpening = false;
    }

    private void PopupListView_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!PopupContextMenu.IsVisible
            && !TitleBarContextMenu.IsVisible
            && !DictTabButtonsItemsControlContextMenu.IsVisible
            && LastLookupResults.Length > 0)
        {
            _listViewItemIndex = _firstVisibleListViewItemIndex;
            LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
        }
    }

    public TextBox? GetDefinitionTextBox(int listViewIndex)
    {
        PopupListView.Items.Filter = null;

        StackPanel? mainStackPanel = (StackPanel?)PopupListView.Items[listViewIndex];
        Debug.Assert(mainStackPanel is not null);

        return mainStackPanel.Children[1].GetChildByName<TextBox>(nameof(LookupResult.FormattedDefinitions));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HidePopup();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton is MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    public void SetSizeToContent(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, double minWidth, double minHeight)
    {
        MaxHeight = maxHeight;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MinWidth = minWidth;

        if (dynamicWidth && dynamicHeight)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            SizeToContent = SizeToContent.Height;
            Width = maxWidth;
        }

        else if (dynamicWidth)
        {
            SizeToContent = SizeToContent.Width;
            Height = maxHeight;
        }

        else
        {
            SizeToContent = SizeToContent.Manual;
            Height = maxHeight;
            Width = maxWidth;
        }
    }

    public void ShowMiningModeResults()
    {
        EnableMiningMode();
        DisplayResults();
        UpdatePosition();

        ConfigManager configManager = ConfigManager.Instance;

        if (configManager.Focusable)
        {
            MainWindow mainWindow = MainWindow.Instance;
            if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
            {
                nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                if (previousWindowHandle != mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
                {
                    WindowsUtils.LastActiveWindowHandle = previousWindowHandle;
                }
            }

            if (!Activate())
            {
                WinApi.StealFocus(WindowHandle);
            }
        }

        _ = Focus();

        WinApi.BringToFront(WindowHandle);

        if (configManager.AutoHidePopupIfMouseIsNotOverIt)
        {
            PopupWindowUtils.SetPopupAutoHideTimer();
        }
    }

    private void PopupListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if ((childPopupWindow is not null && childPopupWindow.IsVisible)
            || ReadingSelectionWindow.IsItVisible()
            || MiningSelectionWindow.IsItVisible())
        {
            e.Handled = true;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Owner = null;
        PopupWindowUtils.PopupWindows[PopupIndex] = null;
        _previousTextBox = null;
        _lastInteractedTextBox = null;
        LastSelectedText = null;
        _lastLookedUpText = null;
        _filteredDict = null;
        _popupListViewScrollViewer = null;
        DictTabButtonsItemsControl.ItemsSource = null;
        PopupListView.ItemsSource = null;
        _lastInteractedTextBox = null;
        LastLookupResults.AsSpan().Clear();
        _dictsWithResults.Clear();
        AllDictionaryTabButton.Click -= DictTypeButtonOnClick;
    }
}
