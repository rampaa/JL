using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.External;
using JL.Core.Lookup;
using JL.Core.Mining;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Config;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;
using Timer = System.Timers.Timer;

namespace JL.Windows.GUI.Popup;

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
    private Timer? _lookupDelayTimer;
    private int _lastCharPosition = -1;

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

    public ContextMenu DefinitionsTextBoxContextMenu { get; } = new();

    public int PopupIndex { get; }

    private static readonly MainWindow s_mainWindow = MainWindow.Instance;

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
        WinApi.SetNoRedirectionBitmapStyle(WindowHandle);
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

        InitLookupDelayTimer(configManager.PopupLookupDelay);
    }

    private void AddMenuItemsToEditableTextBoxContextMenu()
    {
        ConfigManager configManger = ConfigManager.Instance;

        MenuItem addNameMenuItem = new()
        {
            Name = "AddNameMenuItem",
            Header = "Add name"
        };
        addNameMenuItem.Click += AddName;
        _ = DefinitionsTextBoxContextMenu.Items.Add(addNameMenuItem);

        MenuItem addWordMenuItem = new()
        {
            Name = "AddWordMenuItem",
            Header = "Add word"
        };
        addWordMenuItem.Click += AddWord;
        _ = DefinitionsTextBoxContextMenu.Items.Add(addWordMenuItem);

        MenuItem copyMenuItem = new()
        {
            Header = "Copy",
            Command = ApplicationCommands.Copy
        };
        _ = DefinitionsTextBoxContextMenu.Items.Add(copyMenuItem);

        MenuItem cutMenuItem = new()
        {
            Header = "Cut",
            Command = ApplicationCommands.Cut
        };
        _ = DefinitionsTextBoxContextMenu.Items.Add(cutMenuItem);

        MenuItem deleteMenuItem = new()
        {
            Name = "DeleteMenuItem",
            Header = "Delete",
            InputGestureText = "Backspace"
        };
        deleteMenuItem.Click += PressBackSpace;
        _ = DefinitionsTextBoxContextMenu.Items.Add(deleteMenuItem);

        MenuItem enableEditingButton = new()
        {
            Name = "EditableMenuItem",
            Header = "Enable editing",
            InputGestureText = "Ins + Left button",
            IsCheckable = true
        };
        enableEditingButton.Click += (_, _) =>
        {
            TextBox definitionTextBox = (TextBox)DefinitionsTextBoxContextMenu.PlacementTarget;
            definitionTextBox.SetIsReadOnly(!definitionTextBox.IsReadOnly);
        };
        _ = DefinitionsTextBoxContextMenu.Items.Add(enableEditingButton);

        MenuItem searchMenuItem = new()
        {
            Name = "SearchMenuItem",
            Header = "Search"
        };

        searchMenuItem.Click += SearchWithBrowser;
        searchMenuItem.SetInputGestureText(configManger.SearchWithBrowserKeyGesture);
        _ = DefinitionsTextBoxContextMenu.Items.Add(searchMenuItem);

        DefinitionsTextBoxContextMenu.Opened += (_, _) =>
        {
            HandleContextMenuOpening();

            TextBox definitionTextBox = (TextBox)DefinitionsTextBoxContextMenu.PlacementTarget;
            enableEditingButton.IsChecked = !definitionTextBox.IsReadOnly;
            deleteMenuItem.IsEnabled = !definitionTextBox.IsReadOnly;
            ConfigManager configManger = ConfigManager.Instance;
            addNameMenuItem.SetInputGestureText(configManger.ShowAddNameWindowKeyGesture);
            addWordMenuItem.SetInputGestureText(configManger.ShowAddWordWindowKeyGesture);
        };
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

    public Task LookupOnCharPosition(TextBox textBox, int charPosition, bool enableMiningMode, bool mayNeedCoordinateConversion)
    {
        string textBoxText = textBox.Text;

        _currentSourceText = textBoxText;

        if (char.IsLowSurrogate(textBox.Text[charPosition]))
        {
            --charPosition;
        }

        _currentSourceTextCharPosition = charPosition;

        ConfigManager configManager = ConfigManager.Instance;
        bool isFirstPopupWindow = PopupIndex is 0;
        if (isFirstPopupWindow ? configManager.DisableLookupsForNonJapaneseCharsInMainWindow : configManager.DisableLookupsForNonJapaneseCharsInPopups)
        {
            char firstChar = textBoxText[charPosition];
            Debug.Assert(!char.IsHighSurrogate(firstChar) || textBoxText.Length - charPosition > 1);
            if (char.IsHighSurrogate(firstChar)
                ? !JapaneseUtils.ContainsJapaneseCharacters(textBoxText.AsSpan(charPosition, 2))
                : !JapaneseUtils.ContainsJapaneseCharacters(firstChar))
            {
                if (isFirstPopupWindow)
                {
                    if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                    {
                        s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                    }

                    HidePopup();
                    s_mainWindow.ChangeVisibility();
                }
                else
                {
                    HidePopup();
                }

                return Task.CompletedTask;
            }
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
            if (isFirstPopupWindow)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }

            return Task.CompletedTask;
        }

        if (textToLookUp == _lastLookedUpText && IsVisible)
        {
            UpdatePosition(mayNeedCoordinateConversion);
            WinApi.BringToFront(WindowHandle);
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
                    ? s_mainWindow.WindowHandle
                    : PopupWindowUtils.PopupWindows[PopupIndex - 1]!.WindowHandle);

                _ = textBox.Focus();
                textBox.Select(charPosition, lookupResults[0].MatchedText.Length);
            }

            LastLookupResults = lookupResults;

            Show();

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

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            UpdatePosition(mayNeedCoordinateConversion);

            Opacity = 1d;

            if (configManager.Focusable
                && (enableMiningMode || configManager.PopupFocusOnLookup))
            {
                if (configManager.RestoreFocusToPreviouslyActiveWindow && isFirstPopupWindow)
                {
                    nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                    if (previousWindowHandle != s_mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
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
            if (isFirstPopupWindow)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }
        }

        return Task.CompletedTask;
    }

    public Task LookupOnMouseMoveOrClick(TextBox textBox, bool enableMiningMode)
    {
        int charPosition = textBox.GetCharacterIndexFromPoint(Mouse.GetPosition(textBox), false);
        if (charPosition < 0)
        {
            if (PopupIndex is 0)
            {
                if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }

            return Task.CompletedTask;
        }

        return LookupOnCharPosition(textBox, charPosition, enableMiningMode, false);
    }

    public Task LookupOnSelect(TextBox textBox)
    {
        _currentSourceText = textBox.Text;
        _currentSourceTextCharPosition = textBox.SelectionStart;

        string selectedText = textBox.SelectedText;
        ConfigManager configManager = ConfigManager.Instance;
        if (string.IsNullOrEmpty(selectedText) || TextUtils.StartsWithWhiteSpace(selectedText))
        {
            if (PopupIndex is 0)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }

            return Task.CompletedTask;
        }

        if (selectedText == _lastLookedUpText && IsVisible)
        {
            UpdatePosition(false);
            WinApi.BringToFront(WindowHandle);
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

            Show();

            EnableMiningMode();
            DisplayResults();

            if (configManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            UpdatePosition(false);

            Opacity = 1d;

            _ = textBox.Focus();

            if (configManager.Focusable)
            {
                if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
                {
                    nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                    if (previousWindowHandle != s_mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
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
            if (PopupIndex is 0)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }
        }

        return Task.CompletedTask;
    }

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X;
        double mouseY = cursorPosition.Y;

        ConfigManager configManager = ConfigManager.Instance;

        DpiScale dpi = WindowsUtils.Dpi;
        double dpiAwareXOffset = WindowsUtils.DpiAwareXOffset;
        double dpiAwareYOffset = WindowsUtils.DpiAwareYOffset;
        Rectangle screenBounds = WindowsUtils.ActiveScreen.Bounds;

        double currentWidth = ActualWidth * dpi.DpiScaleX;
        double currentHeight = ActualHeight * dpi.DpiScaleY;

        double newLeft = configManager.PositionPopupLeftOfCursor
            ? mouseX - (currentWidth + dpiAwareXOffset)
            : mouseX + dpiAwareXOffset;

        double newTop = configManager.PositionPopupAboveCursor
            ? mouseY - (currentHeight + dpiAwareYOffset)
            : mouseY + dpiAwareYOffset;

        if (configManager.PopupFlipX)
        {
            if (configManager.PositionPopupLeftOfCursor && newLeft < screenBounds.Left)
            {
                newLeft = mouseX + dpiAwareXOffset;
            }
            else if (!configManager.PositionPopupLeftOfCursor && newLeft + currentWidth > screenBounds.Right)
            {
                newLeft = mouseX - (currentWidth + dpiAwareXOffset);
            }
        }

        newLeft = Math.Max(screenBounds.Left, Math.Min(newLeft, screenBounds.Right - currentWidth));

        if (configManager.PopupFlipY)
        {
            if (configManager.PositionPopupAboveCursor && newTop < screenBounds.Top)
            {
                newTop = mouseY + dpiAwareYOffset;
            }
            else if (!configManager.PositionPopupAboveCursor && newTop + currentHeight > screenBounds.Bottom)
            {
                newTop = mouseY - (currentHeight + dpiAwareYOffset);
            }
        }

        newTop = Math.Max(screenBounds.Top, Math.Min(newTop, screenBounds.Bottom - currentHeight));

        UnavoidableMouseEnter = mouseX >= newLeft
            && mouseX <= newLeft + currentWidth
            && mouseY >= newTop
            && mouseY <= newTop + currentHeight;

        WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);
    }

    private void UpdatePositionToFixedPosition(Point fixedPosition)
    {
        Screen activeScreen = WindowsUtils.ActiveScreen;
        ConfigManager configManager = ConfigManager.Instance;

        double x = fixedPosition.X;
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

        double y = fixedPosition.Y;
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

    private void UpdatePosition(bool mayNeedCoordinateConversion)
    {
        if (ConfigManager.Instance.FixedPopupPositioning && PopupIndex is 0)
        {
            ConfigManager configManager = ConfigManager.Instance;
            UpdatePositionToFixedPosition(WindowsUtils.GetMousePosition(new Point(configManager.FixedPopupXPosition, configManager.FixedPopupYPosition), mayNeedCoordinateConversion));
        }

        else
        {
            UpdatePosition(WindowsUtils.GetMousePosition(mayNeedCoordinateConversion));
        }
    }

    private void DisplayResults()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        _dictsWithResults.Clear();

        PopupListView.Items.Filter = PopupWindowUtils.NoAllDictFilter;

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

        LookupDisplayResult[] popupItemSource = new LookupDisplayResult[resultCount];

        for (int i = 0; i < resultCount; i++)
        {
            LookupResult lookupResult = LastLookupResults[i];

            if (!_dictsWithResults.Contains(lookupResult.Dict))
            {
                _dictsWithResults.Add(lookupResult.Dict);
            }

            popupItemSource[i] = new LookupDisplayResult(this, lookupResult, i, resultCount - 1 > i);
        }

        PopupListView.ItemsSource = popupItemSource;

        if (checkForDuplicateCards)
        {
            Debug.Assert(duplicateIcons is not null);
            CheckResultForDuplicates((LookupDisplayResult[])PopupListView.ItemsSource).SafeFireAndForget("Unexpected error while checking results for duplicates");
        }

        GenerateDictTypeButtons();

        UpdateLayout();
    }

    public void AddEventHandlersToTextBox(TextBox textBox)
    {
        textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
    }

    public void AddEventHandlersToDefinitionsTextBox(TextBox textBox)
    {
        textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += DefinitionsTextBox_PreviewMouseLeftButtonDown;
    }

    public void AddEventHandlersToPrimarySpellingTextBox(FrameworkElement textBox)
    {
        textBox.PreviewMouseUp += PrimarySpellingTextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
    }

    private async Task CheckResultForDuplicates(LookupDisplayResult[] lookupDisplayResults)
    {
        LookupResult[] lastLookupResults = LastLookupResults;
        bool[]? duplicateCard = await MiningUtils.CheckDuplicates(lastLookupResults, _currentSourceText, _currentSourceTextCharPosition).ConfigureAwait(true);
        if (duplicateCard is not null)
        {
            Debug.Assert(lookupDisplayResults.Length == duplicateCard.Length);
            for (int i = 0; i < duplicateCard.Length; i++)
            {
                if (duplicateCard[i])
                {
                    LookupDisplayResult item = lookupDisplayResults[i];
                    item.IsDuplicate = true;

                    ListViewItem? container = (ListViewItem?)PopupListView.ItemContainerGenerator.ContainerFromItem(item);
                    Button? miningButton = container?.GetChildByName<Button>("MiningButton");
                    if (miningButton is not null)
                    {
                        miningButton.Foreground = Brushes.OrangeRed;
                        miningButton.ToolTip = "Duplicate note";
                    }
                }
            }
        }
    }

    private int GetFirstVisibleListViewItemIndex()
    {
        ItemContainerGenerator generator = PopupListView.ItemContainerGenerator;
        ReadOnlyCollection<object> items = generator.Items;
        foreach (LookupDisplayResult item in items.Cast<LookupDisplayResult>())
        {
            if (generator.ContainerFromItem(item) is ListViewItem)
            {
                return item.Index;
            }
        }
        return 0;
    }

    public void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
        StackPanel stackPanel = (StackPanel)sender;
        if (PopupContextMenu.IsVisible
            || DefinitionsTextBoxContextMenu.IsVisible
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

        definitionsTextBox.SetIsReadOnly(!definitionsTextBox.IsReadOnly);
    }

    private Task HandleTextBoxMouseMove(TextBox textBox, MouseEventArgs e)
    {
        if (!MiningMode)
        {
            return Task.CompletedTask;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || configManager.LookupOnSelectOnly
            || configManager.LookupOnMouseClickOnly
            || e.LeftButton is MouseButtonState.Pressed
            || PopupContextMenu.IsVisible
            || DefinitionsTextBoxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || DictTabButtonsItemsControlContextMenu.IsVisible
            || ReadingSelectionWindow.IsItVisible()
            || MiningSelectionWindow.IsItVisible()
            || (configManager.RequireLookupKeyPress
                && !configManager.LookupKeyKeyGesture.IsPressed()))
        {
            return Task.CompletedTask;
        }

        if (configManager.DisableLookupsForNonJapaneseCharsInPopups && !JapaneseUtils.ContainsJapaneseCharacters(textBox.Text))
        {
            if (configManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(PopupWindowUtils.PopupWindows[PopupIndex + 1]?._previousTextBox);
            }

            return Task.CompletedTask;
        }

        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        if (childPopupWindow?.MiningMode ?? false)
        {
            return Task.CompletedTask;
        }

        _lastInteractedTextBox = textBox;
        if (_lookupDelayTimer is null)
        {
            if (childPopupWindow is null)
            {
                childPopupWindow = new PopupWindow(PopupIndex + 1)
                {
                    Owner = this
                };

                PopupWindowUtils.PopupWindows[PopupIndex + 1] = childPopupWindow;
            }

            return childPopupWindow.LookupOnMouseMoveOrClick(textBox, false);
        }

        InitDelayedLookup(textBox, childPopupWindow);
        return Task.CompletedTask;
    }

    public void InitLookupDelayTimer(int delayInMilliseconds)
    {
        if (delayInMilliseconds is 0)
        {
            if (_lookupDelayTimer is not null)
            {
                _lookupDelayTimer.Stop();
                _lookupDelayTimer.Dispose();
                _lookupDelayTimer = null;
            }
        }
        else
        {
            _lookupDelayTimer ??= new Timer()
            {
                AutoReset = false
            };

            _lookupDelayTimer.Elapsed += LookupDelayTimer_Elapsed;
            _lookupDelayTimer.Interval = delayInMilliseconds;
            _lookupDelayTimer.Enabled = true;
        }
    }

    private void InitDelayedLookup(TextBox textBox, PopupWindow? childPopupWindow)
    {
        Debug.Assert(_lookupDelayTimer is not null);

        int charPosition = textBox.GetCharacterIndexFromPoint(Mouse.GetPosition(textBox), false);
        if (charPosition < 0)
        {
            _lookupDelayTimer.Stop();
            _lastCharPosition = charPosition;
            childPopupWindow?.HidePopup();
            return;
        }

        if (char.IsLowSurrogate(textBox.Text[charPosition]))
        {
            --charPosition;
        }

        if (charPosition != _lastCharPosition)
        {
            _lookupDelayTimer.Stop();
            _lastCharPosition = charPosition;
            _lookupDelayTimer.Start();
        }
        else if (!childPopupWindow?.IsVisible ?? true)
        {
            _lookupDelayTimer.Start();
        }
    }

    private void LookupDelayTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_lastInteractedTextBox is not null)
        {
            Dispatcher.Invoke(HandleDelayedLookup);
        }
    }

    private void HandleDelayedLookup()
    {
        Debug.Assert(_lastInteractedTextBox is not null);

        int charPosition = _lastInteractedTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(_lastInteractedTextBox), false);
        if (charPosition < 0)
        {
            _lastCharPosition = charPosition;
            return;
        }

        if (char.IsLowSurrogate(_lastInteractedTextBox.Text[charPosition]))
        {
            --charPosition;
        }

        if (charPosition == _lastCharPosition)
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

            childPopupWindow.LookupOnMouseMoveOrClick(_lastInteractedTextBox, false).SafeFireAndForget("LookupOnMouseMoveOrClick failed unexpectedly");
        }
        else
        {
            _lastCharPosition = charPosition;
        }
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
    public async void AudioButton_Click(object sender, MouseButtonEventArgs e)
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

        ListViewItem? popupListViewItem = null;
        int listViewItemIndex;
        if (useSelectedListViewItemIfItExists && PopupListView.SelectedItem is not null)
        {
            popupListViewItem = (ListViewItem?)PopupListView.ItemContainerGenerator.ContainerFromItem(PopupListView.SelectedItem);
            Debug.Assert(popupListViewItem is not null);

            listViewItemIndex = ((LookupDisplayResult)PopupListView.SelectedItem).Index;
        }
        else
        {
            listViewItemIndex = _listViewItemIndex;

            ItemContainerGenerator generator = PopupListView.ItemContainerGenerator;
            ReadOnlyCollection<object> items = generator.Items;
            foreach (LookupDisplayResult item in items.Cast<LookupDisplayResult>())
            {
                if (item.Index == listViewItemIndex)
                {
                    popupListViewItem = (ListViewItem)generator.ContainerFromItem(item);
                    break;
                }
            }
        }

        LookupResult lookupResult = LastLookupResults[listViewItemIndex];
        if (lookupResult.Readings is null || lookupResult.Readings.Length is 1)
        {
            return PopupWindowUtils.PlayAudio(lookupResult.PrimarySpelling, lookupResult.Readings?[0]);
        }

        Point position;
        if (useSelectedListViewItemIfItExists)
        {
            Debug.Assert(popupListViewItem is not null);
            Button? audioButton = popupListViewItem.GetChildByName<Button>("AudioButton");
            if (audioButton is not null)
            {
                position = audioButton.PointToScreen(default);
                position.Y += 7;
                position.X += 7;
            }
            else
            {
                position = WindowsUtils.GetMousePosition(true);
            }
        }
        else
        {
            position = WindowsUtils.GetMousePosition(false);
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

        ListViewItem? popupListViewItem = null;
        int listViewItemIndex;
        if (useSelectedListViewItemIfItExists && PopupListView.SelectedItem is not null)
        {
            popupListViewItem = (ListViewItem)PopupListView.ItemContainerGenerator.ContainerFromItem(PopupListView.SelectedItem);
            listViewItemIndex = ((LookupDisplayResult)PopupListView.SelectedItem).Index;
        }
        else
        {
            listViewItemIndex = _listViewItemIndex;

            ItemContainerGenerator generator = PopupListView.ItemContainerGenerator;
            ReadOnlyCollection<object> items = generator.Items;
            foreach (LookupDisplayResult item in items.Cast<LookupDisplayResult>())
            {
                if (item.Index == listViewItemIndex)
                {
                    popupListViewItem = (ListViewItem)generator.ContainerFromItem(item);
                    break;
                }
            }
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

            ConfigManager configManager = ConfigManager.Instance;
            if (PopupIndex is 0)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }

            return configManager.MineToFileInsteadOfAnki
                ? MiningUtils.MineToFile(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling)
                : MiningUtils.Mine(lookupResults, listViewItemIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, lookupResult.PrimarySpelling);
        }

        Point position;
        if (useSelectedListViewItemIfItExists)
        {
            Debug.Assert(popupListViewItem is not null);
            Button? miningButton = popupListViewItem.GetChildByName<Button>("MiningButton");
            if (miningButton is not null)
            {
                position = miningButton.PointToScreen(default);
                position.Y += 5;
                position.X += 7;
            }
            else
            {
                position = WindowsUtils.GetMousePosition(true);
            }
        }
        else
        {
            position = WindowsUtils.GetMousePosition(false);
        }

        MiningSelectionWindow.Show(this, lookupResults, listViewItemIndex, currentSourceText, currentSourceTextCharPosition, position);
        return Task.CompletedTask;
    }

    // ReSharper disable once AsyncVoidMethod
    public async void MiningButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
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
        if (e.OriginalSource is not TextBox textBox || textBox.IsReadOnly)
        {
            e.Handled = true;
        }

        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    public Task HandleHotKey(KeyGesture keyGesture)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (keyGesture.IsEqual(configManager.DisableHotkeysKeyGesture))
        {
            s_mainWindow.HandleDisableHotkeysToggle();
        }

        else if (keyGesture.IsEqual(configManager.MiningModeKeyGesture))
        {
            HandleMiningModeKeyGesture();
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
            HandleClosePopupKeyGesture();
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
            HandleShowAddNameWindowKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
        {
            HandleShowAddWordWindowKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.SearchWithBrowserKeyGesture))
        {
            HandleSearchWithBrowserKeyGesture();
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
            HandleNextDictKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.PreviousDictKeyGesture))
        {
            HandlePreviousDictKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.ToggleVisibilityOfDictionaryTabsInMiningModeKeyGesture))
        {
            ToggleVisibilityOfDictTabs();
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            HandleToggleMinimizedStateKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.SelectedTextToSpeechKeyGesture))
        {
            return HandleSelectedTextToSpeechKeyGesture();
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
            s_mainWindow.MainTextBox.IsReadOnlyCaretVisible = configManager.AlwaysShowMainTextBoxCaret;
        }

        else if (keyGesture.IsEqual(configManager.LookupSelectedTextKeyGesture))
        {
            return HandleLookupSelectedTextKeyGesture();
        }

        else if (keyGesture.IsEqual(configManager.MousePassThroughModeKeyGesture))
        {
            s_mainWindow.HandlePassThroughKeyGesture();
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.CtrlCKeyGesture))
        {
            return HandleCtrlCKeyGesture();
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            HandleAltF4KeyGesture();
        }

        return Task.CompletedTask;
    }

    private void HandleMiningModeKeyGesture()
    {
        if (MiningMode)
        {
            return;
        }

        EnableMiningMode();
        DisplayResults();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.Focusable)
        {
            if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
            {
                nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                if (previousWindowHandle != s_mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
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

    private void HandleClosePopupKeyGesture()
    {
        if (PopupIndex is 0)
        {
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
            }

            HidePopup();
            s_mainWindow.ChangeVisibility();
        }
        else
        {
            HidePopup();
        }
    }

    private void HandleShowAddNameWindowKeyGesture()
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
                    s_mainWindow.ShowAddNameWindow();
                }

                if (PopupIndex is 0)
                {
                    if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
                    {
                        s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                    }

                    HidePopup();
                    s_mainWindow.ChangeVisibility();
                }
                else
                {
                    HidePopup();
                }
            }

            else
            {
                ShowAddNameWindow(false);
            }

            PopupWindowUtils.PopupAutoHideTimer.Start();
        }
    }

    private void HandleShowAddWordWindowKeyGesture()
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
                    s_mainWindow.ShowAddWordWindow();
                }

                if (PopupIndex is 0)
                {
                    if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
                    {
                        s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                    }

                    HidePopup();
                    s_mainWindow.ChangeVisibility();
                }
                else
                {
                    HidePopup();
                }
            }

            else
            {
                ShowAddWordWindow(false);
            }

            PopupWindowUtils.PopupAutoHideTimer.Start();
        }
    }

    private void HandleSearchWithBrowserKeyGesture()
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
                s_mainWindow.SearchWithBrowser();
            }

            if (PopupIndex is 0)
            {
                if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }
        }

        else
        {
            SearchWithBrowser(false);
        }
    }

    private void HandleNextDictKeyGesture()
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

    private void HandlePreviousDictKeyGesture()
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

    private Task HandleLookupCategoryKeyGesture(LookupCategory lookupCategory)
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.LookupCategory = coreConfigManager.LookupCategory == lookupCategory
            ? LookupCategory.All
            : lookupCategory;

        if (IsVisible && _previousTextBox is not null)
        {
            if (PopupIndex is 0)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }
            }

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

    private static void HandleToggleMinimizedStateKeyGesture()
    {
        PopupWindowUtils.HidePopups(0);

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.Focusable)
        {
            s_mainWindow.WindowState = s_mainWindow.WindowState is WindowState.Minimized
                ? WindowState.Normal
                : WindowState.Minimized;
        }

        else
        {
            if (s_mainWindow.WindowState is WindowState.Minimized)
            {
                WinApi.RestoreWindow(s_mainWindow.WindowHandle);
            }

            else
            {
                WinApi.MinimizeWindow(s_mainWindow.WindowHandle);

                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
                }
            }
        }
    }

    private Task HandleSelectedTextToSpeechKeyGesture()
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

        return Task.CompletedTask;
    }

    private Task HandleLookupSelectedTextKeyGesture()
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
            return s_mainWindow.FirstPopupWindow.LookupOnSelect(s_mainWindow.MainTextBox);
        }

        return Task.CompletedTask;
    }

    private Task HandleCtrlCKeyGesture()
    {
        string? textToCopy = _lastInteractedTextBox?.SelectedText;
        if (string.IsNullOrEmpty(textToCopy))
        {
            textToCopy = _previousTextBox?.SelectedText;
        }

        return !string.IsNullOrEmpty(textToCopy)
            ? WindowsUtils.CopyTextToClipboard(textToCopy)
            : Task.CompletedTask;
    }

    private void HandleAltF4KeyGesture()
    {
        if (PopupIndex is 0)
        {
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
            }

            HidePopup();
            s_mainWindow.ChangeVisibility();
        }
        else
        {
            HidePopup();
        }
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

        if (PopupIndex is 0)
        {
            if (configManager.AutoPauseOrResumeMpvOnHoverChange)
            {
                s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
            }

            HidePopup();
            s_mainWindow.ChangeVisibility();
        }
        else
        {
            HidePopup();
        }
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
                    || DefinitionsTextBoxContextMenu.IsVisible
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
            if (PopupIndex is 0)
            {
                TextBox mainTextBox = s_mainWindow.MainTextBox;
                bool isMouseOverMainTextBox = s_mainWindow.IsMouseOver;
                if (isMouseOverMainTextBox)
                {
                    int charPosition = mainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(mainTextBox), false);
                    if (charPosition >= 0)
                    {
                        return;
                    }
                }

                if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
                {
                    s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
                }

                HidePopup();
                s_mainWindow.ChangeVisibility();
            }
            else
            {
                HidePopup();
            }
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

        if (PopupListView.Items.Count > 0)
        {
            object? firstItem = PopupListView.Items[0];
            Debug.Assert(firstItem is LookupDisplayResult);
            PopupListView.ScrollIntoView(firstItem);
        }

        UpdateLayout();

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
        LookupDisplayResult items = (LookupDisplayResult)item;
        return items.LookupResult.Dict == _filteredDict;
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
                && !DefinitionsTextBoxContextMenu.IsVisible
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
        if (PopupIndex is 0)
        {
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
            }

            HidePopup();
            s_mainWindow.ChangeVisibility();
        }
        else
        {
            HidePopup();
        }
    }

    public void HidePopup()
    {
        if (!IsVisible)
        {
            return;
        }

        ReadingSelectionWindow.HideWindow();
        MiningSelectionWindow.CloseWindow();

        PopupWindow? childPopupWindow = PopupWindowUtils.PopupWindows[PopupIndex + 1];
        bool isFirstPopup = PopupIndex is 0;
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

        Opacity = 0d;
        UpdateLayout();
        Hide();

        if (AddNameWindow.IsItVisible() || AddWordWindow.IsItVisible())
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (isFirstPopup)
        {
            WinApi.ActivateWindow(s_mainWindow.WindowHandle);

            if (configManager.HighlightLongestMatch && !s_mainWindow.ContextMenuIsOpening)
            {
                WindowsUtils.Unselect(_previousTextBox);
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
        HandleContextMenuOpening();
    }

    private void HandleContextMenuOpening()
    {
        _contextMenuIsOpening = true;
        PopupWindowUtils.HidePopups(PopupIndex + 1);
        _contextMenuIsOpening = false;
    }

    private void PopupListView_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!PopupContextMenu.IsVisible
            && !DefinitionsTextBoxContextMenu.IsVisible
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
        ItemContainerGenerator generator = PopupListView.ItemContainerGenerator;
        ReadOnlyCollection<object> items = generator.Items;
        foreach (LookupDisplayResult lookupDisplayResult in items.Cast<LookupDisplayResult>())
        {
            if (lookupDisplayResult.Index == listViewIndex)
            {
                ListViewItem container = (ListViewItem)generator.ContainerFromItem(lookupDisplayResult);
                return container.GetChildByName<TextBox>(nameof(LookupResult.FormattedDefinitions));
            }
        }

        return null;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (PopupIndex is 0)
        {
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                s_mainWindow.MouseEnterDueToFirstPopupHide = s_mainWindow.IsMouseWithinWindowBounds();
            }

            HidePopup();
            s_mainWindow.ChangeVisibility();
        }
        else
        {
            HidePopup();
        }
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
        UpdatePosition(false);

        ConfigManager configManager = ConfigManager.Instance;

        if (configManager.Focusable)
        {
            if (configManager.RestoreFocusToPreviouslyActiveWindow && PopupIndex is 0)
            {
                nint previousWindowHandle = WinApi.GetActiveWindowHandle();
                if (previousWindowHandle != s_mainWindow.WindowHandle && previousWindowHandle != WindowHandle)
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
        LastLookupResults = [];
        _dictsWithResults.Clear();
        AllDictionaryTabButton.Click -= DictTypeButtonOnClick;

        if (_lookupDelayTimer is not null)
        {
            _lookupDelayTimer.Dispose();
            _lookupDelayTimer = null;
        }
    }
}
