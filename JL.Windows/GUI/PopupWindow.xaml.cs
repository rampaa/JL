using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Caching;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Mining;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PopupWindow.xaml
/// </summary>
internal sealed partial class PopupWindow : Window
{
    public PopupWindow? ChildPopupWindow { get; set; }
    private bool ContextMenuIsOpening { get; set; } = false;

    private TextBox? _previousTextBox;

    private TextBox? _lastInteractedTextBox;

    private int _listViewItemIndex = 0;

    private int _firstVisibleListViewItemIndex = 0;

    private int _currentCharPosition;

    private string _currentText = "";

    private readonly Button _buttonAll = new() { Content = "All", Margin = new Thickness(1), Background = Brushes.DodgerBlue };

    public string? LastSelectedText { get; private set; }

    public nint WindowHandle { get; private set; }

    public List<LookupResult> LastLookupResults { get; private set; } = new();

    public List<Dict> DictsWithResults { get; } = new();

    private Dict? _filteredDict = null;

    public bool UnavoidableMouseEnter { get; private set; } = false;

    public string? LastText { get; set; }

    public bool MiningMode { get; private set; }

    public static Timer PopupAutoHideTimer { get; } = new();

    public static LRUCache<string, StackPanel[]> StackPanelCache { get; } = new(Utils.CacheSize, Utils.CacheSize / 5);

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
            : LastLookupResults[_listViewItemIndex].PrimarySpelling;

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
            : LastLookupResults[_listViewItemIndex].PrimarySpelling;

        WindowsUtils.SearchWithBrowser(text);
    }

    public async Task LookupOnCharPosition(TextBox tb, string textBoxText, int charPosition, bool enableMiningMode)
    {
        _currentText = textBoxText;
        _currentCharPosition = charPosition;

        if (Owner != MainWindow.Instance
                ? ConfigManager.DisableLookupsForNonJapaneseCharsInPopups
                  && !JapaneseUtils.JapaneseRegex.IsMatch(textBoxText[charPosition].ToString())
                : ConfigManager.DisableLookupsForNonJapaneseCharsInMainWindow
                  && !JapaneseUtils.JapaneseRegex.IsMatch(textBoxText[charPosition].ToString()))
        {
            HidePopup();
            return;
        }

        int endPosition = (textBoxText.Length - charPosition) > ConfigManager.MaxSearchLength
            ? JapaneseUtils.FindExpressionBoundary(textBoxText[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
            : JapaneseUtils.FindExpressionBoundary(textBoxText, charPosition);

        string text = textBoxText[charPosition..endPosition];

        if (string.IsNullOrEmpty(text))
        {
            HidePopup();
            return;
        }

        if (text == LastText && IsVisible)
        {
            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
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

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
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

            await LookupOnCharPosition(tb, tb.Text, charPosition, ConfigManager.LookupOnMouseClickOnly).ConfigureAwait(false);
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

            _firstVisibleListViewItemIndex = GetFirstVisibleListViewItemIndex();
            _listViewItemIndex = _firstVisibleListViewItemIndex;

            if (ConfigManager.FixedPopupPositioning && Owner == MainWindow.Instance)
            {
                UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
            }

            else
            {
                UpdatePosition(WinApi.GetMousePosition());
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

        bool needsFlipX = ConfigManager.PopupFlipX && (mouseX + ActualWidth) > (WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth);
        bool needsFlipY = ConfigManager.PopupFlipY && (mouseY + ActualHeight) > (WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight);

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
        if ((newLeft + ActualWidth) > (WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth))
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth - ActualWidth;
        }

        if ((newTop + ActualHeight) > (WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight))
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight - ActualHeight;
        }

        if (mouseX >= newLeft && mouseX <= (newLeft + ActualWidth) && mouseY >= newTop && mouseY <= (newTop + ActualHeight))
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
            double pOrthographyInfoFontSize = jmdict.Options?.POrthographyInfoFontSize?.Value ?? 15;

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

                popupItemSource[i] = PrepareResultStackPanel(lookupResult, i, resultCount, pitchDict, pitchDictIsActive, showPOrthographyInfo, showROrthographyInfo, showAOrthographyInfo, pOrthographyInfoFontSize);
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

    private void AddEventHandlersToTextBox(TextBox textBox)
    {
        textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
        textBox.MouseMove += TextBox_MouseMove;
        textBox.LostFocus += Unselect;
        textBox.PreviewMouseRightButtonUp += TextBox_PreviewMouseRightButtonUp;
        textBox.MouseLeave += OnMouseLeave;
        textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
    }

    public StackPanel PrepareResultStackPanel(LookupResult result, int index, int resultsCount, Dict? pitchDict, bool pitchDictIsActive, bool showPOrthographyInfo, bool showROrthographyInfo, bool showAOrthographyInfo, double pOrthographyInfoFontSize)
    {
        // top
        WrapPanel top = new() { Tag = index };

        TextBlock primarySpellingTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.PrimarySpelling),
            result.PrimarySpelling,
            ConfigManager.PrimarySpellingColor,
            ConfigManager.PrimarySpellingFontSize,
            PopupContextMenu,
            VerticalAlignment.Center,
            new Thickness(5, 0, 0, 0));

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
                string.Create(CultureInfo.InvariantCulture, $"({string.Join(", ", result.PrimarySpellingOrthographyInfoList)})"),
                DictOptionManager.POrthographyInfoColor,
                pOrthographyInfoFontSize,
                PopupContextMenu,
                VerticalAlignment.Center,
                new Thickness(5, 0, 0, 0));

            _ = top.Children.Add(textBlockPOrthographyInfo);
        }

        if (result.Readings is not null
            && (pitchDictIsActive || (result.KunReadings is null && result.OnReadings is null)))
        {
            string readingsText = showROrthographyInfo && result.ReadingsOrthographyInfoList is not null
                ? LookupResultUtils.ReadingsToText(result.Readings, result.ReadingsOrthographyInfoList)
                : string.Join(", ", result.Readings);

            if (MiningMode)
            {
                TouchScreenTextBox readingTextBox = PopupWindowUtils.CreateTextBox(nameof(result.Readings),
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
                    new Thickness(5, 0, 0, 0));

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
                Margin = new Thickness(5, 0, 0, 0),
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

        if (result.AlternativeSpellings is not null)
        {
            string alternativeSpellingsText = showAOrthographyInfo && result.AlternativeSpellingsOrthographyInfoList is not null
                ? LookupResultUtils.AlternativeSpellingsToText(result.AlternativeSpellings, result.AlternativeSpellingsOrthographyInfoList)
                : string.Create(CultureInfo.InvariantCulture, $"({string.Join(", ", result.AlternativeSpellings)})");

            if (MiningMode)
            {
                TouchScreenTextBox alternativeSpellingsTexBox = PopupWindowUtils.CreateTextBox(nameof(result.AlternativeSpellings),
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
                    new Thickness(5, 0, 0, 0));

                _ = top.Children.Add(alternativeSpellingsTexBlock);
            }
        }

        if (result.DeconjugationProcess is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox deconjugationProcessTextBox = PopupWindowUtils.CreateTextBox(nameof(result.DeconjugationProcess),
                    string.Create(CultureInfo.InvariantCulture, $"{result.MatchedText} {result.DeconjugationProcess}"),
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
                    string.Create(CultureInfo.InvariantCulture, $"{result.MatchedText} {result.DeconjugationProcess}"),
                    ConfigManager.DeconjugationInfoColor,
                    ConfigManager.DeconjugationInfoFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Top,
                    new Thickness(5, 0, 0, 0));

                _ = top.Children.Add(deconjugationProcessTextBlock);
            }
        }

        if (result.Frequencies is not null)
        {
            string? freqText = LookupResultUtils.FrequenciesToText(result.Frequencies, false);
            if (freqText is not null)
            {
                TextBlock frequencyTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Frequencies),
                    freqText,
                    ConfigManager.FrequencyColor,
                    ConfigManager.FrequencyFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Top,
                    new Thickness(5, 0, 0, 0));

                _ = top.Children.Add(frequencyTextBlock);
            }
        }

        TextBlock dictTypeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.Dict.Name),
            result.Dict.Name,
            ConfigManager.DictTypeColor,
            ConfigManager.DictTypeFontSize,
            PopupContextMenu,
            VerticalAlignment.Top,
            new Thickness(5, 0, 0, 0));

        _ = top.Children.Add(dictTypeTextBlock);

        // bottom
        StackPanel bottom = new();

        if (result.FormattedDefinitions is not null)
        {
            if (MiningMode)
            {
                TouchScreenTextBox definitionsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.FormattedDefinitions),
                    result.FormattedDefinitions,
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(definitionsTextBox);

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
                TouchScreenTextBox onReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.OnReadings),
                    string.Create(CultureInfo.InvariantCulture, $"On: {string.Join(", ", result.OnReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(onReadingsTextBox);

                _ = bottom.Children.Add(onReadingsTextBox);
            }

            else
            {
                TextBlock onReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.OnReadings),
                    string.Create(CultureInfo.InvariantCulture, $"On: {string.Join(", ", result.OnReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
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
                TouchScreenTextBox kunReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.KunReadings),
                    string.Create(CultureInfo.InvariantCulture, $"Kun: {string.Join(", ", result.KunReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(kunReadingsTextBox);

                _ = bottom.Children.Add(kunReadingsTextBox);
            }

            else
            {
                TextBlock kunReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KunReadings),
                    string.Create(CultureInfo.InvariantCulture, $"Kun: {string.Join(", ", result.KunReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
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
                TouchScreenTextBox nanoriReadingsTextBox = PopupWindowUtils.CreateTextBox(nameof(result.NanoriReadings),
                    string.Create(CultureInfo.InvariantCulture, $"Nanori: {string.Join(", ", result.NanoriReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(nanoriReadingsTextBox);

                _ = bottom.Children.Add(nanoriReadingsTextBox);
            }

            else
            {
                TextBlock nanoriReadingsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.NanoriReadings),
                    string.Create(CultureInfo.InvariantCulture, $"Nanori: {string.Join(", ", result.NanoriReadings)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
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
                TouchScreenTextBox radicalNameTextBox = PopupWindowUtils.CreateTextBox(nameof(result.RadicalNames),
                    string.Create(CultureInfo.InvariantCulture, $"Radical names: {string.Join(", ", result.RadicalNames)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(radicalNameTextBox);

                _ = bottom.Children.Add(radicalNameTextBox);
            }

            else
            {
                TextBlock radicalNameTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.RadicalNames),
                    string.Create(CultureInfo.InvariantCulture, $"Radical names: {string.Join(", ", result.RadicalNames)}"),
                    ConfigManager.ReadingsColor,
                    ConfigManager.ReadingsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                _ = bottom.Children.Add(radicalNameTextBlock);
            }
        }

        if (result.KanjiGrade > -1)
        {
            TextBlock gradeTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiGrade),
                    string.Create(CultureInfo.InvariantCulture, $"Grade: {LookupResultUtils.GradeToText(result.KanjiGrade)}"),
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
                TouchScreenTextBox compositionTextBox = PopupWindowUtils.CreateTextBox(nameof(result.KanjiComposition),
                    string.Create(CultureInfo.InvariantCulture, $"Composition: {result.KanjiComposition}"),
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

                AddEventHandlersToTextBox(compositionTextBox);

                _ = bottom.Children.Add(compositionTextBox);
            }

            else
            {
                TextBlock compositionTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiComposition),
                    string.Create(CultureInfo.InvariantCulture, $"Composition: {result.KanjiComposition}"),
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
            TextBlock kanjiStatsTextBlock = PopupWindowUtils.CreateTextBlock(nameof(result.KanjiStats),
                    string.Create(CultureInfo.InvariantCulture, $"Statistics:\n{result.KanjiStats}"),
                    ConfigManager.DefinitionsColor,
                    ConfigManager.DefinitionsFontSize,
                    PopupContextMenu,
                    VerticalAlignment.Center,
                    new Thickness(2));

            _ = bottom.Children.Add(kanjiStatsTextBlock);
        }

        if (index != (resultsCount - 1))
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

        stackPanel.MouseEnter += ListViewItem_MouseEnter;

        return stackPanel;
    }

    private static int GetIndexOfListViewItemFromStackPanel(StackPanel stackPanel)
    {
        return (int)((WrapPanel)stackPanel.Children[0]).Tag;
    }

    private int GetFirstVisibleListViewItemIndex()
    {
        StackPanel? firstVisibleStackPanel = PopupListView.Items.Cast<StackPanel>()
            .FirstOrDefault(static stackPanel => stackPanel.Visibility is Visibility.Visible);

        return firstVisibleStackPanel is not null
            ? GetIndexOfListViewItemFromStackPanel(firstVisibleStackPanel)
            : 0;
    }

    private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
    {
        _listViewItemIndex = GetIndexOfListViewItemFromStackPanel((StackPanel)sender);
        LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
    }

    private static void Unselect(object sender, RoutedEventArgs e)
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

    private async void TextBox_MouseMove(object sender, MouseEventArgs? e)
    {
        if (ConfigManager.InactiveLookupMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnMouseClickOnly
            || e?.LeftButton is MouseButtonState.Pressed
            || PopupContextMenu.IsVisible
            || ReadingSelectionWindow.IsItVisible()
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

    private async void AudioButton_Click(object sender, RoutedEventArgs e)
    {
        LookupResult lookupResult = LastLookupResults[_listViewItemIndex];
        if (lookupResult.Readings is null || lookupResult.Readings.Length is 1)
        {
            await PopupWindowUtils.PlayAudio(lookupResult.PrimarySpelling, lookupResult.Readings?[0]).ConfigureAwait(false);
        }
        else
        {
            ReadingSelectionWindow.Show(lookupResult.PrimarySpelling, lookupResult.Readings);
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

        int listViewItemIndex = _listViewItemIndex;
        string? selectedDefinitions = GetSelectedDefinitions(listViewItemIndex);

        HidePopup();

        if (ConfigManager.MineToFileInsteadOfAnki)
        {
            await MiningUtils.MineToFile(LastLookupResults[listViewItemIndex], _currentText, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
        }
        else
        {
            await MiningUtils.Mine(LastLookupResults[listViewItemIndex], _currentText, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
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

        WindowsUtils.ShowAddNameWindow(text, reading);
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    private void SelectNextLookupResult()
    {
        int nextItemIndex = (PopupListView.SelectedIndex + 1) < PopupListView.Items.Count
            ? PopupListView.SelectedIndex + 1
            : 0;

        PopupListView.SelectedIndex = nextItemIndex;

        PopupListView.ScrollIntoView(PopupListView.Items.GetItemAt(nextItemIndex));
    }

    private void SelectPreviousLookupResult()
    {
        int nextItemIndex = (PopupListView.SelectedIndex - 1) > -1
            ? PopupListView.SelectedIndex - 1
            : PopupListView.Items.Count - 1;

        PopupListView.SelectedIndex = nextItemIndex;

        PopupListView.ScrollIntoView(PopupListView.Items.GetItemAt(nextItemIndex));
    }

    public async Task HandleHotKey(KeyGesture keyGesture)
    {
        bool handled = false;
        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.DisableHotkeysKeyGesture))
        {
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

                PopupAutoHideTimer.Start();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddWordWindowKeyGesture))
        {
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

            Button? nextButton = null;
            for (int i = 0; i < ItemsControlButtons.Items.Count; i++)
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

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.PreviousDictKeyGesture))
        {
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
                    : LastLookupResults[_listViewItemIndex].PrimarySpelling;

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
                int index = GetIndexOfListViewItemFromStackPanel((StackPanel)PopupListView.SelectedItem);
                string? selectedDefinitions = GetSelectedDefinitions(index);

                HidePopup();

                if (ConfigManager.MineToFileInsteadOfAnki)
                {
                    await MiningUtils.MineToFile(LastLookupResults[index], _currentText, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
                }
                else
                {
                    await MiningUtils.Mine(LastLookupResults[index], _currentText, selectedDefinitions, _currentCharPosition).ConfigureAwait(false);
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            ConfigManager.AlwaysShowMainTextBoxCaret = !ConfigManager.AlwaysShowMainTextBoxCaret;
            MainWindow.Instance.MainTextBox.IsReadOnlyCaretVisible = ConfigManager.AlwaysShowMainTextBoxCaret;
        }
    }

    public void EnableMiningMode()
    {
        MiningMode = true;

        TitleBarGrid.Visibility = Visibility.Visible;

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

        LookupResult lastLookupResult = LastLookupResults[_listViewItemIndex];
        string primarySpelling = lastLookupResult.PrimarySpelling;
        string? reading = lastLookupResult.Readings?[0];

        await PopupWindowUtils.PlayAudio(primarySpelling, reading).ConfigureAwait(false);
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
                    || ReadingSelectionWindow.IsItVisible()
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
        List<Button> buttons = new(DictUtils.Dicts.Values.Count + 1);
        _buttonAll.Background = Brushes.DodgerBlue;
        _buttonAll.Click += DictTypeButtonOnClick;
        buttons.Add(_buttonAll);

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

    private void DictTypeButtonOnClick(object sender, RoutedEventArgs e)
    {
        ClickDictTypeButton((Button)sender);
    }

    private void ClickDictTypeButton(Button button)
    {
        foreach (Button btn in ItemsControlButtons.Items)
        {
            btn.ClearValue(BackgroundProperty);
        }

        button.Background = Brushes.DodgerBlue;

        bool isAllButton = button == _buttonAll;
        if (isAllButton)
        {
            PopupListView.Items.Filter = NoAllDictFilter;
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
        MainWindow mainWindow = MainWindow.Instance;
        bool isFirstPopup = Owner == mainWindow;

        if (isFirstPopup
            && (ConfigManager.TextOnlyVisibleOnHover || ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible())
        {
            _ = mainWindow.ChangeVisibility().ConfigureAwait(true);
        }

        ReadingSelectionWindow.HideWindow();

        if (!IsVisible)
        {
            return;
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
        _listViewItemIndex = _firstVisibleListViewItemIndex;
        LastSelectedText = LastLookupResults[_listViewItemIndex].PrimarySpelling;
    }

    private TextBox? GetDefinitionTextBox(int listViewIndex)
    {
        return ((StackPanel)((StackPanel)PopupListView.Items[listViewIndex]!).Children[1]).GetChildByName<TextBox>(nameof(LookupResult.FormattedDefinitions));
    }

    private string? GetSelectedDefinitions(int listViewIndex)
    {
        TextBox? definitionTextBox = GetDefinitionTextBox(listViewIndex);
        return definitionTextBox?.SelectionLength > 0
            ? definitionTextBox.SelectedText
            : null;
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
}
