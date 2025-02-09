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
    public PopupWindow? ChildPopupWindow { get; private set; }

    private bool _contextMenuIsOpening; // = false;

    private TextBox? _previousTextBox;

    private TextBox? _lastInteractedTextBox;

    private int _listViewItemIndex; // 0

    private int _listViewItemIndexAfterContextMenuIsClosed; // 0

    private int _firstVisibleListViewItemIndex; // 0

    private int _currentSourceTextCharPosition;

    private string _currentSourceText = "";

    private Button _buttonAll = new()
    {
        Content = "All",
        Margin = new Thickness(1),
        Background = Brushes.DodgerBlue
    };

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    public LookupResult[] LastLookupResults { get; private set; } = [];

    private List<Dict> _dictsWithResults = [];

    private Dict? _filteredDict;

    public bool UnavoidableMouseEnter { get; private set; } // = false;

    private string? _lastLookedUpText;

    public bool MiningMode { get; private set; }

    private ScrollViewer? _popupListViewScrollViewer;

    private readonly ContextMenu _editableTextBoxContextMenu = new();

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

        AddMenuItemsToEditableTextBoxContextMenu();

        if (configManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder.Text = string.Create(CultureInfo.InvariantCulture,
                $"Click the ➕ button to mine,\nor press {configManager.ClosePopupKeyGesture.ToFormattedString()} or click on the main window to exit.");
            TextBlockMiningModeReminder.ToolTip = "This message can be hidden by disabling Preferences->Popup->Show mining mode reminder";
            TextBlockMiningModeReminder.Cursor = Cursors.Help;
        }
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
        _lastInteractedTextBox!.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(_lastInteractedTextBox)!, 0, Key.Back)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
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
            : LastLookupResults[_listViewItemIndex].PrimarySpelling;

        WindowsUtils.ShowAddWordWindow(this, text);
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        SearchWithBrowser();
    }

    private void SearchWithBrowser()
    {
        string text = _lastInteractedTextBox?.SelectionLength > 0
            ? _lastInteractedTextBox.SelectedText
            : LastLookupResults[_listViewItemIndex].PrimarySpelling;

        WindowsUtils.SearchWithBrowser(text);
    }

    public Task LookupOnCharPosition(TextBox textBox, int charPosition, bool enableMiningMode)
    {
        string textBoxText = textBox.Text;

        _currentSourceText = textBoxText;
        _currentSourceTextCharPosition = charPosition;

        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        if (this != mainWindow.FirstPopupWindow
                ? configManager.DisableLookupsForNonJapaneseCharsInPopups
                  && !JapaneseUtils.JapaneseRegex.IsMatch(textBoxText[charPosition].ToString())
                : configManager.DisableLookupsForNonJapaneseCharsInMainWindow
                  && !JapaneseUtils.JapaneseRegex.IsMatch(textBoxText[charPosition].ToString()))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        string textToLookUp = textBoxText.Length - charPosition > configManager.MaxSearchLength
            ? textBoxText[..(charPosition + configManager.MaxSearchLength)]
            : textBoxText;

        int endPosition = JapaneseUtils.FindExpressionBoundary(textToLookUp, charPosition);
        textToLookUp = textToLookUp[charPosition..endPosition];

        if (string.IsNullOrEmpty(textToLookUp))
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

        if (lookupResults?.Length > 0)
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
                WinApi.ActivateWindow(this == mainWindow.FirstPopupWindow
                    ? mainWindow.WindowHandle
                    : ((PopupWindow)Owner).WindowHandle);

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
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (configManager.AutoPlayAudio)
            {
                return PlayAudio();
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

        if (charPosition > 0 && char.IsHighSurrogate(textBox.Text[charPosition - 1]))
        {
            --charPosition;
        }

        return LookupOnCharPosition(textBox, charPosition, enableMiningMode);
    }

    public Task LookupOnSelect(TextBox textBox)
    {
        _currentSourceText = textBox.Text;
        _currentSourceTextCharPosition = textBox.SelectionStart;

        string selectedText = textBox.SelectedText;
        if (string.IsNullOrWhiteSpace(selectedText))
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

        if (lookupResults?.Length > 0)
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
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (configManager.AutoPlayAudio)
            {
                return PlayAudio();
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
        if (ConfigManager.Instance.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
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
        bool showPOrthographyInfo = jmdict.Options.POrthographyInfo!.Value;
        bool showROrthographyInfo = jmdict.Options.ROrthographyInfo!.Value;
        bool showAOrthographyInfo = jmdict.Options.AOrthographyInfo!.Value;
        double pOrthographyInfoFontSize = jmdict.Options.POrthographyInfoFontSize!.Value;

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
            _ = CheckResultForDuplicates(duplicateIcons!);
        }

        GenerateDictTypeButtons();
        UpdateLayout();
    }

    private void AddEventHandlersToTextBox(FrameworkElement textBox)
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
            new Thickness(2, 0, 0, 0),
            PopupContextMenu);

            AddEventHandlersToTextBox(primarySpellingFrameworkElement);
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
                result.PitchPositions!);

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
        if (showPOrthographyInfo && jmdictLookupResult?.PrimarySpellingOrthographyInfoList is not null)
        {
            TextBlock textBlockPOrthographyInfo = PopupWindowUtils.CreateTextBlock(nameof(jmdictLookupResult.PrimarySpellingOrthographyInfoList),
                $"[{string.Join(", ", jmdictLookupResult.PrimarySpellingOrthographyInfoList)}]",
                DictOptionManager.POrthographyInfoColor,
                pOrthographyInfoFontSize,
                VerticalAlignment.Center,
                new Thickness(3, 0, 0, 0));

            _ = top.Children.Add(textBlockPOrthographyInfo);
        }

        if (result.Readings is not null && configManager.ReadingsFontSize > 0
                                        && (pitchPositionsExist || result.KanjiLookupResult is null || (result.KanjiLookupResult.KunReadings is null && result.KanjiLookupResult.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && jmdictLookupResult?.ReadingsOrthographyInfoList is not null
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
                        result.PitchPositions!);

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
                        result.PitchPositions!);

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

        if (MiningMode)
        {
            Button audioButton = new()
            {
                Name = nameof(audioButton),
                Content = "🔊",
                Foreground = configManager.DefinitionsColor,
                VerticalAlignment = VerticalAlignment.Top,
                // VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                // HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = 12
            };

            audioButton.Click += AudioButton_Click;

            _ = top.Children.Add(audioButton);
        }

        if (result.AlternativeSpellings is not null && configManager.AlternativeSpellingsFontSize > 0)
        {
            string alternativeSpellingsText = showAOrthographyInfo && result.JmdictLookupResult?.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToTextWithParentheses(result.AlternativeSpellings, result.JmdictLookupResult.AlternativeSpellingsOrthographyInfoList)
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

        if (result.DeconjugationProcess is not null && configManager.DeconjugationInfoFontSize > 0)
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

        if (result.Frequencies is not null)
        {
            List<LookupFrequencyResult> validFrequencies = result.Frequencies
                .Where(static f => f.Freq is > 0 and < int.MaxValue).ToList();

            if (validFrequencies.Count > 0)
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

        if (MiningMode)
        {
            Button miningButton = new()
            {
                Name = nameof(miningButton),
                Content = '➕',
                ToolTip = "Mine",
                Foreground = configManager.DefinitionsColor,
                VerticalAlignment = VerticalAlignment.Top,
                VerticalContentAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = 12
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
                if (MiningMode)
                {
                    TextBox compositionTextBox = PopupWindowUtils.CreateTextBox(nameof(kanjiLookupResult.KanjiComposition),
                        $"Composition: {kanjiLookupResult.KanjiComposition}",
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
                        $"Composition: {kanjiLookupResult.KanjiComposition}",
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
        if (PopupContextMenu.IsVisible)
        {
            _listViewItemIndexAfterContextMenuIsClosed = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)sender);
        }
        else
        {
            _listViewItemIndex = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)sender);
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
            || ReadingSelectionWindow.IsItVisible()
            || MiningSelectionWindow.IsItVisible()
            || (configManager.RequireLookupKeyPress
                && !configManager.LookupKeyKeyGesture.IsPressed()))
        {
            return Task.CompletedTask;
        }

        ChildPopupWindow ??= new PopupWindow
        {
            Owner = this
        };

        if (ChildPopupWindow.MiningMode)
        {
            return Task.CompletedTask;
        }

        if (MiningMode)
        {
            _lastInteractedTextBox = textBox;
            if (JapaneseUtils.JapaneseRegex.IsMatch(textBox.Text))
            {
                return ChildPopupWindow.LookupOnMouseMoveOrClick(textBox, false);
            }

            if (configManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(ChildPopupWindow._previousTextBox);
            }
        }

        return Task.CompletedTask;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TextBox_MouseMove(object sender, MouseEventArgs e)
    {
        await HandleTextBoxMouseMove((TextBox)sender, e).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void AudioButton_Click(object sender, RoutedEventArgs e)
    {
        LookupResult lookupResult = LastLookupResults[_listViewItemIndex];
        if (lookupResult.Readings is null || lookupResult.Readings.Length is 1)
        {
            await PopupWindowUtils.PlayAudio(lookupResult.PrimarySpelling, lookupResult.Readings?[0]).ConfigureAwait(false);
        }
        else
        {
            ReadingSelectionWindow.Show(this, lookupResult.PrimarySpelling, lookupResult.Readings);
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MiningButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!MiningMode || e.ChangedButton is MouseButton.Right)
        {
            return;
        }

        int listViewItemIndex = _listViewItemIndex;
        TextBox? definitionsTextBox = GetDefinitionTextBox(listViewItemIndex);
        string? formattedDefinitions = definitionsTextBox?.Text;
        string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);
        string currentSourceText = _currentSourceText;
        int currentSourceTextCharPosition = _currentSourceTextCharPosition;
        LookupResult[] lookupResults = LastLookupResults;

        LookupResult lookupResult = lookupResults[listViewItemIndex];
        if (lookupResult.Readings is null)
        {
            HidePopup();

            ConfigManager configManager = ConfigManager.Instance;
            if (configManager.MineToFileInsteadOfAnki)
            {
                await MiningUtils.MineToFile(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling).ConfigureAwait(false);
            }
            else
            {
                await MiningUtils.Mine(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling).ConfigureAwait(false);
            }
        }
        else
        {
            MiningSelectionWindow.Show(this, lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition);
        }
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
            text = LastLookupResults[_listViewItemIndex].PrimarySpelling;

            string[]? readings = LastLookupResults[_listViewItemIndex].Readings;
            reading = readings?.Length is 1
                ? readings[0]
                : "";
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
        int nextItemIndex = PopupListView.SelectedIndex - 1 >= 0
            ? PopupListView.SelectedIndex - 1
            : PopupListView.Items.Count - 1;

        PopupListView.SelectedIndex = nextItemIndex;

        PopupListView.ScrollIntoView(PopupListView.Items.GetItemAt(nextItemIndex));
    }

    public async Task HandleHotKey(KeyGesture keyGesture, KeyEventArgs? e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        bool handled = false;
        if (keyGesture.IsEqual(configManager.DisableHotkeysKeyGesture))
        {
            if (e is not null)
            {
                e.Handled = true;
            }

            handled = true;

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

        if (configManager.DisableHotkeys || handled)
        {
            return;
        }

        if (keyGesture.IsEqual(configManager.MiningModeKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                return;
            }

            EnableMiningMode();
            DisplayResults();

            if (configManager.Focusable)
            {
                _ = Activate();
            }

            _ = Focus();

            if (configManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }
        }

        else if (keyGesture.IsEqual(configManager.PlayAudioKeyGesture))
        {
            handled = true;

            await PlayAudio().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(configManager.ClosePopupKeyGesture))
        {
            handled = true;

            HidePopup();
        }

        else if (keyGesture.IsEqual(configManager.ShowAddNameWindowKeyGesture))
        {
            handled = true;

            if (DictUtils.SingleDictTypeDicts[DictType.CustomNameDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomNameDictionary].Ready)
            {
                if (!MiningMode)
                {
                    if (Owner is PopupWindow previousPopupWindow)
                    {
                        previousPopupWindow.ShowAddNameWindow();
                    }

                    else
                    {
                        mainWindow.ShowAddNameWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddNameWindow();
                }

                PopupWindowUtils.PopupAutoHideTimer.Start();
            }
        }

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
        {
            handled = true;

            if (DictUtils.SingleDictTypeDicts[DictType.CustomWordDictionary].Ready && DictUtils.SingleDictTypeDicts[DictType.ProfileCustomWordDictionary].Ready)
            {
                if (!MiningMode)
                {
                    if (Owner is PopupWindow previousPopupWindow)
                    {
                        previousPopupWindow.ShowAddWordWindow();
                    }

                    else
                    {
                        mainWindow.ShowAddWordWindow();
                    }

                    HidePopup();
                }

                else
                {
                    ShowAddWordWindow();
                }

                PopupWindowUtils.PopupAutoHideTimer.Start();
            }
        }

        else if (keyGesture.IsEqual(configManager.SearchWithBrowserKeyGesture))
        {
            handled = true;

            if (!MiningMode)
            {
                if (Owner is PopupWindow previousPopupWindow)
                {
                    previousPopupWindow.SearchWithBrowser();
                }

                else
                {
                    mainWindow.SearchWithBrowser();
                }

                HidePopup();
            }

            else
            {
                SearchWithBrowser();
            }
        }

        else if (keyGesture.IsEqual(configManager.InactiveLookupModeKeyGesture))
        {
            handled = true;

            configManager.InactiveLookupMode = !configManager.InactiveLookupMode;
        }

        else if (keyGesture.IsEqual(configManager.MotivationKeyGesture))
        {
            handled = true;

            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(configManager.NextDictKeyGesture))
        {
            handled = true;

            bool foundSelectedButton = false;

            Button? nextButton = null;
            int buttonCount = ItemsControlButtons.Items.Count;
            for (int i = 0; i < buttonCount; i++)
            {
                Button button = (Button)ItemsControlButtons.Items[i]!;

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

            ClickDictTypeButton(nextButton ?? _buttonAll);
        }

        else if (keyGesture.IsEqual(configManager.PreviousDictKeyGesture))
        {
            handled = true;

            bool foundSelectedButton = false;
            Button? previousButton = null;

            int dictCount = ItemsControlButtons.Items.Count;
            for (int i = dictCount - 1; i >= 0; i--)
            {
                Button button = (Button)ItemsControlButtons.Items[i]!;

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
                    Button btn = (Button)ItemsControlButtons.Items[i]!;
                    if (btn.IsEnabled)
                    {
                        ClickDictTypeButton(btn);
                        break;
                    }
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            handled = true;

            PopupWindowUtils.HidePopups(mainWindow.FirstPopupWindow);

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
            handled = true;

            if (MiningMode
                && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string text = _lastInteractedTextBox?.SelectionLength > 0
                    ? _lastInteractedTextBox.SelectedText
                    : LastLookupResults[_listViewItemIndex].PrimarySpelling;

                await SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, text).ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectNextLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                SelectNextLookupResult();
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectPreviousLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                SelectPreviousLookupResult();
            }
        }

        else if (keyGesture.IsEqual(configManager.MineSelectedLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode && PopupListView.SelectedItem is not null)
            {
                int index = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
                TextBox? definitionsTextBox = GetDefinitionTextBox(index);
                string? formattedDefinitions = definitionsTextBox?.Text;
                string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

                HidePopup();

                LookupResult lookupResult = LastLookupResults[index];
                if (configManager.MineToFileInsteadOfAnki)
                {
                    await MiningUtils.MineToFile(LastLookupResults, index, _currentSourceText, formattedDefinitions, selectedDefinitions, _currentSourceTextCharPosition, lookupResult.PrimarySpelling).ConfigureAwait(false);
                }
                else
                {
                    await MiningUtils.Mine(LastLookupResults, index, _currentSourceText, formattedDefinitions, selectedDefinitions, _currentSourceTextCharPosition, lookupResult.PrimarySpelling).ConfigureAwait(false);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            handled = true;

            configManager.AlwaysShowMainTextBoxCaret = !configManager.AlwaysShowMainTextBoxCaret;
            mainWindow.MainTextBox.IsReadOnlyCaretVisible = configManager.AlwaysShowMainTextBoxCaret;
        }

        else if (keyGesture.IsEqual(configManager.LookupSelectedTextKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                if (_lastInteractedTextBox?.SelectionLength > 0)
                {
                    ChildPopupWindow ??= new PopupWindow
                    {
                        Owner = this
                    };

                    await ChildPopupWindow.LookupOnSelect(_lastInteractedTextBox).ConfigureAwait(false);
                }
                else
                {
                    UpdatePosition();
                }
            }

            else if (Owner is PopupWindow previousPopupWindow)
            {
                if (previousPopupWindow._lastInteractedTextBox?.SelectionLength > 0)
                {
                    await LookupOnSelect(previousPopupWindow._lastInteractedTextBox).ConfigureAwait(false);
                }
            }

            else
            {
                await mainWindow.FirstPopupWindow.LookupOnSelect(mainWindow.MainTextBox).ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            handled = true;

            HidePopup();
        }

        if (handled && e is not null)
        {
            e.Handled = true;
        }
    }

    private void EnableMiningMode()
    {
        MiningMode = true;

        TitleBarGrid.Visibility = Visibility.Visible;
        if (ConfigManager.Instance.ShowMiningModeReminder && this == MainWindow.Instance.FirstPopupWindow)
        {
            TextBlockMiningModeReminder.Visibility = Visibility.Visible;
        }

        ItemsControlButtons.Visibility = Visibility.Visible;
    }

    private Task PlayAudio()
    {
        if (LastLookupResults.Length is 0)
        {
            return Task.CompletedTask;
        }

        LookupResult lastLookupResult = LastLookupResults[_listViewItemIndex];
        string primarySpelling = lastLookupResult.PrimarySpelling;
        string? reading = lastLookupResult.Readings?[0];

        return PopupWindowUtils.PlayAudio(primarySpelling, reading);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { LookupOnSelectOnly: false, LookupOnMouseClickOnly: false }
            && ChildPopupWindow is { IsVisible: true, MiningMode: false })
        {
            ChildPopupWindow.HidePopup();
        }

        if (MiningMode)
        {
            if (!ChildPopupWindow?.IsVisible ?? true)
            {
                PopupWindowUtils.PopupAutoHideTimer.Stop();
            }

            return;
        }

        if (UnavoidableMouseEnter
            || (configManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow))
        {
            return;
        }

        HidePopup();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _lastInteractedTextBox = (TextBox)sender;
        LastSelectedText = _lastInteractedTextBox.SelectedText;

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            || ((!configManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!configManager.LookupOnMouseClickOnly || e.ChangedButton != configManager.LookupOnClickMouseButton)))
        {
            return;
        }

        ChildPopupWindow ??= new PopupWindow
        {
            Owner = this
        };

        if (configManager.LookupOnSelectOnly)
        {
            await ChildPopupWindow.LookupOnSelect((TextBox)sender).ConfigureAwait(false);
        }

        else
        {
            await ChildPopupWindow.LookupOnMouseMoveOrClick((TextBox)sender, true).ConfigureAwait(false);
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
            if (ConfigManager.Instance.AutoHidePopupIfMouseIsNotOverIt)
            {
                if (PopupContextMenu.IsVisible
                    || ReadingSelectionWindow.IsItVisible()
                    || MiningSelectionWindow.IsItVisible()
                    || AddWordWindow.IsItVisible()
                    || AddNameWindow.IsItVisible())
                {
                    PopupWindowUtils.PopupAutoHideTimer.Stop();
                }

                else if (!ChildPopupWindow?.IsVisible ?? true)
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
        _buttonAll.Background = Brushes.DodgerBlue;
        _buttonAll.Click += DictTypeButtonOnClick;
        buttons.Add(_buttonAll);

        foreach (Dict dict in DictUtils.Dicts.Values.OrderBy(static dict => dict.Priority).ToArray())
        {
            if (!dict.Active || dict.Type is DictType.PitchAccentYomichan || (ConfigManager.Instance.HideDictTabsWithNoResults && !_dictsWithResults.Contains(dict)))
            {
                continue;
            }

            Button button = new()
            {
                Content = dict.Name,
                Margin = new Thickness(1),
                Tag = dict
            };
            button.Click += DictTypeButtonOnClick;

            if (!_dictsWithResults.Contains(dict))
            {
                button.IsEnabled = false;
            }

            buttons.Add(button);
        }

        ItemsControlButtons.ItemsSource = buttons;
    }

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        ClickDictTypeButton((Button)sender);
    }

    private void ClickDictTypeButton(Button button)
    {
        foreach (Button btn in ItemsControlButtons.Items.Cast<Button>())
        {
            btn.ClearValue(BackgroundProperty);
        }

        button.Background = Brushes.DodgerBlue;

        bool isAllButton = button == _buttonAll;
        if (isAllButton)
        {
            PopupListView.Items.Filter = PopupWindowUtils.NoAllDictFilter;
        }

        else
        {
            _filteredDict = (Dict)button.Tag;
            PopupListView.Items.Filter = DictFilter;
        }

        _popupListViewScrollViewer!.ScrollToTop();
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

    private void PopupContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        bool contextMenuBecameInvisible = !(bool)e.NewValue;
        if (contextMenuBecameInvisible)
        {
            _listViewItemIndex = _listViewItemIndexAfterContextMenuIsClosed;
            LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;

            if (!IsMouseOver
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
        if (ChildPopupWindow is not null
            && e.ChangedButton is not MouseButton.Right
            && e.ChangedButton != configManager.MiningModeMouseButton)
        {
            PopupWindowUtils.HidePopups(ChildPopupWindow);
        }
        else if (e.ChangedButton == configManager.MiningModeMouseButton)
        {
            if (!MiningMode)
            {
                ShowMiningModeResults();
            }

            else if (ChildPopupWindow is { IsVisible: true, MiningMode: false })
            {
                ChildPopupWindow.ShowMiningModeResults();
            }
        }
    }

    public void HidePopup()
    {
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopup = this == mainWindow.FirstPopupWindow;

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

        if (isFirstPopup && ChildPopupWindow is not null)
        {
            ChildPopupWindow.Close();
            ChildPopupWindow = null;
        }

        MiningMode = false;
        TitleBarGrid.Visibility = Visibility.Collapsed;
        TextBlockMiningModeReminder.Visibility = Visibility.Collapsed;
        ItemsControlButtons.Visibility = Visibility.Collapsed;
        ItemsControlButtons.ItemsSource = null;
        _buttonAll.Click -= DictTypeButtonOnClick;

        if (_popupListViewScrollViewer is not null)
        {
            _popupListViewScrollViewer.ScrollToTop();
            PopupListView.UpdateLayout();
        }

        PopupListView.ItemsSource = null;
        _lastLookedUpText = "";
        _listViewItemIndex = 0;
        _firstVisibleListViewItemIndex = 0;
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
        }

        else
        {
            PopupWindow previousPopup = (PopupWindow)Owner;
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
        PopupWindowUtils.HidePopups(ChildPopupWindow);
        _contextMenuIsOpening = false;
    }

    private void PopupListView_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!PopupContextMenu.IsVisible)
        {
            _listViewItemIndex = _firstVisibleListViewItemIndex;
            LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
        }
    }

    private TextBox? GetDefinitionTextBox(int listViewIndex)
    {
        PopupListView.Items.Filter = null;
        return ((StackPanel)((StackPanel)PopupListView.Items[listViewIndex]!).Children[1]).GetChildByName<TextBox>(nameof(LookupResult.FormattedDefinitions));
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
            _ = Activate();
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
        if ((ChildPopupWindow?.IsVisible ?? false)
            || ReadingSelectionWindow.IsItVisible()
            || MiningSelectionWindow.IsItVisible())
        {
            e.Handled = true;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Owner = null;
        ChildPopupWindow = null;
        _previousTextBox = null;
        _lastInteractedTextBox = null;
        LastSelectedText = null;
        _lastLookedUpText = null;
        _filteredDict = null;
        _popupListViewScrollViewer = null;
        ItemsControlButtons.ItemsSource = null;
        PopupListView.ItemsSource = null;
        _lastInteractedTextBox = null;
        LastLookupResults = null!;
        _dictsWithResults = null!;
        _currentSourceText = null!;
        _buttonAll.Click -= DictTypeButtonOnClick;
        _buttonAll = null!;
    }
}
