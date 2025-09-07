using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Tools;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.External;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Lookup;
using JL.Core.Network.WebSocket;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.External;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed partial class MainWindow
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
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
    public bool ContextMenuIsOpening { get; private set; } // = false;
    private bool _contextMenuIsClosed = true;
    public bool MouseEnterDueToFirstPopupHide { get; set; } // = false;
    private Point _swipeStartPoint;
    private InputMethod? _input;
    private static long s_lastTextCopyTimestamp;
    private static DpiScale? s_previousDpi;
    private Point _lastMouseMovePosition;

    public MainWindow()
    {
        s_instance = this;
        InitializeComponent();
        ConfigHelper.Instance.SetLang("en");
        FirstPopupWindow = new PopupWindow(0);
        FrontendManager.Frontend = new WindowsFrontend();
    }

    // ReSharper disable once AsyncVoidMethod
    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowHandle = new WindowInteropHelper(this).Handle;
        WinApi.SetCompositedAndNoRedirectionBitmapStyle(WindowHandle);

        _winApi = new WinApi();
        _winApi.ClipboardChanged += ClipboardChanged;
        _winApi.SubscribeToWndProc(this);

        _input = InputMethod.Current;
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        MagpieUtils.RegisterToMagpieScalingChangedMessage(WindowHandle);
        MagpieUtils.MarkWindowAsMagpieToolWindow(WindowHandle);

        ConfigDBManager.CreateDB();

        SqliteConnection readOnlyConnection = ConfigDBManager.CreateReadOnlyDBConnection();
        await using (readOnlyConnection.ConfigureAwait(true))
        {
            ProfileDBUtils.SetCurrentProfileFromDB(readOnlyConnection);
            StatsDBUtils.SetStatsFromDB(readOnlyConnection);
        }

        ConfigManager configManager = ConfigManager.Instance;
        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(true))
        {
            configManager.ApplyPreferences(connection);
        }

        RegexReplacerUtils.PopulateRegexReplacements();

        if (configManager.AlwaysOnTop)
        {
            WinApi.BringToFront(WindowHandle);
        }

        if (CoreConfigManager.Instance.CaptureTextFromClipboard)
        {
            _ = await CopyFromClipboard().ConfigureAwait(true);
        }

        FirstPopupWindow.Owner = this;

        // Can't use ActivateWindow for FirstPopupWindow if it's not shown at least once
        FirstPopupWindow.Show();
        FirstPopupWindow.HidePopup();

        FocusManager.SetFocusedElement(this, MainTextBox);
        // Makes caret/highlight visible without any mouse click
        MoveCaret(Key.Left);

        nint magpieWindowHandle = MagpieUtils.GetMagpieWindowHandle();
        MagpieUtils.IsMagpieScaling = magpieWindowHandle is not 0;
        if (MagpieUtils.IsMagpieScaling)
        {
            MagpieUtils.SetMagpieInfo(magpieWindowHandle);
        }

        await WindowsUtils.InitializeMainWindow().ConfigureAwait(false);
    }

    private async Task<bool> CopyFromClipboard()
    {
        while (Clipboard.ContainsText())
        {
            try
            {
                return CopyText(Clipboard.GetText());
            }
            catch (ExternalException ex)
            {
                LoggerManager.Logger.Warning(ex, "CopyFromClipboard failed");
                await Task.Delay(5).ConfigureAwait(true);
            }
        }

        return false;
    }

    public Task CopyFromWebSocket(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        return Dispatcher.BeginInvoke(async () =>
        {
            if (CopyText(text)
                && configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket
                && (!configManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                    || WindowState is WindowState.Minimized))
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                {
                    await MpvUtils.PausePlayback().ConfigureAwait(true);
                }

                MoveWindowToScreen();
                await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true, true).ConfigureAwait(false);
            }
        }, DispatcherPriority.Send).Task;
    }

    private bool CopyText(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;

        if (text.Length is 0)
        {
            MainTextBox.Clear();
            UpdatePosition();
            if (configManager.AlwaysOnTop)
            {
                WinApi.BringToFront(WindowHandle);
            }

            return false;
        }

        if (configManager.OnlyCaptureTextWithJapaneseChars && !JapaneseUtils.ContainsJapaneseCharacters(text))
        {
            return false;
        }

        string sanitizedNewText = TextUtils.SanitizeText(text);
        if (sanitizedNewText.Length is 0)
        {
            return false;
        }

        bool mergeTexts = false;
        string? subsequentText = null;
        string? mergedText = null;

        string previousText = MainTextBox.Text;
        bool sameText = sanitizedNewText == previousText;
        if (configManager.DiscardIdenticalText && sameText)
        {
            if (configManager.MergeSequentialTextsWhenTheyMatch)
            {
                s_lastTextCopyTimestamp = Stopwatch.GetTimestamp();
            }

            return configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket || configManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard;
        }

        if (configManager.MergeSequentialTextsWhenTheyMatch)
        {
            mergeTexts = (configManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds is 0
                          || Stopwatch.GetElapsedTime(s_lastTextCopyTimestamp).TotalMilliseconds <= configManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds)
                            && previousText.Length > 0;

            s_lastTextCopyTimestamp = Stopwatch.GetTimestamp();

            if (mergeTexts)
            {
                if (!configManager.DiscardIdenticalText && sameText)
                {
                    return configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket || configManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard;
                }

                if (!configManager.AllowPartialMatchingForTextMerge)
                {
                    if (sanitizedNewText.AsSpan().StartsWith(previousText, StringComparison.Ordinal))
                    {
                        subsequentText = sanitizedNewText[previousText.Length..];
                    }
                }
                else
                {
                    int startIndex = Math.Max(previousText.Length - sanitizedNewText.Length, 0);
                    for (int i = startIndex; i < previousText.Length; i++)
                    {
                        ReadOnlySpan<char> sanitizedNewTextSpan = sanitizedNewText.AsSpan();
                        ReadOnlySpan<char> previousTextSlice = previousText.AsSpan(i);
                        if (sanitizedNewTextSpan.StartsWith(previousTextSlice, StringComparison.Ordinal))
                        {
                            subsequentText = sanitizedNewTextSpan.Length == previousTextSlice.Length
                                ? null
                                : sanitizedNewText[(previousText.Length - i)..];

                            break;
                        }
                    }
                }
            }
        }

        mergeTexts = mergeTexts && subsequentText is not null;
        if (mergeTexts)
        {
            MainTextBox.AppendText(subsequentText);
            mergedText = MainTextBox.Text;
        }
        else
        {
            MainTextBox.Text = sanitizedNewText;
        }

        MainTextBox.Foreground = configManager.MainWindowTextColor;

        bool notMinimized = WindowState is not WindowState.Minimized;
        if (!mergeTexts && SizeToContent is SizeToContent.Manual && notMinimized
                        && (configManager.MainWindowDynamicHeight || configManager.MainWindowDynamicWidth))
        {
            SetSizeToContent(configManager.MainWindowDynamicWidth, configManager.MainWindowDynamicHeight, configManager.MainWindowMaxDynamicWidth, configManager.MainWindowMaxDynamicHeight, configManager.MainWindowMinDynamicWidth, configManager.MainWindowMinDynamicHeight, configManager.MainWindowWidth, configManager.MainWindowHeight);
        }

        TitleBarContextMenu.IsOpen = false;
        MainTextBoxContextMenu.IsOpen = false;

        if (configManager.HidePopupsOnTextChange
            && FirstPopupWindow.IsVisible
            && (!sameText || (PopupWindowUtils.PopupWindows[1]?.IsVisible ?? false)))
        {
            PopupWindowUtils.HidePopups(0);
        }

        if (notMinimized)
        {
            UpdatePosition();
            if (configManager.AlwaysOnTop)
            {
                WinApi.BringToFront(WindowHandle);
            }
        }

        if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || notMinimized)
        {
            StatsUtils.StartTimeStatStopWatch();
            StatsUtils.SetIdleTimeTimerInterval(mergedText?.Length ?? sanitizedNewText.Length);

            string strippedText = configManager.StripPunctuationBeforeCalculatingCharacterCount
                ? JapaneseUtils.RemovePunctuation(subsequentText ?? sanitizedNewText)
                : subsequentText ?? sanitizedNewText;

            if (strippedText.Length > 0)
            {
                StatsUtils.IncrementStat(StatType.Characters, new StringInfo(strippedText).LengthInTextElements);

                if (!mergeTexts)
                {
                    StatsUtils.IncrementStat(StatType.Lines);
                }
            }
        }

        if (configManager.MaxBacklogCapacity is not 0)
        {
            if (mergeTexts)
            {
                Debug.Assert(mergedText is not null);
                BacklogUtils.ReplaceLastBacklogText(mergedText);
            }
            else
            {
                BacklogUtils.AddToBacklog(sanitizedNewText);
            }
        }

        if (configManager.TextToSpeechOnTextChange
            && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
        {
            _ = SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, sanitizedNewText).ConfigureAwait(false);
        }

        return true;
    }

    public void BringToFront()
    {
        if (ConfigManager.Instance.AlwaysOnTop
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
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ClipboardChanged(object? sender, EventArgs e)
    {
        bool gotTextFromClipboard = await CopyFromClipboard().ConfigureAwait(true);

        ConfigManager configManager = ConfigManager.Instance;
        if (gotTextFromClipboard
            && configManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard
            && (!configManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                || WindowState is WindowState.Minimized))
        {
            if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
            {
                await MpvUtils.PausePlayback().ConfigureAwait(true);
            }

            MoveWindowToScreen();
            await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true, true).ConfigureAwait(false);
        }
    }

    private Task HandleMouseMove(MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        return configManager.InactiveLookupMode
               || configManager.LookupOnSelectOnly
               || configManager.LookupOnMouseClickOnly
               || e.LeftButton is MouseButtonState.Pressed
               || MainTextBoxContextMenu.IsVisible
               || TitleBarContextMenu.IsVisible
               || FontSizeSlider.IsVisible
               || OpacitySlider.IsVisible
               || FirstPopupWindow.MiningMode
               || (!configManager.TextBoxIsReadOnly && _input?.ImeState is InputMethodState.On)
               || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            ? Task.CompletedTask
            : FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox, false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MainTextBox_MouseMove(object sender, MouseEventArgs e)
    {
        // WPF sometimes triggers MouseMove even when the mouse is outside the window.
        // In this state, IsMouseOver still returns true, which is likely a WPF bug.
        // During this time, e.GetPosition stops updating and always returns the same value.
        // To work around this, we check whether the position has actually changed to ensure MouseMove is being triggered correctly.
        Point position = e.GetPosition(MainTextBox);
        if (position != _lastMouseMovePosition)
        {
            _lastMouseMovePosition = position;
            await HandleMouseMove(e).ConfigureAwait(false);
        }
        else if (FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
            ChangeVisibility();
        }
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

        else if (ConfigManager.Instance.SteppedBacklogWithMouseWheel)
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

        ConfigManager configManager = ConfigManager.Instance;
        if (!configManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || (Background.Opacity is 0 && !configManager.GlobalHotKeys))
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
            if (FirstPopupWindow.IsVisible)
            {
                PopupWindowUtils.HidePopups(0);
            }
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
            if (FirstPopupWindow.IsVisible)
            {
                PopupWindowUtils.HidePopups(0);
            }
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        await HandleAppClosing().ConfigureAwait(false);
    }

    public async Task HandleAppClosing()
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        MagpieUtils.UnmarkWindowAsMagpieToolWindow(WindowHandle);

        Debug.Assert(_winApi is not null);
        _winApi.UnsubscribeFromWndProc(this);

        await WebSocketUtils.DisconnectFromAllWebSocketConnections().ConfigureAwait(true);

        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(false))
        {
            ConfigManager.Instance.SaveBeforeClosing(connection);
            StatsUtils.IncrementStat(StatType.Time, StatsUtils.TimeStatStopWatch.ElapsedTicks);
            StatsDBUtils.UpdateLifetimeStats(connection);
            StatsDBUtils.UpdateProfileLifetimeStats(connection);
        }

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
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (keyGesture.IsEqual(configManager.DisableHotkeysKeyGesture))
        {
            configManager.DisableHotkeys = !configManager.DisableHotkeys;

            if (configManager.GlobalHotKeys)
            {
                if (configManager.DisableHotkeys)
                {
                    int disableHotkeysKeyGestureId = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict.IndexOf(nameof(configManager.DisableHotkeys));
                    if (disableHotkeysKeyGestureId >= 0)
                    {
                        WinApi.UnregisterAllGlobalHotKeys(WindowHandle, disableHotkeysKeyGestureId);
                    }
                    else
                    {
                        WinApi.UnregisterAllGlobalHotKeys(WindowHandle);
                    }
                }
                else
                {
                    WinApi.RegisterAllGlobalHotKeys(WindowHandle);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.SteppedBacklogBackwardsKeyGesture))
        {
            BacklogUtils.ShowPreviousBacklogItem();
        }

        else if (keyGesture.IsEqual(configManager.SteppedBacklogForwardsKeyGesture))
        {
            BacklogUtils.ShowNextBacklogItem();
        }

        else if (keyGesture.IsEqual(configManager.ShowPreferencesWindowKeyGesture))
        {
            if (PreferencesMenuItem.IsEnabled)
            {
                WindowsUtils.ShowPreferencesWindow();
            }
        }

        else if (keyGesture.IsEqual(configManager.MousePassThroughModeKeyGesture))
        {
            if (Background.Opacity is not 0)
            {
                Background.Opacity = 0d;
                FontSizeSlider.Visibility = Visibility.Collapsed;
                OpacitySlider.Visibility = Visibility.Collapsed;

                if (!configManager.GlobalHotKeys)
                {
                    ShowTitleBarButtons();
                }

                Keyboard.ClearFocus();
            }

            else
            {
                Background.Opacity = OpacitySlider.Value / 100;
                _ = MainTextBox.Focus();
                ChangeVisibility();
            }
        }

        else if (keyGesture.IsEqual(configManager.KanjiModeKeyGesture))
        {
            coreConfigManager.LookupCategory = coreConfigManager.LookupCategory is LookupCategory.Kanji
                ? LookupCategory.All
                : LookupCategory.Kanji;
        }

        else if (keyGesture.IsEqual(configManager.NameModeKeyGesture))
        {
            coreConfigManager.LookupCategory = coreConfigManager.LookupCategory is LookupCategory.Name
                ? LookupCategory.All
                : LookupCategory.Name;
        }

        else if (keyGesture.IsEqual(configManager.WordModeKeyGesture))
        {
            coreConfigManager.LookupCategory = coreConfigManager.LookupCategory is LookupCategory.Word
                ? LookupCategory.All
                : LookupCategory.Word;
        }

        else if (keyGesture.IsEqual(configManager.OtherModeKeyGesture))
        {
            coreConfigManager.LookupCategory = coreConfigManager.LookupCategory is LookupCategory.Other
                ? LookupCategory.All
                : LookupCategory.Other;
        }

        else if (keyGesture.IsEqual(configManager.AllModeKeyGesture))
        {
            coreConfigManager.LookupCategory = LookupCategory.All;
        }

        else if (keyGesture.IsEqual(configManager.ShowAddNameWindowKeyGesture))
        {
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

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
        {
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

        else if (keyGesture.IsEqual(configManager.ShowManageDictionariesWindowKeyGesture))
        {
            if (DictUtils.DictsReady
                && DictUtils.Dicts.Values.ToArray().All(static dict => dict.Ready))
            {
                return WindowsUtils.ShowManageDictionariesWindow();
            }
        }

        else if (keyGesture.IsEqual(configManager.ShowManageFrequenciesWindowKeyGesture))
        {
            if (FreqUtils.FreqsReady)
            {
                return WindowsUtils.ShowManageFrequenciesWindow();
            }
        }

        else if (keyGesture.IsEqual(configManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
        }

        else if (keyGesture.IsEqual(configManager.InactiveLookupModeKeyGesture))
        {
            configManager.InactiveLookupMode = !configManager.InactiveLookupMode;
        }

        else if (keyGesture.IsEqual(configManager.MotivationKeyGesture))
        {
            return WindowsUtils.Motivate();
        }

        else if (keyGesture.IsEqual(configManager.ShowStatsKeyGesture))
        {
            WindowsUtils.ShowStatsWindow();
        }

        else if (keyGesture.IsEqual(configManager.ShowManageAudioSourcesWindowKeyGesture))
        {
            return WindowsUtils.ShowManageAudioSourcesWindow();
        }

        else if (keyGesture.IsEqual(configManager.AlwaysOnTopKeyGesture))
        {
            configManager.AlwaysOnTop = !configManager.AlwaysOnTop;

            Topmost = configManager.AlwaysOnTop;
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromClipboardKeyGesture))
        {
            coreConfigManager.CaptureTextFromClipboard = !coreConfigManager.CaptureTextFromClipboard;
            if (coreConfigManager.CaptureTextFromClipboard)
            {
                WinApi.SubscribeToClipboardChanged(WindowHandle);
            }
            else
            {
                WinApi.UnsubscribeFromClipboardChanged(WindowHandle);
            }

            if (coreConfigManager is { CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
            {
                StatsUtils.StopTimeStatStopWatch();
            }
            else if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StartTimeStatStopWatch();
            }
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromWebSocketKeyGesture))
        {
            coreConfigManager.CaptureTextFromWebSocket = !coreConfigManager.CaptureTextFromWebSocket;
            if (coreConfigManager is { CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
            {
                StatsUtils.StopTimeStatStopWatch();
            }
            else if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StartTimeStatStopWatch();
            }

            if (coreConfigManager.CaptureTextFromWebSocket)
            {
                WebSocketUtils.ConnectToAllWebSockets();
            }
            else
            {
                return WebSocketUtils.DisconnectFromAllWebSocketConnections();
            }
        }

        else if (keyGesture.IsEqual(configManager.ReconnectToWebSocketServerKeyGesture))
        {
            coreConfigManager.CaptureTextFromWebSocket = true;

            WebSocketUtils.ConnectToAllWebSockets();
            if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
            {
                if (!StatsUtils.TimeStatStopWatch.IsRunning)
                {
                    StatsUtils.StartTimeStatStopWatch();
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.TextBoxIsReadOnlyKeyGesture))
        {
            configManager.TextBoxIsReadOnly = !configManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = configManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !configManager.TextBoxIsReadOnly;
            MainTextBox.UndoLimit = configManager.TextBoxIsReadOnly ? 0 : -1;
        }

        else if (keyGesture.IsEqual(configManager.DeleteCurrentLineKeyGesture))
        {
            BacklogUtils.DeleteCurrentLine();
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            if (FirstPopupWindow.IsVisible)
            {
                PopupWindowUtils.HidePopups(0);
            }

            if (configManager.Focusable)
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

                    if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                    {
                        _ = MpvUtils.ResumePlayback();
                    }
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectedTextToSpeechKeyGesture))
        {
            if (SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string selectedText = MainTextBox.SelectionLength > 0
                        ? MainTextBox.SelectedText
                        : MainTextBox.Text;

                if (selectedText.Length > 0)
                {
                    return SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, selectedText);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretLeftKeyGesture))
        {
            MoveCaret(Key.Left);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretRightKeyGesture))
        {
            MoveCaret(Key.Right);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretUpKeyGesture))
        {
            MoveCaret(Key.Up);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretDownKeyGesture))
        {
            MoveCaret(Key.Down);
        }

        else if (keyGesture.IsEqual(configManager.LookupTermAtCaretIndexKeyGesture))
        {
            if (MainTextBox.Text.Length > 0)
            {
                if (configManager.LookupOnSelectOnly && MainTextBox.SelectionLength > 0 && MainTextBox.SelectionStart == MainTextBox.CaretIndex)
                {
                    if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                    {
                        _ = MpvUtils.PausePlayback();
                    }

                    return FirstPopupWindow.LookupOnSelect(MainTextBox);
                }

                if (MainTextBox.Text.Length > MainTextBox.CaretIndex)
                {
                    if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                    {
                        _ = MpvUtils.PausePlayback();
                    }

                    MoveWindowToScreen();
                    return FirstPopupWindow.LookupOnCharPosition(MainTextBox, MainTextBox.CaretIndex, true, true);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.LookupFirstTermKeyGesture))
        {
            if (MainTextBox.Text.Length > 0)
            {
                MoveWindowToScreen();
                return FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true, true);
            }
        }

        else if (keyGesture.IsEqual(configManager.LookupSelectedTextKeyGesture))
        {
            return FirstPopupWindow.LookupOnSelect(MainTextBox);
        }

        else if (keyGesture.IsEqual(configManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            configManager.AlwaysShowMainTextBoxCaret = !configManager.AlwaysShowMainTextBoxCaret;
            MainTextBox.IsReadOnlyCaretVisible = configManager.AlwaysShowMainTextBoxCaret;
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.CtrlCKeyGesture))
        {
            if (MainTextBox.SelectedText.Length > 0)
            {
                return WindowsUtils.CopyTextToClipboard(MainTextBox.SelectedText);
            }
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            Close();
        }

        return Task.CompletedTask;
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
        ConfigManager configManager = ConfigManager.Instance;
        if (Background.Opacity is 0 && !configManager.GlobalHotKeys)
        {
            return;
        }

        if (configManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)
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

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.Focusable)
        {
            WindowState = WindowState.Minimized;
        }
        else
        {
            WinApi.MinimizeWindow(WindowHandle);
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            _ = MpvUtils.ResumePlayback();
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
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false) >= 0
              || (FirstPopupWindow.LastSelectedText is not null
                  && MainTextBox.Text.AsSpan().StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
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

        WindowsUtils.ShowAddNameWindow(this, text, reading);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        ShowAddWordWindow();
    }

    public void ShowAddWordWindow()
    {
        string? text = MainTextBox.SelectionLength > 0
            ? MainTextBox.SelectedText
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false) >= 0
              || (FirstPopupWindow.LastSelectedText is not null
                  && MainTextBox.Text.AsSpan().StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
                ? FirstPopupWindow.LastSelectedText
                : null;

        WindowsUtils.ShowAddWordWindow(this, text);
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
        string? text = MainTextBox.SelectionLength > 0
            ? MainTextBox.SelectedText
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false) >= 0
              || (FirstPopupWindow.LastSelectedText is not null
                  && MainTextBox.Text.AsSpan().StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
                ? FirstPopupWindow.LastSelectedText
                : null;

        WindowsUtils.SearchWithBrowser(text);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ShowManageAudioSourcesWindow(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowManageAudioSourcesWindow().ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
    {
        await WindowsUtils.ShowManageDictionariesWindow().ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
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

        if (!ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || FontSizeSlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void OpacitySlider_LostFocus(object sender, RoutedEventArgs e)
    {
        OpacitySlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || FontSizeSlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void FontSizeSlider_LostMouseCapture(object sender, MouseEventArgs e)
    {
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || OpacitySlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void FontSizeSlider_LostFocus(object sender, RoutedEventArgs e)
    {
        FontSizeSlider.Visibility = Visibility.Collapsed;

        if (!ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar || OpacitySlider.IsVisible)
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
            ConfigManager configManager = ConfigManager.Instance;
            configManager.MainWindowWidth = newSize.Width;
            configManager.MainWindowHeight = newSize.Height;
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MainTextBox_PreviewMouseUp(object? sender, MouseButtonEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
            || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            || ((!configManager.LookupOnSelectOnly || e.ChangedButton is not MouseButton.Left)
                && (!configManager.LookupOnMouseClickOnly || e.ChangedButton != configManager.LookupOnClickMouseButton)))
        {
            return;
        }

        if (configManager.LookupOnSelectOnly)
        {
            await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
        }

        else
        {
            await FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox, true).ConfigureAwait(false);
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == ConfigManager.Instance.MiningModeMouseButton && FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            e.Handled = true;
            FirstPopupWindow.ShowMiningModeResults();
        }
        else if (e.ChangedButton is not MouseButton.Right)
        {
            if (FirstPopupWindow.IsVisible)
            {
                PopupWindowUtils.HidePopups(0);
            }
        }
    }

    private void AdjustWindowsSize(Screen previousActiveScreen, DpiScale previousDpi)
    {
        double oldDpiAwareScreenWidth = previousActiveScreen.Bounds.Width / previousDpi.DpiScaleX;
        double oldDpiAwareScreenHeight = previousActiveScreen.Bounds.Height / previousDpi.DpiScaleY;

        DpiScale dpi = WindowsUtils.Dpi;
        Rectangle bounds = WindowsUtils.ActiveScreen.Bounds;
        double newDpiAwareScreenWidth = bounds.Width / dpi.DpiScaleX;
        double newDpiAwareScreenHeight = bounds.Height / dpi.DpiScaleY;

        double ratioX = oldDpiAwareScreenWidth / newDpiAwareScreenWidth;
        double ratioY = oldDpiAwareScreenHeight / newDpiAwareScreenHeight;

        if (ratioX is 1 && ratioY is 1)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        configManager.MainWindowMaxDynamicHeight = Math.Round(configManager.MainWindowMaxDynamicHeight / ratioY);
        configManager.MainWindowMaxDynamicWidth = Math.Round(configManager.MainWindowMaxDynamicWidth / ratioX);
        if (SizeToContent is not SizeToContent.Manual)
        {
            if (configManager.MainWindowDynamicHeight)
            {
                MaxHeight = configManager.MainWindowMaxDynamicHeight;
            }
            if (configManager.MainWindowDynamicWidth)
            {
                MaxWidth = configManager.MainWindowMaxDynamicWidth;
            }
        }

        Width = Math.Round(Math.Min(WidthBeforeResolutionChange / ratioX, newDpiAwareScreenWidth));
        WidthBeforeResolutionChange = Width;

        Height = Math.Round(Math.Min(HeightBeforeResolutionChange / ratioY, newDpiAwareScreenHeight));
        HeightBeforeResolutionChange = Height;

        double newLeft = Math.Round(LeftPositionBeforeResolutionChange * previousDpi.DpiScaleX / ratioX);
        if (bounds.X > newLeft)
        {
            newLeft = bounds.X;
        }
        else if (newLeft > bounds.Right)
        {
            newLeft = bounds.Right - (Width * dpi.DpiScaleX);
        }

        double newTop = Math.Round(TopPositionBeforeResolutionChange * previousDpi.DpiScaleY / ratioY);
        if (bounds.Y > newTop)
        {
            newTop = bounds.Y;
        }
        else if (newTop > bounds.Bottom)
        {
            newTop = bounds.Bottom - (Height * dpi.DpiScaleY);
        }

        WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);

        configManager.PopupMaxHeight = Math.Round(Math.Min(configManager.PopupMaxHeight / ratioY, newDpiAwareScreenHeight));
        configManager.PopupMaxWidth = Math.Round(Math.Min(configManager.PopupMaxWidth / ratioX, newDpiAwareScreenWidth));

        configManager.PopupYOffset = Math.Round(configManager.PopupYOffset / ratioY);
        configManager.PopupXOffset = Math.Round(configManager.PopupXOffset / ratioX);
        WindowsUtils.DpiAwareXOffset = configManager.PopupXOffset * dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = configManager.PopupYOffset * dpi.DpiScaleY;

        configManager.FixedPopupYPosition = Math.Round(configManager.FixedPopupYPosition / ratioY);
        if (bounds.Y > configManager.FixedPopupYPosition)
        {
            configManager.FixedPopupYPosition = bounds.Y;
        }
        else if (configManager.FixedPopupYPosition > bounds.Bottom)
        {
            configManager.FixedPopupYPosition = bounds.Bottom - (configManager.PopupMaxHeight * dpi.DpiScaleY);
        }

        configManager.FixedPopupXPosition = Math.Round(configManager.FixedPopupXPosition / ratioX);
        if (bounds.X > configManager.FixedPopupXPosition)
        {
            configManager.FixedPopupXPosition = bounds.X;
        }
        else if (configManager.FixedPopupXPosition > bounds.Right)
        {
            configManager.FixedPopupXPosition = bounds.Right - (configManager.PopupMaxWidth * dpi.DpiScaleX);
        }

        PopupWindow? currentPopupWindow = FirstPopupWindow;
        while (currentPopupWindow is not null)
        {
            currentPopupWindow.SetSizeToContent(configManager.PopupDynamicWidth, configManager.PopupDynamicHeight, configManager.PopupMaxWidth, configManager.PopupMaxHeight, configManager.PopupMinWidth, configManager.PopupMinHeight);
            currentPopupWindow = PopupWindowUtils.PopupWindows[currentPopupWindow.PopupIndex + 1];
        }

        if (configManager.AutoAdjustFontSizesOnResolutionChange)
        {
            double fontScale = ratioX * ratioY > 1
                ? Math.Min(ratioX, ratioY) * 0.75
                : Math.Max(ratioX, ratioY) / 0.75;

            FontSizeSlider.Value = Math.Round(FontSizeSlider.Value / fontScale);

            configManager.AlternativeSpellingsFontSize = Math.Round(configManager.AlternativeSpellingsFontSize / fontScale);
            configManager.DeconjugationInfoFontSize = Math.Round(configManager.DeconjugationInfoFontSize / fontScale);
            configManager.DefinitionsFontSize = Math.Round(configManager.DefinitionsFontSize / fontScale);
            configManager.DictTypeFontSize = Math.Round(configManager.DictTypeFontSize / fontScale);
            configManager.FrequencyFontSize = Math.Round(configManager.FrequencyFontSize / fontScale);
            configManager.PrimarySpellingFontSize = Math.Round(configManager.PrimarySpellingFontSize / fontScale);
            configManager.ReadingsFontSize = Math.Round(configManager.ReadingsFontSize / fontScale);
            configManager.AudioButtonFontSize = Math.Round(configManager.AudioButtonFontSize / fontScale);
            configManager.MiningButtonFontSize = Math.Round(configManager.MiningButtonFontSize / fontScale);
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs e)
    {
        HandleDisplaySettingsChange();
    }

    private void HandleDisplaySettingsChange()
    {
        ConfigManager configManager = ConfigManager.Instance;

        _ = Dispatcher.Invoke(DispatcherPriority.Background, () =>
        {
            Screen previousActiveScreen = WindowsUtils.ActiveScreen;

            WindowsUtils.ActiveScreen = Screen.FromHandle(WindowHandle);
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            WindowsUtils.Dpi = dpi;
            WindowsUtils.DpiAwareXOffset = configManager.PopupXOffset * dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = configManager.PopupYOffset * dpi.DpiScaleY;

            Rectangle previousScreenBonds = previousActiveScreen.Bounds;
            Rectangle currentScreenBounds = WindowsUtils.ActiveScreen.Bounds;
            if (Math.Abs(previousScreenBonds.Width - currentScreenBounds.Width) <= 1 && Math.Abs(previousScreenBonds.Height - currentScreenBounds.Height) <= 1)
            {
                return;
            }

            DpiScale previousDpi = s_previousDpi ?? dpi;
            s_previousDpi = null;

            AdjustWindowsSize(previousActiveScreen, previousDpi);
        });
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        WindowsUtils.Dpi = e.NewDpi;
        WindowsUtils.DpiAwareXOffset = configManager.PopupXOffset * e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = configManager.PopupYOffset * e.NewDpi.DpiScaleY;
        s_previousDpi = e.OldDpi;
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        if (WindowsUtils.ActiveScreen.DeviceName == Screen.FromHandle(WindowHandle).DeviceName)
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        WindowsUtils.ActiveScreen = Screen.FromHandle(WindowHandle);
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        WindowsUtils.Dpi = dpi;
        WindowsUtils.DpiAwareXOffset = configManager.PopupXOffset * dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = configManager.PopupYOffset * dpi.DpiScaleY;
    }

    private void Border_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1d;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }

        // For some reason, when DragMove() is used Mouse.GetPosition() returns Point(0, 0)/default(Point)
        if (e.GetPosition(this) == default)
        {
            return;
        }

        Border border = (Border)sender;
        if (LeftBorder == border || RightBorder == border)
        {
            Mouse.OverrideCursor = Cursors.SizeWE;
        }
        else if (TopBorder == border || BottomBorder == border)
        {
            Mouse.OverrideCursor = Cursors.SizeNS;
        }
        else if (TopRightBorder == border || BottomLeftBorder == border)
        {
            Mouse.OverrideCursor = Cursors.SizeNESW;
        }
        else if (BottomRightBorder == border || TopLeftBorder == border)
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
            Border border = (Border)sender;
            nint wParam;
            if (LeftBorder == border)
            {
                wParam = 61441;
            }
            else if (RightBorder == border)
            {
                wParam = 61442;
            }
            else if (TopBorder == border)
            {
                wParam = 61443;
            }
            else if (TopLeftBorder == border)
            {
                wParam = 61444;
            }
            else if (TopRightBorder == border)
            {
                wParam = 61445;
            }
            else if (BottomBorder == border)
            {
                wParam = 61446;
            }
            else if (BottomLeftBorder == border)
            {
                wParam = 61447;
            }
            else // if (BottomRightBorder == border)
            {
                wParam = 61448;
            }

            if (SizeToContent is not SizeToContent.Manual)
            {
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
                MinWidth = 125;
                MinHeight = 50;
            }

            WinApi.ResizeWindow(WindowHandle, wParam);

            LeftPositionBeforeResolutionChange = Left;
            TopPositionBeforeResolutionChange = Top;
            WidthBeforeResolutionChange = ActualWidth;
            HeightBeforeResolutionChange = ActualHeight;
        }
        else if (e.ChangedButton is MouseButton.Right)
        {
            if (FirstPopupWindow.IsVisible)
            {
                PopupWindowUtils.HidePopups(0);
            }
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton is MouseButtonState.Pressed)
        {
            DragMove();
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (e.ClickCount is 2)
        {
            DpiScale dpi = WindowsUtils.Dpi;
            double xPosition;
            double yPosition;
            double width;
            double maxDynamicHeight = configManager.MainWindowMaxDynamicHeight * dpi.DpiScaleY;
            MagpieUtils.IsMagpieScaling = MagpieUtils.IsMagpieScaling && MagpieUtils.IsMagpieReallyScaling();
            if (!MagpieUtils.IsMagpieScaling || !MagpieUtils.SourceWindowRect.Contains(WinApi.GetMousePosition()))
            {
                Rectangle workingArea = WindowsUtils.ActiveScreen.WorkingArea;
                if (configManager.PositionPopupAboveCursor)
                {
                    yPosition = workingArea.Bottom - maxDynamicHeight;
                    if (yPosition < workingArea.Top)
                    {
                        yPosition = workingArea.Top;
                    }
                }
                else
                {
                    yPosition = workingArea.Y;
                }


                double dpiAwareWidth = workingArea.Width / dpi.DpiScaleX;
                width = !configManager.MainWindowDynamicWidth || Width > dpiAwareWidth
                    ? dpiAwareWidth
                    : Width;

                if (configManager.MainWindowFixedRightPosition is 0)
                {
                    double dpiUnawareWidth = width * dpi.DpiScaleX;
                    xPosition = ((workingArea.Right + workingArea.Left + dpiUnawareWidth) / 2) - dpiUnawareWidth;
                }
                else
                {
                    xPosition = workingArea.X;
                }
            }
            else
            {
                if (configManager.PositionPopupAboveCursor
                    || configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true, MainWindowFixedBottomPosition: -2 or -1 })
                {
                    yPosition = MagpieUtils.MagpieWindowBottomEdgePosition - maxDynamicHeight;
                    if (yPosition < MagpieUtils.MagpieWindowTopEdgePosition)
                    {
                        yPosition = MagpieUtils.MagpieWindowTopEdgePosition;
                    }
                }
                else
                {
                    yPosition = MagpieUtils.MagpieWindowTopEdgePosition;
                }

                width = !configManager.MainWindowDynamicWidth || Width > MagpieUtils.DpiAwareMagpieWindowWidth
                    ? MagpieUtils.DpiAwareMagpieWindowWidth
                    : Width;

                if (configManager.MainWindowFixedRightPosition is 0)
                {
                    double dpiUnawareWidth = width * dpi.DpiScaleX;
                    xPosition = ((MagpieUtils.MagpieWindowRightEdgePosition + MagpieUtils.MagpieWindowLeftEdgePosition + dpiUnawareWidth) / 2) - dpiUnawareWidth;
                }
                else
                {
                    xPosition = MagpieUtils.MagpieWindowLeftEdgePosition;
                }
            }

            WinApi.MoveWindowToPosition(WindowHandle, xPosition, yPosition);

            if (configManager.MainWindowMaxDynamicWidth < width)
            {
                configManager.MainWindowMaxDynamicWidth = width;
                MaxWidth = width;
            }

            Width = width;
            WidthBeforeResolutionChange = width;
        }

        LeftPositionBeforeResolutionChange = Left;
        TopPositionBeforeResolutionChange = Top;
    }

    private void MainTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ManageDictionariesMenuItem.IsEnabled = DictUtils.DictsReady && DictUtils.Dicts.Values.ToArray().All(static dict => !dict.Updating);
        ManageFrequenciesMenuItem.IsEnabled = FreqUtils.FreqsReady && FreqUtils.FreqDicts.Values.ToArray().All(static freq => !freq.Updating);

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

        int charIndex = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
        ContextMenuIsOpening = charIndex >= MainTextBox.SelectionStart && charIndex <= MainTextBox.SelectionStart + MainTextBox.SelectionLength;

        if (FirstPopupWindow.IsVisible)
        {
            PopupWindowUtils.HidePopups(0);
        }

        if (!ContextMenuIsOpening && MainTextBox.SelectionLength > 0)
        {
            WindowsUtils.Unselect(MainTextBox);
        }

        ContextMenuIsOpening = false;
        _contextMenuIsClosed = false;
    }

    public bool IsMouseWithinWindowBounds()
    {
        Point mousePosition = WinApi.GetMousePosition();
        DpiScale dpi = WindowsUtils.Dpi;
        double physicalWidth = ActualWidth * dpi.DpiScaleX;
        double physicalHeight = ActualHeight * dpi.DpiScaleY;
        double physicalLeft = Left * dpi.DpiScaleX;
        double physicalTop = Top * dpi.DpiScaleY;
        return mousePosition.X > physicalLeft && mousePosition.X - physicalWidth < physicalLeft
            && mousePosition.Y > physicalTop && mousePosition.Y - physicalHeight < physicalTop;
    }

    public void ChangeVisibility()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (WindowState is WindowState.Minimized
            || FirstPopupWindow.IsVisible
            || Mouse.LeftButton is MouseButtonState.Pressed
            || IsMouseWithinWindowBounds()
            || ManageDictionariesWindow.IsItVisible()
            || ManageFrequenciesWindow.IsItVisible()
            || ManageAudioSourcesWindow.IsItVisible()
            || AddNameWindow.IsItVisible()
            || AddWordWindow.IsItVisible()
            || PreferencesWindow.IsItVisible()
            || StatsWindow.IsItVisible()
            || MainTextBoxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible
            || (!configManager.TextBoxIsReadOnly && _input?.ImeState is InputMethodState.On))
        {
            return;
        }

        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 0d;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0d)
        {
            Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
        }

        nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
        if (configManager is { RestoreFocusToPreviouslyActiveWindow: true, Focusable: true }
            && (configManager.MainWindowFocusOnHover || configManager.PopupFocusOnLookup)
            && lastActiveWindowHandle is not 0
            && lastActiveWindowHandle != WindowHandle)
        {
            WinApi.GiveFocusToWindow(lastActiveWindowHandle);
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            _ = MpvUtils.ResumePlayback().ConfigureAwait(false);
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (IsMouseOver
            || FirstPopupWindow.MiningMode
            || (FirstPopupWindow.IsMouseOver
                && (configManager.FixedPopupPositioning
                    || FirstPopupWindow.UnavoidableMouseEnter)))
        {
            return;
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            MouseEnterDueToFirstPopupHide = IsMouseWithinWindowBounds();
        }

        FirstPopupWindow.HidePopup();
        ChangeVisibility();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1d;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0d)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }

        if (configManager is { Focusable: true, MainWindowFocusOnHover: true })
        {
            if (configManager.RestoreFocusToPreviouslyActiveWindow)
            {
                nint lastActiveWindowHandle = WinApi.GetActiveWindowHandle();
                if (lastActiveWindowHandle != WindowHandle)
                {
                    WindowsUtils.LastActiveWindowHandle = lastActiveWindowHandle;
                }
            }

            WinApi.StealFocus(WindowHandle);
            _ = Focus();
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            if (_contextMenuIsClosed)
            {
                await MpvUtils.PausePlayback(MouseEnterDueToFirstPopupHide).ConfigureAwait(false);
            }
        }

        MouseEnterDueToFirstPopupHide = false;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (WindowState is WindowState.Minimized)
        {
            if (configManager.StopIncreasingTimeAndCharStatsWhenMinimized)
            {
                StatsUtils.StopTimeStatStopWatch();
            }

            if (configManager.GlobalHotKeys)
            {
                OrderedDictionary<string, KeyGesture> globalKeyGestureNameToKeyGestureDict = KeyGestureUtils.GlobalKeyGestureNameToKeyGestureDict;

                string[] namesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized = KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized;
                List<int> keyGestureIdsToIgnore = new(namesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized.Length);
                foreach (string name in namesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized)
                {
                    int id = globalKeyGestureNameToKeyGestureDict.IndexOf(name);
                    if (id >= 0)
                    {
                        keyGestureIdsToIgnore.Add(id);
                    }
                }

                if (keyGestureIdsToIgnore.Count > 0)
                {
                    WinApi.UnregisterAllGlobalHotKeys(WindowHandle, keyGestureIdsToIgnore.AsReadOnlySpan());
                }
                else
                {
                    WinApi.UnregisterAllGlobalHotKeys(WindowHandle);
                }
            }
        }

        else
        {
            if (configManager.StopIncreasingTimeAndCharStatsWhenMinimized
                && (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket))
            {
                StatsUtils.StartTimeStatStopWatch();
            }

            if (configManager.GlobalHotKeys)
            {
                WinApi.RegisterAllGlobalHotKeys(WindowHandle);
            }

            if (SizeToContent is SizeToContent.Manual && (configManager.MainWindowDynamicHeight || configManager.MainWindowDynamicWidth))
            {
                SetSizeToContent(configManager.MainWindowDynamicWidth, configManager.MainWindowDynamicHeight, configManager.MainWindowMaxDynamicWidth, configManager.MainWindowMaxDynamicHeight, configManager.MainWindowMinDynamicWidth, configManager.MainWindowMinDynamicHeight, configManager.MainWindowWidth, configManager.MainWindowHeight);
            }

            UpdatePosition();

            if (configManager.AlwaysOnTop)
            {
                WinApi.BringToFront(WindowHandle);
            }
        }
    }

    private void TitleBar_MouseEnter(object sender, MouseEventArgs e)
    {
        if (FirstPopupWindow is { IsVisible: true, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
        }

        if (ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)
        {
            ShowTitleBarButtons();
        }
    }

    private void TitleBar_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!ConfigManager.Instance.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
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
        ConfigManager configManager = ConfigManager.Instance;
        if (!configManager.HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar
            || FontSizeSlider.IsVisible
            || OpacitySlider.IsVisible
            || (Background.Opacity is 0 && !configManager.GlobalHotKeys))
        {
            return;
        }

        HideTitleBarButtons();
    }

    private void MoveCaret(Key key)
    {
        WinApi.ActivateWindow(WindowHandle);

        PresentationSource? mainTextBoxSource = PresentationSource.FromVisual(MainTextBox);
        Debug.Assert(mainTextBoxSource is not null);

        MainTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, mainTextBoxSource, 0, key)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
    }

    private void Swipe(Point currentPosition)
    {
        //Swipe down
        if (MainTextBox.VerticalOffset is 0
            && currentPosition.Y > _swipeStartPoint.Y + 50)
        {
            BacklogUtils.ShowPreviousBacklogItem();
        }

        //Swipe up
        else if (MainTextBox.GetLastVisibleLineIndex() == MainTextBox.LineCount - 1
                 && currentPosition.Y < _swipeStartPoint.Y - 50)
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
        if (MainTextBox.SelectionLength is 0)
        {
            Swipe(e.GetTouchPoint(this).Position);
        }
    }

    private void TitleBar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (FirstPopupWindow.IsVisible)
        {
            PopupWindowUtils.HidePopups(0);
        }
        _contextMenuIsClosed = false;
    }

    public void UpdatePosition()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true }
            or { RepositionMainWindowOnTextChangeByRightPosition: true, MainWindowDynamicWidth: true })
        {
            Opacity = 0d;
            UpdateLayout();
            Opacity = 1d;

            DpiScale dpi = WindowsUtils.Dpi;
            double currentTop = Top * dpi.DpiScaleY;
            double newTop = currentTop;
            if (configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true })
            {
                newTop = GetDynamicYPosition(configManager.MainWindowFixedBottomPosition);
            }

            double currentLeft = Left * dpi.DpiScaleX;
            double newLeft = currentLeft;
            if (configManager is { RepositionMainWindowOnTextChangeByRightPosition: true, MainWindowDynamicWidth: true })
            {
                newLeft = GetDynamicXPosition(configManager.MainWindowFixedRightPosition);
            }

            if (Math.Abs(currentLeft - newLeft) >= 1 || Math.Abs(currentTop - newTop) >= 1)
            {
                WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);

                LeftPositionBeforeResolutionChange = Left;
                TopPositionBeforeResolutionChange = Top;
                HeightBeforeResolutionChange = Height;
                WidthBeforeResolutionChange = Width;
            }
        }
    }

    private double GetDynamicXPosition(double rightPosition)
    {
        double currentWidth = ActualWidth * WindowsUtils.Dpi.DpiScaleX;
        Screen activeScreen = WindowsUtils.ActiveScreen;

        if (MagpieUtils.IsMagpieScaling)
        {
            MagpieUtils.IsMagpieScaling = MagpieUtils.IsMagpieReallyScaling();
            if (MagpieUtils.IsMagpieScaling)
            {
                if (rightPosition is 0)
                {
                    rightPosition = (MagpieUtils.MagpieWindowLeftEdgePosition + MagpieUtils.MagpieWindowRightEdgePosition + currentWidth) / 2;
                }
                else if (rightPosition is -1 or -2)
                {
                    rightPosition = MagpieUtils.MagpieWindowRightEdgePosition;
                }
                else
                {
                    return Math.Max(activeScreen.Bounds.Left, rightPosition - currentWidth);
                }

                return Math.Max(MagpieUtils.MagpieWindowLeftEdgePosition, rightPosition - currentWidth);
            }
        }

        if (rightPosition is 0)
        {
            rightPosition = (activeScreen.Bounds.Left + activeScreen.Bounds.Right + currentWidth) / 2;
        }
        else if (rightPosition is -1)
        {
            rightPosition = activeScreen.WorkingArea.Right;
        }
        else if (rightPosition is -2)
        {
            rightPosition = activeScreen.Bounds.Right;
        }

        return Math.Max(rightPosition is -1 ? activeScreen.WorkingArea.Left : activeScreen.Bounds.Left, rightPosition - currentWidth);
    }

    private double GetDynamicYPosition(double bottomPosition)
    {
        double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;
        Screen activeScreen = WindowsUtils.ActiveScreen;

        if (MagpieUtils.IsMagpieScaling)
        {
            MagpieUtils.IsMagpieScaling = MagpieUtils.IsMagpieReallyScaling();
            if (MagpieUtils.IsMagpieScaling)
            {
                if (bottomPosition is -2 or -1)
                {
                    bottomPosition = MagpieUtils.MagpieWindowBottomEdgePosition;
                }
                else if (bottomPosition is 0)
                {
                    bottomPosition = (MagpieUtils.MagpieWindowTopEdgePosition + MagpieUtils.MagpieWindowBottomEdgePosition + currentHeight) / 2;
                }
                else
                {
                    return Math.Max(activeScreen.Bounds.Top, bottomPosition - currentHeight);
                }

                return Math.Max(MagpieUtils.MagpieWindowTopEdgePosition, bottomPosition - currentHeight);
            }
        }

        if (bottomPosition is -2)
        {
            bottomPosition = activeScreen.Bounds.Bottom;
        }
        else if (bottomPosition is -1)
        {
            bottomPosition = activeScreen.WorkingArea.Bottom;
        }
        else if (bottomPosition is 0)
        {
            bottomPosition = (activeScreen.Bounds.Top + activeScreen.Bounds.Bottom + currentHeight) / 2;
        }

        return Math.Max(bottomPosition is -1 ? activeScreen.WorkingArea.Top : activeScreen.Bounds.Top, bottomPosition - currentHeight);
    }

    public void SetSizeToContent(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, double minWidth, double minHeight, double width, double height)
    {
        if (dynamicWidth && dynamicHeight)
        {
            MaxHeight = maxHeight;
            MaxWidth = maxWidth;
            MinHeight = minHeight;
            MinWidth = minWidth;
            SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            MaxHeight = maxHeight;
            MinHeight = minHeight;
            MaxWidth = double.PositiveInfinity;
            MinWidth = 125;
            SizeToContent = SizeToContent.Height;
            Width = width;
        }

        else if (dynamicWidth)
        {
            MaxHeight = double.PositiveInfinity;
            MinHeight = 50;
            MaxWidth = maxWidth;
            MinWidth = minWidth;
            SizeToContent = SizeToContent.Width;
            Height = height;
        }

        else
        {
            SizeToContent = SizeToContent.Manual;
            MaxHeight = double.PositiveInfinity;
            MaxWidth = double.PositiveInfinity;
            MinHeight = 50;
            MinWidth = 125;
            Width = width;
            Height = height;
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_ContextMenuClosing(object sender, ContextMenuEventArgs e)
    {
        _contextMenuIsClosed = true;
        ConfigManager configManager = ConfigManager.Instance;
        if (WindowState is WindowState.Minimized
            || IsMouseOver
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
            || (!configManager.TextBoxIsReadOnly && _input?.ImeState is InputMethodState.On))
        {
            return;
        }

        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 0d;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0d)
        {
            Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
        }

        nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
        if (configManager is { RestoreFocusToPreviouslyActiveWindow: true, Focusable: true }
            && (configManager.MainWindowFocusOnHover || configManager.PopupFocusOnLookup)
            && lastActiveWindowHandle is not 0
            && lastActiveWindowHandle != WindowHandle)
        {
            WinApi.GiveFocusToWindow(lastActiveWindowHandle);
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            await MpvUtils.ResumePlayback().ConfigureAwait(false);
        }
    }

    private void MoveWindowToScreen()
    {
        Point mousePosition = WinApi.GetMousePosition();
        int x = double.ConvertToIntegerNative<int>(mousePosition.X);
        int y = double.ConvertToIntegerNative<int>(mousePosition.Y);
        if (!WindowsUtils.ActiveScreen.Bounds.Contains(x, y))
        {
            Rectangle workingArea = Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;

            Opacity = 0d;
            UpdateLayout();

            bool isMinimized = WindowState is WindowState.Minimized;
            if (isMinimized)
            {

                if (ConfigManager.Instance.Focusable)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    // If another window is not set as active window
                    // Main Window gets activated on restore
                    WinApi.ActivateWindow(FirstPopupWindow.WindowHandle);
                    WinApi.RestoreWindow(WindowHandle);
                }
            }

            WinApi.MoveWindowToPosition(WindowHandle, workingArea.X, workingArea.Y);
            HandleDisplaySettingsChange();
            UpdatePosition();

            if (isMinimized)
            {
                if (ConfigManager.Instance.Focusable)
                {
                    WindowState = WindowState.Minimized;
                }
                else
                {
                    WinApi.MinimizeWindow(WindowHandle);
                }
            }

            Opacity = 1d;
        }
    }
}
