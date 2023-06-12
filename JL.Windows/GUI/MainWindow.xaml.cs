using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Tools;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Lookup;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Win32;
using Window = System.Windows.Window;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : Window
{
    private readonly List<string> _backlog = new();
    private int _currentTextIndex = 0;
    private bool _stopPrecache = false;
    private DateTime _lastClipboardChangeTime;
    private WinApi? _winApi;
    public IntPtr WindowHandle { get; private set; }

    public PopupWindow FirstPopupWindow { get; }

    private static MainWindow? s_instance;
    public static MainWindow Instance => s_instance ??= new MainWindow();

    public double LeftPositionBeforeResolutionChange { get; set; }
    public double TopPositionBeforeResolutionChange { get; set; }
    public double HeightBeforeResolutionChange { get; set; }
    public double WidthBeforeResolutionChange { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        s_instance = this;
        ConfigHelper.Instance.SetLang("en");
        FirstPopupWindow = new PopupWindow();
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        AppDomain.CurrentDomain.UnhandledException += static (_, eventArgs) =>
        {
            Exception ex = (Exception)eventArgs.ExceptionObject;
            Utils.Logger.Fatal(ex, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += static (_, eventArgs) => Utils.Logger.Fatal(eventArgs.Exception, "Unobserved task exception");

        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        WindowHandle = new WindowInteropHelper(this).Handle;
        _winApi = new WinApi();
        _winApi.SubscribeToWndProc(this);
        WinApi.SubscribeToClipboardChanged(WindowHandle);
        _winApi.ClipboardChanged += ClipboardChanged;

        ConfigManager.ApplyPreferences();

        WinApi.RestoreWindow(WindowHandle);
        _ = Focus();

        if (ConfigManager.CaptureTextFromClipboard)
        {
            CopyFromClipboard();
            _lastClipboardChangeTime = new DateTime(Stopwatch.GetTimestamp());
        }

        FirstPopupWindow.Owner = this;

        await WindowsUtils.InitializeMainWindow().ConfigureAwait(false);
    }

    private async void CopyFromClipboard()
    {
        bool gotTextFromClipboard = false;
        while (Clipboard.ContainsText() && !gotTextFromClipboard)
        {
            try
            {
                string text = Clipboard.GetText();
                gotTextFromClipboard = true;
                if (!ConfigManager.OnlyCaptureTextWithJapaneseChars || JapaneseUtils.JapaneseRegex.IsMatch(text))
                {
                    text = SanitizeText(text);

                    MainTextBox.Text = text;
                    MainTextBox.Foreground = ConfigManager.MainWindowTextColor;

                    await HandlePostCopy(text).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Utils.Logger.Warning(ex, "CopyFromClipboard failed");
            }
        }
    }

    public async Task CopyFromWebSocket(string text)
    {
        if (!ConfigManager.OnlyCaptureTextWithJapaneseChars || JapaneseUtils.JapaneseRegex.IsMatch(text))
        {
            text = SanitizeText(text);

            Dispatcher.Invoke(() =>
            {
                MainTextBox.Text = text;
                MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
            });

            await HandlePostCopy(text).ConfigureAwait(false);
        }
    }

    private static string SanitizeText(string text)
    {
        if (ConfigManager.TextBoxTrimWhiteSpaceCharacters)
        {
            text = text.Trim();
        }

        if (ConfigManager.TextBoxRemoveNewlines)
        {
            text = text.ReplaceLineEndings("");
        }

        return text;
    }

    private async Task HandlePostCopy(string text)
    {
        _backlog.Add(text);
        _currentTextIndex = _backlog.Count - 1;

        await Stats.IncrementStat(StatType.Characters, new StringInfo(JapaneseUtils.RemovePunctuation(text)).LengthInTextElements).ConfigureAwait(false);
        await Stats.IncrementStat(StatType.Lines).ConfigureAwait(false);

        Dispatcher.Invoke(() =>
        {
            if (SizeToContent is SizeToContent.Manual && (ConfigManager.MainWindowDynamicHeight || ConfigManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContent(ConfigManager.MainWindowDynamicWidth, ConfigManager.MainWindowDynamicHeight, this);
            }
        });

        if (ConfigManager.AlwaysOnTop
            && !FirstPopupWindow.IsVisible
            && !ManageDictionariesWindow.IsItVisible()
            && !ManageFrequenciesWindow.IsItVisible()
            && !ManageAudioSourcesWindow.IsItVisible()
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible()
            && !PreferencesWindow.IsItVisible()
            && !StatsWindow.IsItVisible()
            && !MainTextboxContextMenu.IsVisible
            && !TitleBarContextMenu.IsVisible)
        {
            WinApi.BringToFront(WindowHandle);
        }

        if (ConfigManager.Precaching && DictUtils.DictsReady
            && !DictUtils.UpdatingJmdict && !DictUtils.UpdatingJmnedict && !DictUtils.UpdatingKanjidic
            && FreqUtils.FreqsReady && MainTextBox.Text.Length < Utils.CacheSize)
        {
            _ = Dispatcher.Invoke(DispatcherPriority.Render, static () => { }); // let MainTextBox text update
            await Precache(MainTextBox.Text).ConfigureAwait(false);
        }
    }

    private async Task DeleteCurrentLine()
    {
        if (_backlog.Count is 0 || MainTextBox.Text != _backlog[_currentTextIndex])
        {
            return;
        }

        await Stats.IncrementStat(StatType.Characters,
            new StringInfo(JapaneseUtils.RemovePunctuation(_backlog[_currentTextIndex])).LengthInTextElements * -1)
            .ConfigureAwait(false);

        await Stats.IncrementStat(StatType.Lines, -1).ConfigureAwait(false);

        _backlog.RemoveAt(_currentTextIndex);

        if (_currentTextIndex > 0)
        {
            --_currentTextIndex;
        }

        MainTextBox.Foreground = _currentTextIndex < _backlog.Count - 1
            ? ConfigManager.MainWindowBacklogTextColor
            : ConfigManager.MainWindowTextColor;

        MainTextBox.Text = _backlog.Count > 0
            ? _backlog[_currentTextIndex]
            : "";
    }

    private async Task Precache(string input)
    {
        FirstPopupWindow.DictsWithResults.Clear();

        for (int charPosition = 0; charPosition < input.Length; charPosition++)
        {
            if (_stopPrecache)
            {
                _stopPrecache = false;
                return;
            }

            if (charPosition > 0 && char.IsHighSurrogate(input[charPosition - 1]))
            {
                --charPosition;
            }

            if (charPosition % 10 is 0)
            {
                await Task.Delay(1).ConfigureAwait(true); // let user interact with the GUI while this method is running
            }

            int endPosition = input.Length - charPosition > ConfigManager.MaxSearchLength
                ? JapaneseUtils.FindExpressionBoundary(input[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
                : JapaneseUtils.FindExpressionBoundary(input, charPosition);

            string text = input[charPosition..endPosition];

            if (!PopupWindow.StackPanelCache.Contains(text))
            {
                List<LookupResult>? lookupResults = Lookup.LookupText(text);
                if (lookupResults is { Count: > 0 })
                {
                    int resultCount = Math.Min(lookupResults.Count, ConfigManager.MaxNumResultsNotInMiningMode);
                    var popupItemSource = new StackPanel[resultCount];
                    for (int i = 0; i < resultCount; i++)
                    {
                        LookupResult lookupResult = lookupResults[i];

                        if (!FirstPopupWindow.DictsWithResults.Contains(lookupResult.Dict))
                        {
                            FirstPopupWindow.DictsWithResults.Add(lookupResult.Dict);
                        }

                        popupItemSource[i] = FirstPopupWindow.MakeResultStackPanel(lookupResult, i, lookupResults.Count);
                    }

                    PopupWindow.StackPanelCache.AddReplace(text, popupItemSource);
                }
            }
        }
    }

    private void ClipboardChanged(object? sender, EventArgs? e)
    {
        if (!ConfigManager.CaptureTextFromClipboard)
        {
            return;
        }

        DateTime currentTime = new(Stopwatch.GetTimestamp());

        if ((currentTime - _lastClipboardChangeTime).TotalMilliseconds > 5)
        {
            _lastClipboardChangeTime = currentTime;
            CopyFromClipboard();
        }
    }

    public void MainTextBox_MouseMove(object? sender, MouseEventArgs? e)
    {
        if (ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnLeftClickOnly
            || MainTextboxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || FirstPopupWindow.MiningMode
            || (ConfigManager.RequireLookupKeyPress && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture))
            || (!ConfigManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On))
        {
            return;
        }

        _stopPrecache = true;
        FirstPopupWindow.TextBox_MouseMove(MainTextBox);
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        Application.Current.Shutdown();
    }

    private void ShowPreviousBacklogItem()
    {
        if (FirstPopupWindow.MiningMode)
        {
            return;
        }

        if (_currentTextIndex > 0)
        {
            --_currentTextIndex;
            MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        MainTextBox.Text = _backlog[_currentTextIndex];
    }

    private void ShowNextBacklogItem()
    {
        if (_currentTextIndex < _backlog.Count - 1)
        {
            ++_currentTextIndex;
            MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
        }

        if (_currentTextIndex == _backlog.Count - 1)
        {
            MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
        }

        MainTextBox.Text = _backlog[_currentTextIndex];
    }

    private void MainTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.Delta > 0)
            {
                FontSizeSlider.Value += 5;
            }

            else if (e.Delta < 0)
            {
                FontSizeSlider.Value -= 5;
            }
        }

        else if (ConfigManager.SteppedBacklogWithMouseWheel)
        {
            if (e.Delta > 0)
            {
                ShowPreviousBacklogItem();
            }

            else if (e.Delta < 0)
            {
                ShowNextBacklogItem();
            }
        }

        else if (e.Delta > 0)
        {
            string allBacklogText = string.Join("\n", _backlog);
            if (MainTextBox.Text != allBacklogText)
            {
                if (MainTextBox.GetFirstVisibleLineIndex() is 0)
                {
                    int caretIndex = allBacklogText.Length - MainTextBox.Text.Length;

                    MainTextBox.Text = allBacklogText;
                    MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;

                    if (caretIndex >= 0)
                    {
                        MainTextBox.CaretIndex = caretIndex;
                    }

                    MainTextBox.ScrollToEnd();
                }
            }
        }
    }

    private void Button_MouseEnter(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).Foreground = Brushes.SteelBlue;
    }

    private void Button_MouseLeave(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).Foreground = Brushes.White;

        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || (Background.Opacity is 0 && !ConfigManager.GlobalHotKeys))
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void OpacityButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (Background.Opacity is 0)
        {
            Background.Opacity = OpacitySlider.Value / 100;
            _ = MainTextBox.Focus();
        }

        else if (OpacitySlider.Visibility is Visibility.Collapsed)
        {
            OpacitySlider.Visibility = Visibility.Visible;
            _ = OpacitySlider.Focus();
        }

        else
        {
            OpacitySlider.Visibility = Visibility.Collapsed;
        }
    }

    private void FontSizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpacitySlider.Visibility = Visibility.Collapsed;

        if (FontSizeSlider.Visibility is Visibility.Collapsed)
        {
            FontSizeSlider.Visibility = Visibility.Visible;
            _ = FontSizeSlider.Focus();
        }

        else
        {
            FontSizeSlider.Visibility = Visibility.Collapsed;
        }
    }

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ConfigManager.SaveBeforeClosing();

        await Stats.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks).ConfigureAwait(false);
        await Stats.UpdateLifetimeStats().ConfigureAwait(false);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Background.Opacity = OpacitySlider.Value / 100;
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        MainTextBox.FontSize = FontSizeSlider.Value;
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
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
                    WinApi.UnregisterAllHotKeys(WindowHandle);
                }
                else
                {
                    WinApi.RegisterAllHotKeys(WindowHandle);
                }
            }
        }

        if (ConfigManager.DisableHotkeys)
        {
            return;
        }

        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SteppedBacklogBackwardsKeyGesture))
        {
            ShowPreviousBacklogItem();
        }
        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SteppedBacklogForwardsKeyGesture))
        {
            ShowNextBacklogItem();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowPreferencesWindowKeyGesture))
        {
            WindowsUtils.ShowPreferencesWindow();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MousePassThroughModeKeyGesture))
        {
            if (Background.Opacity is not 0)
            {
                Background.Opacity = 0;
                FontSizeSlider.Visibility = Visibility.Collapsed;
                OpacitySlider.Visibility = Visibility.Collapsed;

                if (!ConfigManager.GlobalHotKeys)
                {
                    ShowTitleBarButtons();
                }

                Keyboard.ClearFocus();
            }

            else
            {
                Background.Opacity = OpacitySlider.Value / 100;
                _ = MainTextBox.Focus();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.KanjiModeKeyGesture))
        {
            CoreConfig.KanjiMode = !CoreConfig.KanjiMode;
            FirstPopupWindow.LastText = "";
            Utils.Frontend.InvalidateDisplayCache();
            MainTextBox_MouseMove(null, null);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (DictUtils.DictsReady)
            {
                WindowsUtils.ShowAddNameWindow(MainTextBox.SelectedText);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (DictUtils.DictsReady)
            {
                WindowsUtils.ShowAddWordWindow(MainTextBox.SelectedText);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageDictionariesWindowKeyGesture))
        {
            if (DictUtils.DictsReady
                && !DictUtils.UpdatingJmdict
                && !DictUtils.UpdatingJmnedict
                && !DictUtils.UpdatingKanjidic)
            {
                WindowsUtils.ShowManageDictionariesWindow();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageFrequenciesWindowKeyGesture))
        {
            if (FreqUtils.FreqsReady)
            {
                WindowsUtils.ShowManageFrequenciesWindow();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
            WindowsUtils.UpdateMainWindowVisibility();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MotivationKeyGesture))
        {
            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ClosePopupKeyGesture))
        {
            FirstPopupWindow.HidePopup();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowStatsKeyGesture))
        {
            await WindowsUtils.ShowStatsWindow().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageAudioSourcesWindowKeyGesture))
        {
            WindowsUtils.ShowManageAudioSourcesWindow();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.AlwaysOnTopKeyGesture))
        {
            ConfigManager.AlwaysOnTop = !ConfigManager.AlwaysOnTop;

            Topmost = ConfigManager.AlwaysOnTop;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.TextOnlyVisibleOnHoverKeyGesture))
        {
            ConfigManager.TextOnlyVisibleOnHover = !ConfigManager.TextOnlyVisibleOnHover;

            if (ConfigManager.TextOnlyVisibleOnHover && Background.Opacity is not 0)
            {
                MainGrid.Opacity = IsMouseOver ? 1 : 0;
            }

            else
            {
                MainGrid.Opacity = 1;
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.CaptureTextFromClipboardKeyGesture))
        {
            ConfigManager.CaptureTextFromClipboard = !ConfigManager.CaptureTextFromClipboard;
            if (!CoreConfig.CaptureTextFromWebSocket && !ConfigManager.CaptureTextFromClipboard)
            {
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.CaptureTextFromWebSocketKeyGesture))
        {
            CoreConfig.CaptureTextFromWebSocket = !CoreConfig.CaptureTextFromWebSocket;
            WebSocketUtils.HandleWebSocket();

            if (!CoreConfig.CaptureTextFromWebSocket && !ConfigManager.CaptureTextFromClipboard)
            {
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ReconnectToWebSocketServerKeyGesture))
        {
            CoreConfig.CaptureTextFromWebSocket = true;
            WebSocketUtils.HandleWebSocket();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.TextBoxIsReadOnlyKeyGesture))
        {
            ConfigManager.TextBoxIsReadOnly = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !ConfigManager.TextBoxIsReadOnly;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.DeleteCurrentLineKeyGesture))
        {
            await DeleteCurrentLine().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleMinimizedStateKeyGesture))
        {
            if (!FirstPopupWindow.IsVisible)
            {
                if (ConfigManager.Focusable)
                {
                    WindowState = WindowState is WindowState.Minimized
                        ? WindowState.Normal
                        : WindowState.Minimized;
                }

                else
                {
                    // If another window is not set as active window
                    // Main Window gets activated on restore
                    WinApi.ChangeActiveWindow(FirstPopupWindow.WindowHandle);

                    if (WindowState is WindowState.Minimized)
                    {
                        WinApi.RestoreWindow(WindowHandle);
                    }

                    else
                    {
                        WinApi.MinimizeWindow(WindowHandle);
                    }
                }
            }
        }
    }

    private void ShowTitleBarButtons()
    {
        OpacityButton.Visibility = Visibility.Visible;
        FontSizeButton.Visibility = Visibility.Visible;
        MinimizeButton.Visibility = Visibility.Visible;
        CloseButton.Visibility = Visibility.Visible;
    }

    private void HideTitleBarButtons()
    {
        OpacityButton.Visibility = Visibility.Collapsed;
        FontSizeButton.Visibility = Visibility.Collapsed;
        MinimizeButton.Visibility = Visibility.Collapsed;
        CloseButton.Visibility = Visibility.Collapsed;
    }

    public void ChangeVisibilityOfTitleBarButtons()
    {
        if (Background.Opacity is 0 && !ConfigManager.GlobalHotKeys)
        {
            return;
        }

        if (ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)
        {
            if (TitleBar.IsMouseOver
                || FontSizeButton.IsMouseOver
                || OpacityButton.IsMouseOver
                || MinimizeButton.IsMouseOver
                || CloseButton.IsMouseOver
                || FontSizeSlider.IsVisible
                || OpacitySlider.IsVisible)
            {
                ShowTitleBarButtons();
            }

            else
            {
                HideTitleBarButtons();
            }
        }

        else
        {
            ShowTitleBarButtons();
        }
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        OpacitySlider.Visibility = Visibility.Collapsed;
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (ConfigManager.Focusable)
        {
            WindowState = WindowState.Minimized;
        }
        else
        {
            WinApi.MinimizeWindow(WindowHandle);
        }
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddNameWindow(MainTextBox.SelectedText);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddWordWindow(MainTextBox.SelectedText);
    }

    private void ShowPreferences(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowPreferencesWindow();
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
        WindowsUtils.UpdateMainWindowVisibility();
    }

    private void ShowManageAudioSourcesWindow(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowManageAudioSourcesWindow();
    }

    private void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowManageDictionariesWindow();
    }

    private void ShowManageFrequenciesWindow(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowManageFrequenciesWindow();
    }

    private async void ShowStats(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowStatsWindow().ConfigureAwait(false);
    }

    private void OpacitySlider_LostMouseCapture(object sender, MouseEventArgs e)
    {
        OpacitySlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || FontSizeSlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void OpacitySlider_LostFocus(object sender, RoutedEventArgs e)
    {
        OpacitySlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || FontSizeSlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void FontSizeSlider_LostMouseCapture(object sender, MouseEventArgs e)
    {
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || OpacitySlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void FontSizeSlider_LostFocus(object sender, RoutedEventArgs e)
    {
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || OpacitySlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Size newSize = e.NewSize;
        ConfigManager.MainWindowWidth = newSize.Width;
        ConfigManager.MainWindowHeight = newSize.Height;
    }

    private void MainTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((!ConfigManager.LookupOnSelectOnly && !ConfigManager.LookupOnLeftClickOnly)
            || ConfigManager.InactiveLookupMode
            || FirstPopupWindow.MiningMode
            || (ConfigManager.RequireLookupKeyPress && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        if (ConfigManager.LookupOnSelectOnly)
        {
            FirstPopupWindow.LookupOnSelect(MainTextBox);
        }

        else
        {
            FirstPopupWindow.TextBox_MouseMove(MainTextBox);
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton is MouseButtonState.Pressed && FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            e.Handled = true;
            PopupWindow.PopupWindow_PreviewMouseDown(FirstPopupWindow);
        }

        else
        {
            PopupWindow? currentPopupWindow = FirstPopupWindow;

            while (currentPopupWindow is not null)
            {
                currentPopupWindow.HidePopup();
                currentPopupWindow = currentPopupWindow.ChildPopupWindow;
            }
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs? e)
    {
        Size oldResolution = new(WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX, WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY);
        WindowsUtils.ActiveScreen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        WindowsUtils.Dpi = VisualTreeHelper.GetDpi(this);
        WindowsUtils.DpiAwareWorkAreaWidth = WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareWorkAreaHeight = WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY;

        if (Math.Abs(oldResolution.Width - WindowsUtils.DpiAwareWorkAreaWidth) > 1 || Math.Abs(oldResolution.Height - WindowsUtils.DpiAwareWorkAreaHeight) > 1)
        {
            double ratioX = oldResolution.Width / WindowsUtils.DpiAwareWorkAreaWidth;
            double ratioY = oldResolution.Height / WindowsUtils.DpiAwareWorkAreaHeight;

            double fontScale = ratioX * ratioY > 1
                ? Math.Min(ratioX, ratioY) * 0.75
                : Math.Max(ratioX, ratioY) / 0.75;

            FontSizeSlider.Value = (int)Math.Round(FontSizeSlider.Value / fontScale);

            Left = LeftPositionBeforeResolutionChange / ratioX;
            LeftPositionBeforeResolutionChange = Left;

            Top = TopPositionBeforeResolutionChange / ratioY;
            TopPositionBeforeResolutionChange = Top;

            Width = (int)Math.Round(WidthBeforeResolutionChange / ratioX);
            WidthBeforeResolutionChange = Width;

            Height = (int)Math.Round(HeightBeforeResolutionChange / ratioY);
            HeightBeforeResolutionChange = Height;

            ConfigManager.PopupMaxHeight = (int)Math.Round(ConfigManager.PopupMaxHeight / ratioY);
            ConfigManager.PopupMaxWidth = (int)Math.Round(ConfigManager.PopupMaxWidth / ratioX);
            WindowsUtils.DpiAwarePopupMaxHeight = ConfigManager.PopupMaxHeight / WindowsUtils.Dpi.DpiScaleY;
            WindowsUtils.DpiAwarePopupMaxWidth = ConfigManager.PopupMaxWidth / WindowsUtils.Dpi.DpiScaleX;

            ConfigManager.PopupYOffset = (int)Math.Round(ConfigManager.PopupYOffset / ratioY);
            ConfigManager.PopupXOffset = (int)Math.Round(ConfigManager.PopupXOffset / ratioX);
            WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / WindowsUtils.Dpi.DpiScaleY;

            ConfigManager.FixedPopupYPosition = (int)Math.Round(ConfigManager.FixedPopupYPosition / ratioY);
            ConfigManager.FixedPopupXPosition = (int)Math.Round(ConfigManager.FixedPopupXPosition / ratioX);
            WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / WindowsUtils.Dpi.DpiScaleY;

            PopupWindow? currentPopupWindow = FirstPopupWindow;
            while (currentPopupWindow is not null)
            {
                WindowsUtils.SetSizeToContentForPopup(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, currentPopupWindow);
                currentPopupWindow = currentPopupWindow.ChildPopupWindow;
            }

            if (ConfigManager.AutoAdjustFontSizesOnResolutionChange)
            {
                ConfigManager.AlternativeSpellingsFontSize = (int)Math.Round(ConfigManager.AlternativeSpellingsFontSize / fontScale);
                ConfigManager.DeconjugationInfoFontSize = (int)Math.Round(ConfigManager.DeconjugationInfoFontSize / fontScale);
                ConfigManager.DefinitionsFontSize = (int)Math.Round(ConfigManager.DefinitionsFontSize / fontScale);
                ConfigManager.DictTypeFontSize = (int)Math.Round(ConfigManager.DictTypeFontSize / fontScale);
                ConfigManager.FrequencyFontSize = (int)Math.Round(ConfigManager.FrequencyFontSize / fontScale);
                ConfigManager.PrimarySpellingFontSize = (int)Math.Round(ConfigManager.PrimarySpellingFontSize / fontScale);
                ConfigManager.ReadingsFontSize = (int)Math.Round(ConfigManager.ReadingsFontSize / fontScale);
            }
        }
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        WindowsUtils.Dpi = e.NewDpi;
        WindowsUtils.ActiveScreen = System.Windows.Forms.Screen.FromHandle(WindowHandle);
        WindowsUtils.DpiAwareWorkAreaWidth = WindowsUtils.ActiveScreen.Bounds.Width / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareWorkAreaHeight = WindowsUtils.ActiveScreen.Bounds.Height / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwareFixedPopupXPosition = ConfigManager.FixedPopupXPosition / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareFixedPopupYPosition = ConfigManager.FixedPopupYPosition / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwarePopupMaxWidth = ConfigManager.PopupMaxWidth / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwarePopupMaxHeight = ConfigManager.PopupMaxHeight / e.NewDpi.DpiScaleY;
    }

    private void Border_OnMouseEnter(object sender, MouseEventArgs e)
    {
        // For some reason, when DragMove() is used Mouse.GetPosition() returns Point(0, 0)
        if (e.GetPosition(this) == new Point(0, 0))
        {
            return;
        }

        var border = (Border)sender;

        Mouse.OverrideCursor = border.Name switch
        {
            "LeftBorder" => Cursors.SizeWE,
            "RightBorder" => Cursors.SizeWE,
            "TopBorder" => Cursors.SizeNS,
            "TopRightBorder" => Cursors.SizeNESW,
            "BottomBorder" => Cursors.SizeNS,
            "BottomLeftBorder" => Cursors.SizeNESW,
            "BottomRightBorder" => Cursors.SizeNWSE,
            "TopLeftBorder" => Cursors.SizeNWSE,
            _ => Cursors.Arrow
        };
    }
    private void Border_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (Mouse.LeftButton is MouseButtonState.Released)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

    private void ResizeWindow(object sender, MouseButtonEventArgs e)
    {
        WinApi.ResizeWindow(WindowHandle, ((Border)sender).Name);

        LeftPositionBeforeResolutionChange = Left;
        TopPositionBeforeResolutionChange = Top;
        WidthBeforeResolutionChange = ActualWidth;
        HeightBeforeResolutionChange = ActualHeight;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton is MouseButtonState.Pressed)
        {
            DragMove();
        }

        LeftPositionBeforeResolutionChange = Left;
        TopPositionBeforeResolutionChange = Top;
    }

    private void MainTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ManageDictionariesMenuItem.IsEnabled = DictUtils.DictsReady
                                      && !DictUtils.UpdatingJmdict
                                      && !DictUtils.UpdatingJmnedict
                                      && !DictUtils.UpdatingKanjidic;

        ManageFrequenciesMenuItem.IsEnabled = FreqUtils.FreqsReady;

        AddNameMenuItem.IsEnabled = DictUtils.DictsReady;
        AddWordMenuItem.IsEnabled = DictUtils.DictsReady;

        FirstPopupWindow.HidePopup();
    }

    public async Task ChangeVisibility()
    {
        // Prevents main window background flicker
        await Task.Delay(5).ConfigureAwait(true);

        if (IsMouseOver
            || FirstPopupWindow.IsMouseOver
            || FirstPopupWindow.IsVisible
            || ManageDictionariesWindow.IsItVisible()
            || ManageFrequenciesWindow.IsItVisible()
            || ManageAudioSourcesWindow.IsItVisible()
            || AddNameWindow.IsItVisible()
            || AddWordWindow.IsItVisible()
            || PreferencesWindow.IsItVisible()
            || StatsWindow.IsItVisible()
            || MainTextboxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || Mouse.LeftButton is MouseButtonState.Pressed
            || (!ConfigManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On))
        {
            return;
        }

        if (Background.Opacity is not 0)
        {
            if (ConfigManager.TextOnlyVisibleOnHover)
            {
                MainGrid.Opacity = 0;
            }

            if (ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            {
                Background.Opacity = ConfigManager.MainWindowBackgroundOpacityOnUnhover / 100;
            }
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (IsMouseOver
            || FirstPopupWindow.MiningMode
            || (FirstPopupWindow.IsMouseOver
                && (ConfigManager.FixedPopupPositioning
                    || FirstPopupWindow is { UnavoidableMouseEnter: true })))
        {
            return;
        }

        FirstPopupWindow.HidePopup();

        if (ConfigManager.HighlightLongestMatch)
        {
            WindowsUtils.Unselect(MainTextBox);
        }
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        if (ConfigManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1;
        }

        if (ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }

        if (ConfigManager.Focusable
            && !FirstPopupWindow.IsVisible
            && ConfigManager.MainWindowFocusOnHover)
        {
            _ = Activate();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState is WindowState.Minimized)
        {
            StatsUtils.StatsStopWatch.Stop();

            if (ConfigManager.GlobalHotKeys)
            {
                if (KeyGestureUtils.KeyGestureNameToIntDict.TryGetValue(nameof(ConfigManager.ToggleMinimizedStateKeyGesture), out int id))
                {
                    WinApi.UnregisterAllHotKeys(WindowHandle, id);
                }

                else
                {
                    WinApi.UnregisterAllHotKeys(WindowHandle);
                }
            }
        }

        else
        {
            StatsUtils.StatsStopWatch.Start();

            if (ConfigManager.GlobalHotKeys)
            {
                WinApi.RegisterAllHotKeys(WindowHandle);
            }
        }
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        System.Windows.Forms.Screen newScreen = System.Windows.Forms.Screen.FromHandle(WindowHandle);

        if (WindowsUtils.ActiveScreen.DeviceName != newScreen.DeviceName)
        {
            WindowsUtils.ActiveScreen = System.Windows.Forms.Screen.FromHandle(WindowHandle);
            WindowsUtils.Dpi = VisualTreeHelper.GetDpi(this);
            WindowsUtils.DpiAwareWorkAreaWidth = WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareWorkAreaHeight = WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY;
            WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / WindowsUtils.Dpi.DpiScaleY;
            WindowsUtils.DpiAwareFixedPopupXPosition = ConfigManager.FixedPopupXPosition / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareFixedPopupYPosition = ConfigManager.FixedPopupYPosition / WindowsUtils.Dpi.DpiScaleY;
            WindowsUtils.DpiAwarePopupMaxWidth = ConfigManager.PopupMaxWidth / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwarePopupMaxHeight = ConfigManager.PopupMaxHeight / WindowsUtils.Dpi.DpiScaleY;

            PopupWindow? currentPopupWindow = FirstPopupWindow;
            while (currentPopupWindow is not null)
            {
                WindowsUtils.SetSizeToContentForPopup(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, currentPopupWindow);
                currentPopupWindow = currentPopupWindow.ChildPopupWindow;
            }
        }
    }

    private void TitleBar_MouseEnter(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)
        {
            return;
        }

        ShowTitleBarButtons();
    }

    private void TitleBar_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
            || FontSizeButton.IsMouseOver
            || OpacityButton.IsMouseOver
            || MinimizeButton.IsMouseOver
            || CloseButton.IsMouseOver
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void TitleBarButtonMouseLeave(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || (Background.Opacity is 0 && !ConfigManager.GlobalHotKeys))
        {
            return;
        }

        HideTitleBarButtons();
    }
}
