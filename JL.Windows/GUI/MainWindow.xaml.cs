using System.Configuration;
using System.Globalization;
using System.Runtime.InteropServices;
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
using JL.Core.Profile;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Window = System.Windows.Window;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : Window
{
    private WinApi? _winApi;
    public nint WindowHandle { get; private set; }

    public PopupWindow FirstPopupWindow { get; }

    private static MainWindow? s_instance;
    public static MainWindow Instance => s_instance!;

    public double LeftPositionBeforeResolutionChange { get; set; }
    public double TopPositionBeforeResolutionChange { get; set; }
    public double HeightBeforeResolutionChange { get; set; }
    public double WidthBeforeResolutionChange { get; set; }

    private static ulong s_clipboardSequenceNo;
    public bool ContextMenuIsOpening { get; private set; } // = false;

    private static CancellationTokenSource s_precacheCancellationTokenSource = new();

    private static string? s_lastTextCopiedWhileMinimized;

    private Point _swipeStartPoint;

    private MainWindow()
    {
        InitializeComponent();
        s_instance = this;
        ConfigHelper.Instance.SetLang("en");
        FirstPopupWindow = new PopupWindow();
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        WindowHandle = new WindowInteropHelper(this).Handle;
        _winApi = new WinApi();
        _winApi.ClipboardChanged += ClipboardChanged;
        _winApi.SubscribeToWndProc(this);

        await ProfileUtils.DeserializeProfiles().ConfigureAwait(true);

        ConfigManager.MappedExeConfiguration = new ExeConfigurationFileMap
        {
            ExeConfigFilename = ProfileUtils.GetProfilePath(ProfileUtils.CurrentProfile)
        };

        ConfigManager.ApplyPreferences();

        WinApi.RestoreWindow(WindowHandle);

        await StatsUtils.DeserializeLifetimeStats().ConfigureAwait(true);
        await StatsUtils.DeserializeProfileLifetimeStats().ConfigureAwait(true);

        if (CoreConfig.CaptureTextFromClipboard)
        {
            s_clipboardSequenceNo = WinApi.GetClipboardSequenceNo();
            _ = CopyFromClipboard();
        }

        FirstPopupWindow.Owner = this;

        // Can't use ActivateWindow for FirstPopupWindow if it's not shown at least once
        FirstPopupWindow.Show();
        FirstPopupWindow.HidePopup();

        FocusManager.SetFocusedElement(this, MainTextBox);
        // Makes caret/highlight visible without any mouse click
        MoveCaret(Key.Left);

        await WindowsUtils.InitializeMainWindow().ConfigureAwait(false);
    }

    private bool CopyFromClipboard()
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
                    text = TextUtils.SanitizeText(text);
                    if (text.Length > 0)
                    {
                        if (WindowState is not WindowState.Minimized)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MainTextBox.Text = text;
                                MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
                            }, DispatcherPriority.Send);
                        }

                        HandlePostCopy(text);

                        return true;
                    }
                }
            }
            catch (ExternalException ex)
            {
                Utils.Logger.Warning(ex, "CopyFromClipboard failed");
            }
        }

        return false;
    }

    public async Task CopyFromWebSocket(string text)
    {
        if (!ConfigManager.OnlyCaptureTextWithJapaneseChars || JapaneseUtils.JapaneseRegex.IsMatch(text))
        {
            text = TextUtils.SanitizeText(text);
            if (text.Length > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    if (WindowState is not WindowState.Minimized)
                    {
                        MainTextBox.Text = text;
                        MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
                    }
                }, DispatcherPriority.Send);

                HandlePostCopy(text);

                await Dispatcher.Invoke(async () =>
                {
                    if (ConfigManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket
                        && (!ConfigManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                            || WindowState is WindowState.Minimized))
                    {
                        await FirstPopupWindow.LookupOnCharPosition(MainTextBox, text, 0, true).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
    }

    private void HandlePostCopy(string text)
    {
        Dispatcher.Invoke(() =>
        {
            s_lastTextCopiedWhileMinimized = WindowState is WindowState.Minimized
                ? text
                : null;

            if (SizeToContent is SizeToContent.Manual && (ConfigManager.MainWindowDynamicHeight || ConfigManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContent(ConfigManager.MainWindowDynamicWidth, ConfigManager.MainWindowDynamicHeight, this);
            }

            TitleBarContextMenu.IsOpen = false;
            MainTextBoxContextMenu.IsOpen = false;

            if (ConfigManager.HidePopupsOnTextChange)
            {
                PopupWindowUtils.HidePopups(FirstPopupWindow);
            }
        }, DispatcherPriority.Send);

        if (ConfigManager.AlwaysOnTop
            && !FirstPopupWindow.IsVisible
            && !ManageDictionariesWindow.IsItVisible()
            && !ManageFrequenciesWindow.IsItVisible()
            && !ManageAudioSourcesWindow.IsItVisible()
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible()
            && !PreferencesWindow.IsItVisible()
            && !StatsWindow.IsItVisible()
            && !MainTextBoxContextMenu.IsVisible
            && !TitleBarContextMenu.IsVisible)
        {
            WinApi.BringToFront(WindowHandle);
        }

        BacklogUtils.AddToBacklog(text);

        if (ConfigManager.TextToSpeechOnTextChange
            && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
        {
            _ = SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, text, CoreConfig.AudioVolume).ConfigureAwait(false);
        }

        Stats.IncrementStat(StatType.Lines);

        string strippedText = ConfigManager.StripPunctuationBeforeCalculatingCharacterCount
            ? JapaneseUtils.RemovePunctuation(text)
            : text;

        Stats.IncrementStat(StatType.Characters, new StringInfo(strippedText).LengthInTextElements);

        if (ConfigManager.Precaching && DictUtils.DictsReady
                                     && !DictUtils.UpdatingJmdict && !DictUtils.UpdatingJmnedict && !DictUtils.UpdatingKanjidic
                                     && FreqUtils.FreqsReady
                                     && text.Length < Utils.CacheSize)
        {
            s_precacheCancellationTokenSource.Cancel();
            s_precacheCancellationTokenSource.Dispose();
            s_precacheCancellationTokenSource = new CancellationTokenSource();

            _ = Dispatcher.InvokeAsync(async () => await Precache(text, s_precacheCancellationTokenSource.Token).ConfigureAwait(false), DispatcherPriority.Background);
        }
    }

    private async Task Precache(string input, CancellationToken cancellationToken)
    {
        FirstPopupWindow.DictsWithResults.Clear();

        for (int charPosition = 0; charPosition < input.Length; charPosition++)
        {
            if (charPosition % 10 is 0)
            {
                await Task.Delay(1, CancellationToken.None).ConfigureAwait(true); // let user interact with the GUI while this method is running
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (char.IsLowSurrogate(input[charPosition]))
            {
                continue;
            }

            int endPosition = (input.Length - charPosition) > ConfigManager.MaxSearchLength
                ? JapaneseUtils.FindExpressionBoundary(input[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
                : JapaneseUtils.FindExpressionBoundary(input, charPosition);

            string text = input[charPosition..endPosition];

            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            if (!PopupWindow.StackPanelCache.Contains(text))
            {
                List<LookupResult>? lookupResults = LookupUtils.LookupText(text);
                if (lookupResults?.Count > 0)
                {
                    _ = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.PitchAccentYomichan, out Dict? pitchDict);
                    bool pitchDictIsActive = pitchDict?.Active ?? false;
                    Dict jmdict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
                    bool pOrthographyInfo = jmdict.Options?.POrthographyInfo?.Value ?? true;
                    bool rOrthographyInfo = jmdict.Options?.ROrthographyInfo?.Value ?? true;
                    bool aOrthographyInfo = jmdict.Options?.AOrthographyInfo?.Value ?? true;
                    double pOrthographyInfoFontSize = jmdict.Options?.POrthographyInfoFontSize?.Value ?? 15;

                    int resultCount = Math.Min(lookupResults.Count, ConfigManager.MaxNumResultsNotInMiningMode);
                    StackPanel[] popupItemSource = new StackPanel[resultCount];
                    for (int i = 0; i < resultCount; i++)
                    {
                        LookupResult lookupResult = lookupResults[i];

                        if (!FirstPopupWindow.DictsWithResults.Contains(lookupResult.Dict))
                        {
                            FirstPopupWindow.DictsWithResults.Add(lookupResult.Dict);
                        }

                        popupItemSource[i] = FirstPopupWindow.PrepareResultStackPanel(lookupResult, i, lookupResults.Count, pitchDict, pitchDictIsActive, pOrthographyInfo, rOrthographyInfo, aOrthographyInfo, pOrthographyInfoFontSize);
                    }

                    PopupWindow.StackPanelCache.AddReplace(text, popupItemSource);
                }
            }
        }
    }

    private async void ClipboardChanged(object? sender, EventArgs? e)
    {
        ulong currentClipboardSequenceNo = WinApi.GetClipboardSequenceNo();
        if (s_clipboardSequenceNo != currentClipboardSequenceNo)
        {
            s_clipboardSequenceNo = currentClipboardSequenceNo;
            bool gotTextFromClipboard = CopyFromClipboard();

            if (gotTextFromClipboard
                && ConfigManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard
                && (!ConfigManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                    || WindowState is WindowState.Minimized))
            {
                await FirstPopupWindow.LookupOnCharPosition(MainTextBox, s_lastTextCopiedWhileMinimized ?? MainTextBox.Text, 0, true).ConfigureAwait(false);
            }
        }
    }

    public async void MainTextBox_MouseMove(object? sender, MouseEventArgs? e)
    {
        if (ConfigManager.InactiveLookupMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnMouseClickOnly
            || e?.LeftButton is MouseButtonState.Pressed
            || MainTextBoxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || FirstPopupWindow.MiningMode
            || (!ConfigManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On)
            || (ConfigManager.RequireLookupKeyPress && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        s_precacheCancellationTokenSource.Cancel();
        await FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox).ConfigureAwait(false);
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        Application.Current.Shutdown();
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
                BacklogUtils.ShowPreviousBacklogItem();
            }

            else if (e.Delta < 0)
            {
                BacklogUtils.ShowNextBacklogItem();
            }
        }

        else if (e.Delta > 0)
        {
            BacklogUtils.ShowAllBacklog();
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

    private void OpacityButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.Left)
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
                _ = MainTextBox.Focus();
            }
        }
        else if (e.ChangedButton is MouseButton.Right)
        {
            PopupWindowUtils.HidePopups(FirstPopupWindow);
        }
    }

    private void FontSizeButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.Left)
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
                _ = MainTextBox.Focus();
            }
        }
        else if (e.ChangedButton is MouseButton.Right)
        {
            PopupWindowUtils.HidePopups(FirstPopupWindow);
        }
    }

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        ConfigManager.SaveBeforeClosing();
        Stats.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks);
        await Stats.SerializeLifetimeStats().ConfigureAwait(false);
        await Stats.SerializeProfileLifetimeStats().ConfigureAwait(false);
        await BacklogUtils.WriteBacklog().ConfigureAwait(false);
        DBUtils.SendOptimizePragmaToAllDBs();
        SqliteConnection.ClearAllPools();
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

    public async Task HandleHotKey(KeyGesture keyGesture, KeyEventArgs? e)
    {
        bool handled = false;
        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.DisableHotkeysKeyGesture))
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
                    if (KeyGestureUtils.KeyGestureNameToIntDict.TryGetValue(nameof(ConfigManager.DisableHotkeys), out int id))
                    {
                        WinApi.UnregisterAllHotKeys(WindowHandle, id);
                    }
                    else
                    {
                        WinApi.UnregisterAllHotKeys(WindowHandle);
                    }
                }
                else
                {
                    WinApi.RegisterAllHotKeys(WindowHandle);
                }
            }
        }

        if (ConfigManager.DisableHotkeys || handled)
        {
            return;
        }

        if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SteppedBacklogBackwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowPreviousBacklogItem();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SteppedBacklogForwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowNextBacklogItem();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowPreferencesWindowKeyGesture))
        {
            handled = true;

            WindowsUtils.ShowPreferencesWindow();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MousePassThroughModeKeyGesture))
        {
            handled = true;

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
            handled = true;

            CoreConfig.KanjiMode = !CoreConfig.KanjiMode;
            FirstPopupWindow.LastText = "";
            Utils.Frontend.InvalidateDisplayCache();
            MainTextBox_MouseMove(null, null);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            handled = true;

            bool customNameDictReady = false;
            if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomNameDictionary, out Dict? customNameDict))
            {
                customNameDictReady = customNameDict.Ready;
            }

            bool profileCustomNameDictReady = false;
            if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomNameDictionary, out Dict? profileCustomNameDict))
            {
                profileCustomNameDictReady = profileCustomNameDict.Ready;
            }

            if (customNameDictReady && profileCustomNameDictReady)
            {
                ShowAddNameWindow();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            handled = true;

            bool customWordDictReady = false;
            if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict))
            {
                customWordDictReady = customWordDict.Ready;
            }

            bool profileCustomWordDictReady = false;
            if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict))
            {
                profileCustomWordDictReady = profileCustomWordDict.Ready;
            }

            if (customWordDictReady && profileCustomWordDictReady)
            {
                ShowAddWordWindow();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageDictionariesWindowKeyGesture))
        {
            handled = true;

            if (DictUtils.DictsReady
                && !DictUtils.UpdatingJmdict
                && !DictUtils.UpdatingJmnedict
                && !DictUtils.UpdatingKanjidic)
            {
                await WindowsUtils.ShowManageDictionariesWindow().ConfigureAwait(false);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageFrequenciesWindowKeyGesture))
        {
            handled = true;

            if (FreqUtils.FreqsReady)
            {
                await WindowsUtils.ShowManageFrequenciesWindow().ConfigureAwait(false);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SearchWithBrowserKeyGesture))
        {
            handled = true;

            WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
            WindowsUtils.UpdateMainWindowVisibility();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.InactiveLookupModeKeyGesture))
        {
            handled = true;

            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MotivationKeyGesture))
        {
            handled = true;

            await WindowsUtils.Motivate().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ClosePopupKeyGesture))
        {
            handled = true;

            FirstPopupWindow.HidePopup();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowStatsKeyGesture))
        {
            handled = true;

            WindowsUtils.ShowStatsWindow();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ShowManageAudioSourcesWindowKeyGesture))
        {
            handled = true;

            await WindowsUtils.ShowManageAudioSourcesWindow().ConfigureAwait(false);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.AlwaysOnTopKeyGesture))
        {
            handled = true;

            ConfigManager.AlwaysOnTop = !ConfigManager.AlwaysOnTop;

            Topmost = ConfigManager.AlwaysOnTop;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.CaptureTextFromClipboardKeyGesture))
        {
            handled = true;

            CoreConfig.CaptureTextFromClipboard = !CoreConfig.CaptureTextFromClipboard;
            if (CoreConfig.CaptureTextFromClipboard)
            {
                WinApi.SubscribeToClipboardChanged(WindowHandle);
            }
            else
            {
                WinApi.UnsubscribeFromClipboardChanged(WindowHandle);
            }

            if (!CoreConfig.CaptureTextFromWebSocket && !CoreConfig.CaptureTextFromClipboard)
            {
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else if (!ConfigManager.StopIncreasingTimeStatWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.CaptureTextFromWebSocketKeyGesture))
        {
            handled = true;

            CoreConfig.CaptureTextFromWebSocket = !CoreConfig.CaptureTextFromWebSocket;
            WebSocketUtils.HandleWebSocket();

            if (!CoreConfig.CaptureTextFromWebSocket && !CoreConfig.CaptureTextFromClipboard)
            {
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else if (!ConfigManager.StopIncreasingTimeStatWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ReconnectToWebSocketServerKeyGesture))
        {
            handled = true;

            if (!WebSocketUtils.Connected)
            {
                CoreConfig.CaptureTextFromWebSocket = true;

                if (!StatsUtils.StatsStopWatch.IsRunning)
                {
                    StatsUtils.StatsStopWatch.Start();
                    StatsUtils.StartStatsTimer();
                }

                WebSocketUtils.HandleWebSocket();
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.TextBoxIsReadOnlyKeyGesture))
        {
            handled = true;

            ConfigManager.TextBoxIsReadOnly = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.UndoLimit = ConfigManager.TextBoxIsReadOnly ? 0 : -1;
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.DeleteCurrentLineKeyGesture))
        {
            handled = true;

            BacklogUtils.DeleteCurrentLine();
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleMinimizedStateKeyGesture))
        {
            handled = true;

            PopupWindowUtils.HidePopups(FirstPopupWindow);

            if (ConfigManager.Focusable)
            {
                WindowState = WindowState is WindowState.Minimized
                    ? WindowState.Normal
                    : WindowState.Minimized;
            }

            else
            {
                if (WindowState is WindowState.Minimized)
                {
                    // If another window is not set as active window
                    // Main Window gets activated on restore
                    WinApi.ActivateWindow(FirstPopupWindow.WindowHandle);

                    WinApi.RestoreWindow(WindowHandle);
                }

                else
                {
                    WinApi.MinimizeWindow(WindowHandle);
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.SelectedTextToSpeechKeyGesture))
        {
            handled = true;

            if (SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string selectedText = WindowState is not WindowState.Minimized
                    ? MainTextBox.SelectionLength > 0
                        ? MainTextBox.SelectedText
                        : MainTextBox.Text
                    : s_lastTextCopiedWhileMinimized ?? MainTextBox.Text;

                if (selectedText.Length > 0)
                {
                    await SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, selectedText, CoreConfig.AudioVolume).ConfigureAwait(false);
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MoveCaretLeftKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Left);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MoveCaretRightKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Right);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MoveCaretUpKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Up);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.MoveCaretDownKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Down);
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.LookupTermAtCaretIndexKeyGesture))
        {
            handled = true;

            if (MainTextBox.Text.Length > 0)
            {
                if (ConfigManager.LookupOnSelectOnly && MainTextBox.SelectionLength > 0 && MainTextBox.SelectionStart == MainTextBox.CaretIndex)
                {
                    await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
                }

                else
                {
                    await FirstPopupWindow.LookupOnCharPosition(MainTextBox, MainTextBox.Text, MainTextBox.CaretIndex, true).ConfigureAwait(false);
                }
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.LookupFirstTermKeyGesture))
        {
            handled = true;

            string text = s_lastTextCopiedWhileMinimized ?? MainTextBox.Text;
            if (text.Length > 0)
            {
                await FirstPopupWindow.LookupOnCharPosition(MainTextBox, text, 0, true).ConfigureAwait(false);
            }
        }

        else if (KeyGestureUtils.CompareKeyGestures(keyGesture, ConfigManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            handled = true;

            ConfigManager.AlwaysShowMainTextBoxCaret = !ConfigManager.AlwaysShowMainTextBoxCaret;
            MainTextBox.IsReadOnlyCaretVisible = ConfigManager.AlwaysShowMainTextBoxCaret;
        }

        if (handled && e is not null)
        {
            e.Handled = true;
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
        ShowAddNameWindow();
    }

    public void ShowAddNameWindow()
    {
        string? text = MainTextBox.SelectionLength > 0
            ? MainTextBox.SelectedText
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), ConfigManager.HorizontallyCenterMainWindowText) is not -1
                ? FirstPopupWindow.LastSelectedText
                : null;

        string reading = "";

        if (text is not null && FirstPopupWindow.LastSelectedText is not null && text == FirstPopupWindow.LastSelectedText)
        {
            string[]? readings = FirstPopupWindow.LastLookupResults[0].Readings;
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

    private void AddWord(object sender, RoutedEventArgs e)
    {
        ShowAddWordWindow();
    }

    public void ShowAddWordWindow()
    {
        string? text = MainTextBox.SelectionLength > 0
            ? MainTextBox.SelectedText
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), ConfigManager.HorizontallyCenterMainWindowText) is not -1
                ? FirstPopupWindow.LastSelectedText
                : null;

        WindowsUtils.ShowAddWordWindow(text);
    }

    private void ShowPreferences(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowPreferencesWindow();
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        SearchWithBrowser();
    }

    public void SearchWithBrowser()
    {
        WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
        WindowsUtils.UpdateMainWindowVisibility();
    }

    private async void ShowManageAudioSourcesWindow(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowManageAudioSourcesWindow().ConfigureAwait(false);
    }

    private async void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowManageDictionariesWindow().ConfigureAwait(false);
    }

    private async void ShowManageFrequenciesWindow(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowManageFrequenciesWindow().ConfigureAwait(false);
    }

    private void ShowStats(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowStatsWindow();
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
        if (e.PreviousSize.Width is not 0)
        {
            Size newSize = e.NewSize;
            ConfigManager.MainWindowWidth = newSize.Width;
            ConfigManager.MainWindowHeight = newSize.Height;
        }
    }

    private async void MainTextBox_PreviewMouseUp(object? sender, MouseButtonEventArgs e)
    {
        if (ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress && !KeyGestureUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture))
            || ((!ConfigManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!ConfigManager.LookupOnMouseClickOnly || e.ChangedButton != ConfigManager.LookupOnClickMouseButton)))
        {
            return;
        }

        if (ConfigManager.LookupOnSelectOnly)
        {
            await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
        }

        else
        {
            await FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox).ConfigureAwait(false);
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == ConfigManager.MiningModeMouseButton && FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            e.Handled = true;
            PopupWindowUtils.ShowMiningModeResults(FirstPopupWindow);
        }
        else if (e.ChangedButton is not MouseButton.Right)
        {
            PopupWindowUtils.HidePopups(FirstPopupWindow);
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

            double fontScale = (ratioX * ratioY) > 1
                ? Math.Min(ratioX, ratioY) * 0.75
                : Math.Max(ratioX, ratioY) / 0.75;

            FontSizeSlider.Value = Math.Round(FontSizeSlider.Value / fontScale);

            Left = LeftPositionBeforeResolutionChange / ratioX;
            LeftPositionBeforeResolutionChange = Left;

            Top = TopPositionBeforeResolutionChange / ratioY;
            TopPositionBeforeResolutionChange = Top;

            Width = Math.Round(WidthBeforeResolutionChange / ratioX);
            WidthBeforeResolutionChange = Width;

            Height = Math.Round(HeightBeforeResolutionChange / ratioY);
            HeightBeforeResolutionChange = Height;

            ConfigManager.PopupMaxHeight = Math.Round(ConfigManager.PopupMaxHeight / ratioY);
            ConfigManager.PopupMaxWidth = Math.Round(ConfigManager.PopupMaxWidth / ratioX);
            WindowsUtils.DpiAwarePopupMaxHeight = ConfigManager.PopupMaxHeight / WindowsUtils.Dpi.DpiScaleY;
            WindowsUtils.DpiAwarePopupMaxWidth = ConfigManager.PopupMaxWidth / WindowsUtils.Dpi.DpiScaleX;

            ConfigManager.PopupYOffset = Math.Round(ConfigManager.PopupYOffset / ratioY);
            ConfigManager.PopupXOffset = Math.Round(ConfigManager.PopupXOffset / ratioX);
            WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / WindowsUtils.Dpi.DpiScaleY;

            ConfigManager.FixedPopupYPosition = Math.Round(ConfigManager.FixedPopupYPosition / ratioY);
            ConfigManager.FixedPopupXPosition = Math.Round(ConfigManager.FixedPopupXPosition / ratioX);
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
                ConfigManager.AlternativeSpellingsFontSize = Math.Round(ConfigManager.AlternativeSpellingsFontSize / fontScale);
                ConfigManager.DeconjugationInfoFontSize = Math.Round(ConfigManager.DeconjugationInfoFontSize / fontScale);
                ConfigManager.DefinitionsFontSize = Math.Round(ConfigManager.DefinitionsFontSize / fontScale);
                ConfigManager.DictTypeFontSize = Math.Round(ConfigManager.DictTypeFontSize / fontScale);
                ConfigManager.FrequencyFontSize = Math.Round(ConfigManager.FrequencyFontSize / fontScale);
                ConfigManager.PrimarySpellingFontSize = Math.Round(ConfigManager.PrimarySpellingFontSize / fontScale);
                ConfigManager.ReadingsFontSize = Math.Round(ConfigManager.ReadingsFontSize / fontScale);
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
        if (FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
        }

        // For some reason, when DragMove() is used Mouse.GetPosition() returns Point(0, 0)/default(Point)
        if (e.GetPosition(this) == default)
        {
            return;
        }

        if (LeftBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }
        else if (RightBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }
        else if (TopBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }
        else if (TopRightBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNESW;
        }
        else if (BottomBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }
        else if (BottomLeftBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNESW;
        }
        else if (BottomRightBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNWSE;
        }
        else if (TopLeftBorder == sender)
        {
            Mouse.OverrideCursor = Cursors.SizeNWSE;
        }
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
        if (e.ChangedButton is MouseButton.Left)
        {
            nint wParam;
            if (LeftBorder == sender)
            {
                wParam = 61441;
            }
            else if (RightBorder == sender)
            {
                wParam = 61442;
            }
            else if (TopBorder == sender)
            {
                wParam = 61443;
            }
            else if (TopLeftBorder == sender)
            {
                wParam = 61444;
            }
            else if (TopRightBorder == sender)
            {
                wParam = 61445;
            }
            else if (BottomBorder == sender)
            {
                wParam = 61446;
            }
            else if (BottomLeftBorder == sender)
            {
                wParam = 61447;
            }
            else // if (BottomRightBorder == sender)
            {
                wParam = 61448;
            }

            WinApi.ResizeWindow(WindowHandle, wParam);

            LeftPositionBeforeResolutionChange = Left;
            TopPositionBeforeResolutionChange = Top;
            WidthBeforeResolutionChange = ActualWidth;
            HeightBeforeResolutionChange = ActualHeight;
        }
        else if (e.ChangedButton is MouseButton.Right)
        {
            PopupWindowUtils.HidePopups(FirstPopupWindow);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton is MouseButtonState.Pressed)
        {
            DragMove();
        }

        if (e.ClickCount is 2
            && !ConfigManager.MainWindowDynamicWidth
            && ConfigManager.MainWindowDynamicHeight)
        {
            Left = WindowsUtils.ActiveScreen.Bounds.X;
            Top = WindowsUtils.ActiveScreen.Bounds.Y;
            Width = WindowsUtils.DpiAwareWorkAreaWidth;
            WidthBeforeResolutionChange = Width;
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

        bool customNameDictReady = false;
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomNameDictionary, out Dict? customNameDict))
        {
            customNameDictReady = customNameDict.Ready;
        }

        bool profileCustomNameDictReady = false;
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomNameDictionary, out Dict? profileCustomNameDict))
        {
            profileCustomNameDictReady = profileCustomNameDict.Ready;
        }

        AddNameMenuItem.IsEnabled = customNameDictReady && profileCustomNameDictReady;

        bool customWordDictReady = false;
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict))
        {
            customWordDictReady = customWordDict.Ready;
        }

        bool profileCustomWordDictReady = false;
        if (DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict))
        {
            profileCustomWordDictReady = profileCustomWordDict.Ready;
        }

        AddWordMenuItem.IsEnabled = customWordDictReady && profileCustomWordDictReady;

        int charIndex = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), ConfigManager.HorizontallyCenterMainWindowText);
        ContextMenuIsOpening = charIndex >= MainTextBox.SelectionStart && charIndex <= (MainTextBox.SelectionStart + MainTextBox.SelectionLength);

        PopupWindowUtils.HidePopups(FirstPopupWindow);

        if (!ContextMenuIsOpening && MainTextBox.SelectionLength > 0)
        {
            WindowsUtils.Unselect(MainTextBox);
        }

        ContextMenuIsOpening = false;
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
            || MainTextBoxContextMenu.IsVisible
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
                    || FirstPopupWindow.UnavoidableMouseEnter)))
        {
            return;
        }

        FirstPopupWindow.HidePopup();
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
            if (ConfigManager.StopIncreasingTimeStatWhenMinimized)
            {
                StatsUtils.StatsStopWatch.Stop();
            }

            if (ConfigManager.GlobalHotKeys)
            {
                List<int> keyGestureIdsToIgnore = new(KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized.Length);
                for (int i = 0; i < KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized.Length; i++)
                {
                    if (KeyGestureUtils.KeyGestureNameToIntDict.TryGetValue(KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized[i], out int id))
                    {
                        keyGestureIdsToIgnore.Add(id);
                    }
                }

                if (keyGestureIdsToIgnore.Count > 0)
                {
                    WinApi.UnregisterAllHotKeys(WindowHandle, keyGestureIdsToIgnore);
                }
                else
                {
                    WinApi.UnregisterAllHotKeys(WindowHandle);
                }
            }
        }

        else
        {
            if (s_lastTextCopiedWhileMinimized is not null)
            {
                MainTextBox.Text = s_lastTextCopiedWhileMinimized;
                MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
            }

            if (ConfigManager.StopIncreasingTimeStatWhenMinimized
                && (CoreConfig.CaptureTextFromClipboard || (CoreConfig.CaptureTextFromWebSocket && WebSocketUtils.Connected)))
            {
                StatsUtils.StatsStopWatch.Start();
            }

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
        if (FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
        }

        if (ConfigManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)
        {
            ShowTitleBarButtons();
        }
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

    private void MoveCaret(Key key)
    {
        WinApi.ActivateWindow(WindowHandle);

        MainTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(MainTextBox)!, 0, key)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
    }

    private void Swipe(Point currentPosition)
    {
        //Swipe down
        if (MainTextBox.VerticalOffset is 0
            && currentPosition.Y > (_swipeStartPoint.Y + 50))
        {
            BacklogUtils.ShowPreviousBacklogItem();
        }

        //Swipe up
        else if (MainTextBox.GetLastVisibleLineIndex() == (MainTextBox.LineCount - 1)
            && currentPosition.Y < (_swipeStartPoint.Y - 50))
        {
            BacklogUtils.ShowNextBacklogItem();
        }
    }

    private void MainTextBox_PreviewTouchDown(object sender, TouchEventArgs e)
    {
        _swipeStartPoint = e.GetTouchPoint(this).Position;
    }

    private void MainTextBox_PreviewTouchUp(object sender, TouchEventArgs e)
    {
        if (!FirstPopupWindow.IsVisible && MainTextBox.SelectionLength is 0)
        {
            Swipe(e.GetTouchPoint(this).Position);
        }
    }

    private void TitleBar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        PopupWindowUtils.HidePopups(FirstPopupWindow);
    }
}
