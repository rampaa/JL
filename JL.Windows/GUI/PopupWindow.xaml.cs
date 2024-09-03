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

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal sealed partial class PopupWindow : Window
{
    public PopupWindow? ChildPopupWindow { get; private set; }

    private bool _contextMenuIsOpening; // = false;

    private TextBox? _previousTextBox;

    private TextBox? _lastInteractedTextBox;

    private int _listViewItemIndex; // 0

    private int _listViewItemIndexAfterContextMenuIsClosed; // 0

    private int _firstVisibleListViewItemIndex; // 0

    private int _currentCharPosition;

    private string _currentText = "";

    private Button _buttonAll = new()
    {
        Content = "All",
        Margin = new Thickness(1),
        Background = Brushes.DodgerBlue
    };

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    public List<LookupResult> LastLookupResults { get; private set; } = [];

    private List<Dict> _dictsWithResults = [];

    private Dict? _filteredDict;

    public bool UnavoidableMouseEnter { get; private set; } // = false;

    public string? LastText { get; set; }

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

        WindowsUtils.SetSizeToContent(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, ConfigManager.PopupMaxWidth, ConfigManager.PopupMaxHeight, this);

        AddNameMenuItem.SetInputGestureText(ConfigManager.ShowAddNameWindowKeyGesture);
        AddWordMenuItem.SetInputGestureText(ConfigManager.ShowAddWordWindowKeyGesture);
        SearchMenuItem.SetInputGestureText(ConfigManager.SearchWithBrowserKeyGesture);

        AddMenuItemsToEditableTextBoxContextMenu();

        if (ConfigManager.ShowMiningModeReminder)
        {
            TextBlockMiningModeReminder.Text = string.Create(CultureInfo.InvariantCulture,
                $"Click on an entry's main spelling to mine it,\nor press {ConfigManager.ClosePopupKeyGesture.ToFormattedString()} or click on the main window to exit.");
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

        _currentText = textBoxText;
        _currentCharPosition = charPosition;

        if (this != MainWindow.Instance.FirstPopupWindow
                ? ConfigManager.DisableLookupsForNonJapaneseCharsInPopups
                  && !JapaneseUtils.JapaneseRegex().IsMatch(textBoxText[charPosition].ToString())
                : ConfigManager.DisableLookupsForNonJapaneseCharsInMainWindow
                  && !JapaneseUtils.JapaneseRegex().IsMatch(textBoxText[charPosition].ToString()))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        string text = textBoxText.Length - charPosition > ConfigManager.MaxSearchLength
            ? textBoxText[..(charPosition + ConfigManager.MaxSearchLength)]
            : textBoxText;

        int endPosition = JapaneseUtils.FindExpressionBoundary(text, charPosition);
        text = text[charPosition..endPosition];

        if (string.IsNullOrEmpty(text))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        if (text == LastText && IsVisible)
        {
            if (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
            {
                UpdatePosition(ConfigManager.FixedPopupXPosition, ConfigManager.FixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
            }

            return Task.CompletedTask;
        }

        LastText = text;

        List<LookupResult>? lookupResults = LookupUtils.LookupText(text);

        if (lookupResults?.Count > 0)
        {
            Stats.IncrementStat(StatType.NumberOfLookups);
            _previousTextBox = textBox;
            LastSelectedText = lookupResults[0].MatchedText;

            if (ConfigManager.HighlightLongestMatch)
            {
                WinApi.ActivateWindow(this == MainWindow.Instance.FirstPopupWindow
                    ? MainWindow.Instance.WindowHandle
                    : ((PopupWindow)Owner).WindowHandle);

                _ = textBox.Focus();
                textBox.Select(charPosition, lookupResults[0].MatchedText.Length);
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
                DisplayResults(false);
            }

            Show();

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            if (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
            {
                UpdatePosition(ConfigManager.FixedPopupXPosition, ConfigManager.FixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
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
                return PlayAudio();
            }
        }
        else
        {
            HidePopup();
        }

        return Task.CompletedTask;
    }

    public Task LookupOnMouseMoveOrClick(TextBox textBox)
    {
        int charPosition = textBox.GetCharacterIndexFromPoint(Mouse.GetPosition(textBox), false);

        if (charPosition >= 0)
        {
            if (charPosition > 0 && char.IsHighSurrogate(textBox.Text[charPosition - 1]))
            {
                --charPosition;
            }

            return LookupOnCharPosition(textBox, charPosition, ConfigManager.LookupOnMouseClickOnly);
        }

        HidePopup();
        return Task.CompletedTask;
    }

    public Task LookupOnSelect(TextBox textBox)
    {
        string text = textBox.SelectedText;
        if (string.IsNullOrWhiteSpace(text))
        {
            HidePopup();
            return Task.CompletedTask;
        }

        int charPosition = textBox.SelectionStart;

        _currentText = text;
        _currentCharPosition = charPosition;

        if (text == LastText && IsVisible)
        {
            if (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
            {
                UpdatePosition(ConfigManager.FixedPopupXPosition, ConfigManager.FixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
            }

            return Task.CompletedTask;
        }

        LastText = text;

        List<LookupResult>? lookupResults = LookupUtils.LookupText(textBox.SelectedText);

        if (lookupResults?.Count > 0)
        {
            Stats.IncrementStat(StatType.NumberOfLookups);
            _previousTextBox = textBox;
            LastSelectedText = lookupResults[0].MatchedText;
            LastLookupResults = lookupResults;

            EnableMiningMode();
            DisplayResults(true);

            if (ConfigManager.AutoHidePopupIfMouseIsNotOverIt)
            {
                PopupWindowUtils.SetPopupAutoHideTimer();
            }

            Show();

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            if (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
            {
                UpdatePosition(ConfigManager.FixedPopupXPosition, ConfigManager.FixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
            }

            _ = textBox.Focus();

            if (ConfigManager.Focusable)
            {
                _ = Activate();
            }

            _ = Focus();

            WinApi.BringToFront(WindowHandle);

            if (ConfigManager.AutoPlayAudio)
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

        double currentWidth = ActualWidth * WindowsUtils.Dpi.DpiScaleX;
        double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;

        double newLeft = ConfigManager.PositionPopupLeftOfCursor
            ? mouseX - (currentWidth + WindowsUtils.DpiAwareXOffset)
            : mouseX + WindowsUtils.DpiAwareXOffset;

        double newTop = ConfigManager.PositionPopupAboveCursor
            ? mouseY - (currentHeight + WindowsUtils.DpiAwareYOffset)
            : mouseY + WindowsUtils.DpiAwareYOffset;

        Rectangle screenBounds = WindowsUtils.ActiveScreen.Bounds;

        if (ConfigManager.PopupFlipX)
        {
            if (ConfigManager.PositionPopupLeftOfCursor && newLeft < screenBounds.Left)
            {
                newLeft = mouseX + WindowsUtils.DpiAwareXOffset;
            }
            else if (!ConfigManager.PositionPopupLeftOfCursor && newLeft + currentWidth > screenBounds.Right)
            {
                newLeft = mouseX - (currentWidth + WindowsUtils.DpiAwareXOffset);
            }
        }

        newLeft = Math.Max(screenBounds.Left, Math.Min(newLeft, screenBounds.Right - currentWidth));

        if (ConfigManager.PopupFlipY)
        {
            if (ConfigManager.PositionPopupAboveCursor && newTop < screenBounds.Top)
            {
                newTop = mouseY + WindowsUtils.DpiAwareYOffset;
            }
            else if (!ConfigManager.PositionPopupAboveCursor && newTop + currentHeight > screenBounds.Bottom)
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

    private void UpdatePosition(double x, double y)
    {
        WinApi.MoveWindowToPosition(WindowHandle, x, y);
    }

    public void DisplayResults(bool generateAllResults)
    {
        _dictsWithResults.Clear();

        PopupListView.Items.Filter = PopupWindowUtils.NoAllDictFilter;

        _ = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict);
        bool pitchDictIsActive = pitchDict?.Active ?? false;
        Dict jmdict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        bool showPOrthographyInfo = jmdict.Options.POrthographyInfo!.Value;
        bool showROrthographyInfo = jmdict.Options.ROrthographyInfo!.Value;
        bool showAOrthographyInfo = jmdict.Options.AOrthographyInfo!.Value;
        double pOrthographyInfoFontSize = jmdict.Options.POrthographyInfoFontSize!.Value;

        int resultCount = generateAllResults
            ? LastLookupResults.Count
            : Math.Min(LastLookupResults.Count, ConfigManager.MaxNumResultsNotInMiningMode);

        StackPanel[] popupItemSource = new StackPanel[resultCount];

        for (int i = 0; i < resultCount; i++)
        {
            LookupResult lookupResult = LastLookupResults[i];

            if (!_dictsWithResults.Contains(lookupResult.Dict))
            {
                _dictsWithResults.Add(lookupResult.Dict);
            }

            popupItemSource[i] = PrepareResultStackPanel(lookupResult, i, resultCount, pitchDict, pitchDictIsActive, showPOrthographyInfo, showROrthographyInfo, showAOrthographyInfo, pOrthographyInfoFontSize);
        }

        PopupListView.ItemsSource = popupItemSource;
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

    private StackPanel PrepareResultStackPanel(LookupResult result, int index, int resultCount, Dict? pitchDict, bool pitchDictIsActive, bool showPOrthographyInfo, bool showROrthographyInfo, bool showAOrthographyInfo, double pOrthographyInfoFontSize)
    {
        // top
        WrapPanel top = new()
        {
            Tag = index
        };

        TextBlock primarySpellingTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            ConfigManager.PrimarySpellingColor,
            ConfigManager.PrimarySpellingFontSize,
            PopupContextMenu,
            VerticalAlignment.Center,
            new Thickness(2, 0, 0, 0));

        primarySpellingTextBlock.PreviewMouseUp += PrimarySpelling_PreviewMouseUp; // for mining

        if (result.Readings is null && pitchDictIsActive)
        {
            Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                result.AlternativeSpellings,
                null,
                primarySpellingTextBlock.Text.Split(", "),
                primarySpellingTextBlock.Margin.Left,
                pitchDict!,
                result.PitchAccentDict);

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
            TextBlock textBlockPOrthographyInfo = PopupWindowUtils.CreateTextBlock(nameof(result.PrimarySpellingOrthographyInfoList),
                $"({string.Join(", ", result.PrimarySpellingOrthographyInfoList)})",
                DictOptionManager.POrthographyInfoColor,
                pOrthographyInfoFontSize,
                PopupContextMenu,
                VerticalAlignment.Center,
                new Thickness(3, 0, 0, 0));

            _ = top.Children.Add(textBlockPOrthographyInfo);
        }

        if (result.Readings is not null && ConfigManager.ReadingsFontSize > 0
                                        && (pitchDictIsActive || (result.KunReadings is null && result.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && result.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToText(result.Readings, result.ReadingsOrthographyInfoList)
                : string.Join(", ", result.Readings);

            if (MiningMode)
            {
                TextBox readingTextBox = PopupWindowUtils.CreateTextBox(nameof(result.Readings),
                    readingsText, ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0));

                if (pitchDictIsActive)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBox.Text.Split(", "),
                        readingTextBox.Margin.Left,
                        pitchDict!,
                        result.PitchAccentDict);

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
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                if (pitchDictIsActive)
                {
                    Grid pitchAccentGrid = PopupWindowUtils.CreatePitchAccentGrid(result.PrimarySpelling,
                        result.AlternativeSpellings,
                        result.Readings,
                        readingTextBlock.Text.Split(", "),
                        readingTextBlock.Margin.Left,
                        pitchDict!,
                        result.PitchAccentDict);

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
                Foreground = ConfigManager.DefinitionsColor,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(3, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                Cursor = Cursors.Arrow,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = 12
            };

            audioButton.Click += AudioButton_Click;

            _ = top.Children.Add(audioButton);
        }

        if (result.AlternativeSpellings is not null && ConfigManager.AlternativeSpellingsFontSize > 0)
        {
            string alternativeSpellingsText = showAOrthographyInfo && result.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.ElementWithOrthographyInfoToTextWithParentheses(result.AlternativeSpellings, result.AlternativeSpellingsOrthographyInfoList)
                : $"({string.Join(", ", result.AlternativeSpellings)})";

            if (MiningMode)
            {
                TextBox alternativeSpellingsTexBox = PopupWindowUtils.CreateTextBox(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    ConfigManager.AlternativeSpellingsColor,
                    ConfigManager.AlternativeSpellingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(5, 0, 0, 0));

                AddEventHandlersToTextBox(alternativeSpellingsTexBox);

                _ = top.Children.Add(alternativeSpellingsTexBox);
            }
            else
            {
                TextBlock alternativeSpellingsTexBlock = PopupWindowUtils.CreateTextBlock(nameof(result.AlternativeSpellings),
                    alternativeSpellingsText,
                    ConfigManager.AlternativeSpellingsColor,
                    ConfigManager.AlternativeSpellingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(alternativeSpellingsTexBlock);
            }
        }

        if (result.DeconjugationProcess is not null && ConfigManager.DeconjugationInfoFontSize > 0)
        {
            if (MiningMode)
            {
                TextBox deconjugationProcessTextBox = PopupWindowUtils.CreateTextBox(nameof(result.DeconjugationProcess),
                    $"{result.MatchedText} {result.DeconjugationProcess}",
                    ConfigManager.DeconjugationInfoColor,
                    ConfigManager.DeconjugationInfoFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Top,
                    new Thickness(5, 0, 0, 0));

                AddEventHandlersToTextBox(deconjugationProcessTextBox);

                _ = top.Children.Add(deconjugationProcessTextBox);
            }
            else
            {
                TextBlock deconjugationProcessTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.DeconjugationProcess),
                    $"{result.MatchedText} {result.DeconjugationProcess}",
                    ConfigManager.DeconjugationInfoColor,
                    ConfigManager.DeconjugationInfoFontSize,
                    PopupContextMenu,
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
                    ConfigManager.FrequencyColor,
                    ConfigManager.FrequencyFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Top,
                    new Thickness(7, 0, 0, 0));

                _ = top.Children.Add(frequencyTextBlock);
            }
        }

        if (ConfigManager.DictTypeFontSize > 0)
        {
            TextBlock dictTypeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Dict.Name),
                result.Dict.Name,
                ConfigManager.DictTypeColor,
                ConfigManager.DictTypeFontSize,
                PopupContextMenu,
                VerticalAlignment.Top,
                new Thickness(7, 0, 0, 0));

            _ = top.Children.Add(dictTypeTextBlock);
        }

        // bottom
        StackPanel bottom = new();

        if (result.FormattedDefinitions is not null)
        {
            if (MiningMode)
            {
                TextBox definitionsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToDefinitionsTextBox(definitionsTextBox);
                _ = bottom.Children.Add(definitionsTextBox);
            }

            else
            {
                TextBlock definitionsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(definitionsTextBlock);
            }
        }

        // KANJIDIC
        if (result.OnReadings is not null)
        {
            if (MiningMode)
            {
                TextBox onReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.OnReadings),
                    $"On: {string.Join(", ", result.OnReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(onReadingsTextBox);

                _ = bottom.Children.Add(onReadingsTextBox);
            }

            else
            {
                TextBlock onReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.OnReadings),
                    $"On: {string.Join(", ", result.OnReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(onReadingsTextBlock);
            }
        }

        if (result.KunReadings is not null)
        {
            if (MiningMode)
            {
                TextBox kunReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.KunReadings),
                    $"Kun: {string.Join(", ", result.KunReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(kunReadingsTextBox);

                _ = bottom.Children.Add(kunReadingsTextBox);
            }

            else
            {
                TextBlock kunReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KunReadings),
                    $"Kun: {string.Join(", ", result.KunReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(kunReadingsTextBlock);
            }
        }

        if (result.NanoriReadings is not null)
        {
            if (MiningMode)
            {
                TextBox nanoriReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.NanoriReadings),
                    $"Nanori: {string.Join(", ", result.NanoriReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(nanoriReadingsTextBox);

                _ = bottom.Children.Add(nanoriReadingsTextBox);
            }

            else
            {
                TextBlock nanoriReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.NanoriReadings),
                    $"Nanori: {string.Join(", ", result.NanoriReadings)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(nanoriReadingsTextBlock);
            }
        }

        if (result.RadicalNames is not null)
        {
            if (MiningMode)
            {
                TextBox radicalNameTextBox = PopupWindowUtils.CreateTextBox(nameof(result.RadicalNames),
                    $"Radical names: {string.Join(", ", result.RadicalNames)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(radicalNameTextBox);

                _ = bottom.Children.Add(radicalNameTextBox);
            }

            else
            {
                TextBlock radicalNameTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.RadicalNames),
                    $"Radical names: {string.Join(", ", result.RadicalNames)}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(radicalNameTextBlock);
            }
        }

        if (result.KanjiGrade is not byte.MaxValue)
        {
            TextBlock gradeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiGrade),
                $"Grade: {LookupResultUtils.GradeToText(result.KanjiGrade)}",
                ConfigManager.DefinitionsColor,
                ConfigManager.DefinitionsFontSize,
                PopupContextMenu,
                VerticalAlignment.Center,
                new Thickness(2));

            _ = bottom.Children.Add(gradeTextBlock);
        }

        if (result.StrokeCount > 0)
        {
            TextBlock strokeCountTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.StrokeCount),
                string.Create(CultureInfo.InvariantCulture, $"Stroke count: {result.StrokeCount}"),
                ConfigManager.DefinitionsColor,
                ConfigManager.DefinitionsFontSize,
                PopupContextMenu,
                VerticalAlignment.Center,
                new Thickness(2));

            _ = bottom.Children.Add(strokeCountTextBlock);
        }

        if (result.KanjiComposition is not null)
        {
            if (MiningMode)
            {
                TextBox compositionTextBox = PopupWindowUtils.CreateTextBox(nameof(result.KanjiComposition),
                    $"Composition: {result.KanjiComposition}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(compositionTextBox);

                _ = bottom.Children.Add(compositionTextBox);
            }

            else
            {
                TextBlock compositionTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiComposition),
                    $"Composition: {result.KanjiComposition}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(compositionTextBlock);
            }
        }

        if (result.KanjiStats is not null)
        {
            if (MiningMode)
            {
                TextBox kanjiStatsTextBlock = PopupWindowUtils.CreateTextBox(nameof(result.KanjiStats),
                    $"Statistics:\n{result.KanjiStats}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(0, 2, 2, 2));

                AddEventHandlersToTextBox(kanjiStatsTextBlock);

                _ = bottom.Children.Add(kanjiStatsTextBlock);
            }

            else
            {
                TextBlock kanjiStatsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiStats),
                    $"Statistics:\n{result.KanjiStats}",
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(kanjiStatsTextBlock);
            }
        }

        if (index != resultCount - 1)
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

    private Task HandleTextBoxMouseMove(TextBox textBox, MouseEventArgs? e)
    {
        if (ConfigManager.InactiveLookupMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnMouseClickOnly
            || e?.LeftButton is MouseButtonState.Pressed
            || PopupContextMenu.IsVisible
            || ReadingSelectionWindow.IsItVisible()
            || (ConfigManager.RequireLookupKeyPress
                && !ConfigManager.LookupKeyKeyGesture.IsPressed()))
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
            if (JapaneseUtils.JapaneseRegex().IsMatch(textBox.Text))
            {
                return ChildPopupWindow.LookupOnMouseMoveOrClick(textBox);
            }

            if (ConfigManager.HighlightLongestMatch)
            {
                WindowsUtils.Unselect(ChildPopupWindow._previousTextBox);
            }
        }

        return Task.CompletedTask;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void TextBox_MouseMove(object sender, MouseEventArgs? e)
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

        int listViewItemIndex = _listViewItemIndex;
        TextBox? definitionsTextBox = GetDefinitionTextBox(listViewItemIndex);
        string? formattedDefinitions = definitionsTextBox?.Text;
        string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

        HidePopup();

        if (ConfigManager.MineToFileInsteadOfAnki)
        {
            await MiningUtils.MineToFile(LastLookupResults[listViewItemIndex], _currentText, formattedDefinitions, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
        }
        else
        {
            await MiningUtils.Mine(LastLookupResults[listViewItemIndex], _currentText, formattedDefinitions, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
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
        bool handled = false;
        if (keyGesture.IsEqual(ConfigManager.DisableHotkeysKeyGesture))
        {
            if (e is not null)
            {
                e.Handled = true;
            }

            handled = true;

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

        if (ConfigManager.DisableHotkeys || handled)
        {
            return;
        }

        if (keyGesture.IsEqual(ConfigManager.MiningModeKeyGesture))
        {
            handled = true;

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

        else if (keyGesture.IsEqual(ConfigManager.PlayAudioKeyGesture))
        {
            handled = true;

            await PlayAudio().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(ConfigManager.ClosePopupKeyGesture))
        {
            handled = true;

            HidePopup();
        }

        else if (keyGesture.IsEqual(ConfigManager.KanjiModeKeyGesture))
        {
            handled = true;

            CoreConfigManager.KanjiMode = !CoreConfigManager.KanjiMode;
            LastText = "";

            if (this != MainWindow.Instance.FirstPopupWindow)
            {
                await HandleTextBoxMouseMove(_previousTextBox!, null).ConfigureAwait(false);
            }

            else
            {
                await MainWindow.Instance.HandleMouseMove(null).ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.ShowAddNameWindowKeyGesture))
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
                        MainWindow.Instance.ShowAddNameWindow();
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

        else if (keyGesture.IsEqual(ConfigManager.ShowAddWordWindowKeyGesture))
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
                        MainWindow.Instance.ShowAddWordWindow();
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

        else if (keyGesture.IsEqual(ConfigManager.SearchWithBrowserKeyGesture))
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
                    MainWindow.Instance.SearchWithBrowser();
                }

                HidePopup();
            }

            else
            {
                SearchWithBrowser();
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.InactiveLookupModeKeyGesture))
        {
            handled = true;

            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (keyGesture.IsEqual(ConfigManager.MotivationKeyGesture))
        {
            handled = true;

            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(ConfigManager.NextDictKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.PreviousDictKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.ToggleMinimizedStateKeyGesture))
        {
            handled = true;

            PopupWindowUtils.HidePopups(MainWindow.Instance.FirstPopupWindow);

            if (ConfigManager.Focusable)
            {
                MainWindow.Instance.WindowState = MainWindow.Instance.WindowState is WindowState.Minimized
                    ? WindowState.Normal
                    : WindowState.Minimized;
            }

            else
            {
                if (MainWindow.Instance.WindowState is WindowState.Minimized)
                {
                    WinApi.RestoreWindow(MainWindow.Instance.WindowHandle);
                }

                else
                {
                    WinApi.MinimizeWindow(MainWindow.Instance.WindowHandle);
                }
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.SelectedTextToSpeechKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.SelectNextLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                SelectNextLookupResult();
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.SelectPreviousLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode)
            {
                SelectPreviousLookupResult();
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.MineSelectedLookupResultKeyGesture))
        {
            handled = true;

            if (MiningMode && PopupListView.SelectedItem is not null)
            {
                int index = PopupWindowUtils.GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
                TextBox? definitionsTextBox = GetDefinitionTextBox(index);
                string? formattedDefinitions = definitionsTextBox?.Text;
                string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

                HidePopup();

                if (ConfigManager.MineToFileInsteadOfAnki)
                {
                    await MiningUtils.MineToFile(LastLookupResults[index], _currentText, formattedDefinitions, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
                }
                else
                {
                    await MiningUtils.Mine(LastLookupResults[index], _currentText, formattedDefinitions, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
                }
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            handled = true;

            ConfigManager.AlwaysShowMainTextBoxCaret = !ConfigManager.AlwaysShowMainTextBoxCaret;
            MainWindow.Instance.MainTextBox.IsReadOnlyCaretVisible = ConfigManager.AlwaysShowMainTextBoxCaret;
        }

        else if (keyGesture.IsEqual(ConfigManager.LookupSelectedTextKeyGesture))
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

                else if (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow)
                {
                    UpdatePosition(ConfigManager.FixedPopupXPosition, ConfigManager.FixedPopupYPosition);
                }

                else
                {
                    UpdatePosition(WinApi.GetMousePosition());
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
                await MainWindow.Instance.FirstPopupWindow.LookupOnSelect(MainWindow.Instance.MainTextBox).ConfigureAwait(false);
            }
        }

        if (handled && e is not null)
        {
            e.Handled = true;
        }
    }

    public void EnableMiningMode()
    {
        MiningMode = true;

        TitleBarGrid.Visibility = Visibility.Visible;

        if (ConfigManager.ShowMiningModeReminder && this == MainWindow.Instance.FirstPopupWindow)
        {
            TextBlockMiningModeReminder.Visibility = Visibility.Visible;
        }

        ItemsControlButtons.Visibility = Visibility.Visible;
    }

    private Task PlayAudio()
    {
        if (LastLookupResults.Count is 0)
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
                PopupWindowUtils.PopupAutoHideTimer.Stop();
            }

            return;
        }

        if (UnavoidableMouseEnter
            || (ConfigManager.FixedPopupPositioning && this == MainWindow.Instance.FirstPopupWindow))
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

        if (ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress && !ConfigManager.LookupKeyKeyGesture.IsPressed())
            || ((!ConfigManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!ConfigManager.LookupOnMouseClickOnly || e.ChangedButton != ConfigManager.LookupOnClickMouseButton)))
        {
            return;
        }

        ChildPopupWindow ??= new PopupWindow
        {
            Owner = this
        };

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
                    || ReadingSelectionWindow.IsItVisible()
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

        foreach (Dict dict in DictUtils.Dicts.Values.OrderBy(static dict => dict.Priority).ToList())
        {
            if (!dict.Active || dict.Type is DictType.PitchAccentYomichan || (ConfigManager.HideDictTabsWithNoResults && !_dictsWithResults.Contains(dict)))
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
        bool isFirstPopup = this == MainWindow.Instance.FirstPopupWindow;

        if (isFirstPopup
            && (ConfigManager.TextOnlyVisibleOnHover || ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible())
        {
            _ = MainWindow.Instance.ChangeVisibility().ConfigureAwait(true);
        }

        if (!IsVisible)
        {
            return;
        }

        ReadingSelectionWindow.HideWindow();

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
        LastText = "";
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
            WinApi.ActivateWindow(MainWindow.Instance.WindowHandle);

            if (ConfigManager.HighlightLongestMatch && !MainWindow.Instance.ContextMenuIsOpening)
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

            if (ConfigManager.HighlightLongestMatch && !previousPopup._contextMenuIsOpening)
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
        _listViewItemIndex = _firstVisibleListViewItemIndex;
        LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
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

    private void Window_Closed(object sender, EventArgs e)
    {
        Owner = null;
        ChildPopupWindow = null;
        _previousTextBox = null;
        _lastInteractedTextBox = null;
        LastSelectedText = null;
        LastText = null;
        _filteredDict = null;
        _popupListViewScrollViewer = null;
        ItemsControlButtons.ItemsSource = null;
        PopupListView.ItemsSource = null;
        _lastInteractedTextBox = null;
        LastLookupResults = null!;
        _dictsWithResults = null!;
        _currentText = null!;
        _buttonAll.Click -= DictTypeButtonOnClick;
        _buttonAll = null!;
    }
}
