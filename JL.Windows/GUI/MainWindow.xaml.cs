using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Tools;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.External;
using JL.Core.Freqs;
using JL.Core.Lookup;
using JL.Core.Network.WebSocket;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;
using JL.Core.Utilities.Database;
using JL.Windows.Config;
using JL.Windows.External.Magpie;
using JL.Windows.External.Tsukikage;
using JL.Windows.GUI.Audio;
using JL.Windows.GUI.Dictionary;
using JL.Windows.GUI.Frequency;
using JL.Windows.GUI.Popup;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;
using Timer = System.Timers.Timer;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
internal sealed partial class MainWindow : IDisposable
{
    public static readonly MainWindow Instance = new();

    private static long s_lastTextCopyTimestamp;
    private static DpiScale? s_previousDpi;

    private bool _contextMenuIsClosed = true;
    private bool _passThroughMode; // = false;
    private Point _swipeStartPoint;
    private InputMethod? _input;
    private Point _lastMouseMovePosition;
    private readonly Timer _lookupDelayTimer;
    private int _lastCharPosition = -1;

    private string? _webSocketTextToProcess;
    private readonly AtomicBool _processingWebSocketText = new(false);

    public nint WindowHandle { get; private set; }
    public PopupWindow FirstPopupWindow { get; }

    public bool ContextMenuIsOpening { get; private set; } // = false;
    public bool MouseEnterDueToFirstPopupHide { get; set; } // = false;

    public double LeftPositionBeforeResolutionChange { get; set; }
    public double TopPositionBeforeResolutionChange { get; set; }
    public double HeightBeforeResolutionChange { get; set; }
    public double WidthBeforeResolutionChange { get; set; }

    private static readonly AtomicBool s_closingEventStarted = new(false);
    private static readonly AtomicBool s_cleanupStarted = new(false);

    private MainWindow()
    {
        InitializeComponent();
        ConfigHelper.Instance.SetLang("en");
        FirstPopupWindow = new PopupWindow(0);

        _lookupDelayTimer = new()
        {
            AutoReset = false,
            Enabled = false
        };
        _lookupDelayTimer.Elapsed += LookupDelayTimer_Elapsed;
    }

    // ReSharper disable once AsyncVoidMethod
    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowHandle = new WindowInteropHelper(this).Handle;
        WinApi.SetCompositedAndNoRedirectionBitmapStyle(WindowHandle);
        WinApi.SubscribeToWndProc(this);

        _input = InputMethod.Current;
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        MagpieUtils.RegisterToMagpieScalingChangedMessage(WindowHandle);

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

        // Can't use ActivateWindow for FirstPopupWindow if it's not shown at least once
        FirstPopupWindow.Show();
        FirstPopupWindow.HidePopup();

        FocusManager.SetFocusedElement(this, MainTextBox);
        // Makes caret/highlight visible without any mouse click
        EditingCommands.MoveLeftByCharacter.Execute(null, MainTextBox);

        MagpieUtils.Init();

        await WindowsUtils.InitializeMainWindow().ConfigureAwait(false);
    }

    private async Task<bool> CopyFromClipboard()
    {
        while (Clipboard.ContainsText())
        {
            try
            {
                string text = Clipboard.GetText();
                WindowsUtils.LastWebSocketTextWasVertical = false;
                return CopyText(text);
            }
            catch (ExternalException ex)
            {
                LoggerManager.Logger.Warning(ex, "CopyFromClipboard failed");
                await Task.Delay(5).ConfigureAwait(true);
            }
        }

        return false;
    }

    public async Task CopyFromWebSocket(string text, bool tsukikage)
    {
        Volatile.Write(ref _webSocketTextToProcess, text);
        if (!_processingWebSocketText.TrySetTrue())
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        bool copiedText = false;

        try
        {
            while (true)
            {
                string? currentText = Interlocked.Exchange(ref _webSocketTextToProcess, null);
                if (currentText is null)
                {
                    break;
                }

                bool verticalText = false;
                int charIndex = 0;

                if (tsukikage && currentText.StartsWith('{'))
                {
                    try
                    {
                        GraphemeInfo? wordInfo = JsonSerializer.Deserialize<GraphemeInfo>(currentText);
                        Debug.Assert(wordInfo is not null);
                        currentText = wordInfo.Text;
                        verticalText = wordInfo.IsVertical;
                        charIndex = wordInfo.GraphemeStartIndex;
                    }
                    catch (JsonException)
                    {
                        LoggerManager.Logger.Debug("CopyFromWebSocket: Failed to deserialize WordInfo from SecretProject WebSocket text: {Text}", currentText);
                    }
                }

                WindowsUtils.LastWebSocketTextWasVertical = verticalText;
                await Dispatcher.BeginInvoke(async () =>
                {
                    copiedText = CopyText(currentText, tsukikage)
                        && !FirstPopupWindow.MiningMode
                        && (tsukikage || configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket)
                        && (!tsukikage || !configManager.RequireLookupKeyPress || configManager.LookupKeyKeyGesture.IsPressed())
                        && (!configManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized || WindowState is WindowState.Minimized);

                    if (!copiedText)
                    {
                        return;
                    }

                    if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                    {
                        await MpvUtils.PausePlayback().ConfigureAwait(true);
                    }

                    MoveWindowToScreen();

                    if (!PopupWindowUtils.TransparentDueToAutoLookup)
                    {
                        WinApi.SetTransparentStyle(FirstPopupWindow.WindowHandle);
                        PopupWindowUtils.TransparentDueToAutoLookup = true;
                    }

                    await FirstPopupWindow.LookupOnCharPosition(MainTextBox, charIndex, false, true, verticalText).ConfigureAwait(true);
                }, DispatcherPriority.Send).Task.ConfigureAwait(true);
            }
        }
        finally
        {
            _processingWebSocketText.SetFalse();
        }
    }

    private bool CopyText(string text, bool tsukikage = false)
    {
        ConfigManager configManager = ConfigManager.Instance;

        if (text.Length is 0)
        {
            if (!configManager.AlwaysShowBacklog)
            {
                MainTextBox.Clear();
                UpdatePosition();
            }

            if (!FirstPopupWindow.MiningMode)
            {
                FirstPopupWindow.HidePopup();
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

        if (configManager.MaxTextLengthToCapture > 0 && sanitizedNewText.Length > configManager.MaxTextLengthToCapture)
        {
            return false;
        }

        bool mergeTexts = false;
        string? subsequentText = null;
        string? mergedText = null;

        string previousText = BacklogUtils.LastItem ?? MainTextBox.Text;
        bool sameText = sanitizedNewText == previousText;
        if (configManager.DiscardIdenticalText && sameText)
        {
            if (configManager.MergeSequentialTextsWhenTheyMatch)
            {
                s_lastTextCopyTimestamp = Stopwatch.GetTimestamp();
            }

            if (MainTextBox.Text.Length is 0 && BacklogUtils.LastItem is not null)
            {
                MainTextBox.Text = BacklogUtils.LastItem;
            }

            return configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket || configManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard || tsukikage;
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
                    if (MainTextBox.Text.Length is 0 && BacklogUtils.LastItem is not null)
                    {
                        MainTextBox.Text = BacklogUtils.LastItem;
                    }

                    Debug.Assert(MainTextBox.Text.Length > 0);
                    return configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket || configManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard || tsukikage;
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

        bool backlogActive = configManager.MaxBacklogCapacity is not 0;
        mergeTexts = mergeTexts && subsequentText is not null;
        bool doNotShowAllBacklog = !backlogActive || !configManager.AlwaysShowBacklog;
        if (mergeTexts)
        {
            if (doNotShowAllBacklog && MainTextBox.Text != previousText)
            {
                MainTextBox.Text = previousText;
            }

            MainTextBox.AppendText(subsequentText);
            mergedText = previousText + subsequentText;
            if (backlogActive)
            {
                BacklogUtils.ReplaceLastBacklogText(mergedText);
            }
        }
        else
        {
            if (doNotShowAllBacklog)
            {
                MainTextBox.Text = sanitizedNewText;
                Debug.Assert(MainTextBox.Text.Length > 0);
                if (backlogActive)
                {
                    BacklogUtils.AddToBacklog(sanitizedNewText);
                }
            }
            else
            {
                BacklogUtils.AddToBacklogShowAllBacklog(sanitizedNewText);
            }
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
            && FirstPopupWindow.Opacity is not 0
            && (!sameText || ((PopupWindowUtils.PopupWindows[1]?.Opacity ?? 0) is not 0)))
        {
            PopupWindowUtils.HidePopups(0);
        }

        if (notMinimized)
        {
            UpdatePosition();
            if (configManager.AlwaysOnTop && FirstPopupWindow.Opacity is 0)
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
                StatsUtils.IncrementStat(StatType.Characters, strippedText.GetGraphemeCount());

                if (!mergeTexts)
                {
                    StatsUtils.IncrementStat(StatType.Lines);
                }
            }
        }

        if (configManager.TextToSpeechOnTextChange
            && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
        {
            SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, sanitizedNewText).SafeFireAndForget("TextToSpeech failed");
        }

        Debug.Assert(MainTextBox.Text.Length > 0);
        return true;
    }

    public void BringToFront()
    {
        if (ConfigManager.Instance.AlwaysOnTop
            && FirstPopupWindow.Opacity is 0
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

    public async Task ClipboardChanged()
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
            await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true, true, false).ConfigureAwait(false);
        }
    }

    private Task HandleMouseMove(MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.InactiveLookupMode
               || configManager.LookupOnSelectOnly
               || configManager.LookupOnMouseClickOnly
               || e.LeftButton is MouseButtonState.Pressed
               || MainTextBoxContextMenu.IsVisible
               || TitleBarContextMenu.IsVisible
               || FontSizeSlider.IsVisible
               || OpacitySlider.IsVisible
               || FirstPopupWindow.MiningMode
               || (!configManager.TextBoxIsReadOnly && _input?.ImeState is InputMethodState.On)
               || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed()))
        {
            return Task.CompletedTask;
        }

        if (PopupWindowUtils.TransparentDueToAutoLookup)
        {
            WinApi.UnsetTransparentStyle(FirstPopupWindow.WindowHandle);
            PopupWindowUtils.TransparentDueToAutoLookup = false;
        }

        if (!_lookupDelayTimer.Enabled)
        {
            return FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox, false);
        }

        InitDelayedLookup();
        return Task.CompletedTask;
    }

    public void InitLookupDelayTimer(int delayInMilliseconds)
    {
        if (delayInMilliseconds is 0)
        {
            _lookupDelayTimer.Enabled = false;
        }
        else
        {
            _lookupDelayTimer.Interval = delayInMilliseconds;
            _lookupDelayTimer.Enabled = true;
        }
    }

    private void InitDelayedLookup()
    {
        int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
        if (charPosition < 0)
        {
            _lookupDelayTimer.Enabled = false;
            _lastCharPosition = charPosition;
            FirstPopupWindow.HidePopup();
            return;
        }

        if (char.IsLowSurrogate(MainTextBox.Text[charPosition]))
        {
            --charPosition;
        }

        if (charPosition != _lastCharPosition)
        {
            _lookupDelayTimer.Enabled = false;
            _lastCharPosition = charPosition;
            _lookupDelayTimer.Enabled = true;
        }
        else if (FirstPopupWindow.Opacity is 0)
        {
            _lookupDelayTimer.Enabled = true;
        }
    }

    private void LookupDelayTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(HandleDelayedLookup);
    }

    private void HandleDelayedLookup()
    {
        if (WindowState is WindowState.Minimized
            || MainTextBoxContextMenu.IsVisible
            || TitleBarContextMenu.IsVisible)
        {
            return;
        }

        int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
        if (charPosition < 0)
        {
            _lastCharPosition = charPosition;
            return;
        }

        if (char.IsLowSurrogate(MainTextBox.Text[charPosition]))
        {
            --charPosition;
        }

        if (charPosition == _lastCharPosition)
        {
            FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox, false).SafeFireAndForget("LookupOnMouseMoveOrClick failed unexpectedly");
        }
        else
        {
            _lastCharPosition = charPosition;
        }
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
        else if (FirstPopupWindow is { Opacity: not 0, MiningMode: false })
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
            || OpacitySlider.IsVisible)
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

            if (OpacitySlider.Visibility is Visibility.Collapsed)
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
            if (FirstPopupWindow.Opacity is not 0)
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
            if (FirstPopupWindow.Opacity is not 0)
            {
                PopupWindowUtils.HidePopups(0);
            }
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;

        if (!s_closingEventStarted.TrySetTrue())
        {
            return;
        }

        Hide();
        await HandleAppClosing().ConfigureAwait(true);
        Application.Current.Shutdown();
    }

    public async Task HandleAppClosing()
    {
        if (!s_cleanupStarted.TrySetTrue())
        {
            return;
        }

        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        WinApi.UnsubscribeFromWndProc(this);

        await WebSocketUtils.DisconnectFromAllWebSocketConnections().ConfigureAwait(true);
        await WebSocketUtils.DisconnectFromTsukikageWebSocketConnection().ConfigureAwait(true);

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
            HandleDisableHotkeysToggle();
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
            HandlePassThroughKeyGesture();
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
            HandleShowAddNameWindow();
        }

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
        {
            HandleShowAddWordWindow();
        }

        else if (keyGesture.IsEqual(configManager.ShowManageDictionariesWindowKeyGesture))
        {
            if (DictUtils.DictsReady && DictUtils.Dicts.Values.ToArray().All(static dict => dict.Ready))
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
            HandleClipboardCaptureToggle();
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromWebSocketKeyGesture))
        {
            return HandleWebSocketCaptureToggle();
        }

        else if (keyGesture.IsEqual(configManager.ReconnectToWebSocketServerKeyGesture))
        {
            HandleReconnectToWebSocket();
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromTsukikageWebSocketKeyGesture))
        {
            return HandleTsukikageWebSocketCaptureToggle();
        }

        else if (keyGesture.IsEqual(configManager.ReconnectToTsukikageWebSocketKeyGesture))
        {
            HandleReconnectToTsukikageWebSocket();
        }

        else if (keyGesture.IsEqual(configManager.TextBoxIsReadOnlyKeyGesture))
        {
            HandleTextBoxReadOnlyToggle();
        }

        else if (keyGesture.IsEqual(configManager.DeleteCurrentLineKeyGesture))
        {
            BacklogUtils.DeleteCurrentLine();
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            HandleToggleMinimizedState();
        }

        else if (keyGesture.IsEqual(configManager.SelectedTextToSpeechKeyGesture))
        {
            return HandleSelectedTextToSpeech();
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretLeftKeyGesture))
        {
            EditingCommands.MoveLeftByCharacter.Execute(null, MainTextBox);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretRightKeyGesture))
        {
            EditingCommands.MoveRightByCharacter.Execute(null, MainTextBox);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretUpKeyGesture))
        {
            EditingCommands.MoveUpByLine.Execute(null, MainTextBox);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretDownKeyGesture))
        {
            EditingCommands.MoveDownByLine.Execute(null, MainTextBox);
        }

        else if (keyGesture.IsEqual(configManager.LookupTermAtCaretIndexKeyGesture))
        {
            return HandleLookupTermAtCaret();
        }

        else if (keyGesture.IsEqual(configManager.LookupFirstTermKeyGesture))
        {
            return HandleLookupFirstTerm();
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

    public void HandleDisableHotkeysToggle()
    {
        ConfigManager configManager = ConfigManager.Instance;
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

    private void HandleShowAddNameWindow()
    {
        bool customNameDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomNameDictionary, out Dict? customNameDict) && customNameDict.Ready;
        bool profileCustomNameDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomNameDictionary, out Dict? profileCustomNameDict) && profileCustomNameDict.Ready;
        if (customNameDictReady && profileCustomNameDictReady)
        {
            ShowAddNameWindow();
        }
    }

    private void HandleShowAddWordWindow()
    {
        bool customWordDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict) && customWordDict.Ready;
        bool profileCustomWordDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict) && profileCustomWordDict.Ready;
        if (customWordDictReady && profileCustomWordDictReady)
        {
            ShowAddWordWindow();
        }
    }

    private void HandleClipboardCaptureToggle()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.CaptureTextFromClipboard = !coreConfigManager.CaptureTextFromClipboard;

        if (coreConfigManager.CaptureTextFromClipboard)
        {
            WinApi.SubscribeToClipboardChanged(WindowHandle);
        }
        else
        {
            WinApi.UnsubscribeFromClipboardChanged(WindowHandle);
        }

        if (coreConfigManager is { CaptureTextFromTsukikageWebsocket: false, CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
        {
            StatsUtils.StopTimeStatStopWatch();
        }
        else if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
        {
            StatsUtils.StartTimeStatStopWatch();
        }
    }

    private Task HandleWebSocketCaptureToggle()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.CaptureTextFromWebSocket = !coreConfigManager.CaptureTextFromWebSocket;

        if (coreConfigManager is { CaptureTextFromTsukikageWebsocket: false, CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
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
            return Task.CompletedTask;
        }

        return WebSocketUtils.DisconnectFromAllWebSocketConnections();
    }

    private Task HandleTsukikageWebSocketCaptureToggle()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.CaptureTextFromTsukikageWebsocket = !coreConfigManager.CaptureTextFromTsukikageWebsocket;

        if (coreConfigManager is { CaptureTextFromTsukikageWebsocket: false, CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
        {
            StatsUtils.StopTimeStatStopWatch();
        }
        else if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
        {
            StatsUtils.StartTimeStatStopWatch();
        }

        if (coreConfigManager.CaptureTextFromTsukikageWebsocket)
        {
            WebSocketUtils.ConnectToTsukikageWebSocket();
            return Task.CompletedTask;
        }

        return WebSocketUtils.DisconnectFromTsukikageWebSocketConnection();
    }

    private void HandleReconnectToWebSocket()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager.Instance.CaptureTextFromWebSocket = true;

        WebSocketUtils.ConnectToAllWebSockets();
        if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
        {
            if (!StatsUtils.TimeStatStopWatch.IsRunning)
            {
                StatsUtils.StartTimeStatStopWatch();
            }
        }
    }

    private void HandleReconnectToTsukikageWebSocket()
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager.Instance.CaptureTextFromTsukikageWebsocket = true;

        WebSocketUtils.ConnectToTsukikageWebSocket();
        if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized || WindowState is not WindowState.Minimized)
        {
            if (!StatsUtils.TimeStatStopWatch.IsRunning)
            {
                StatsUtils.StartTimeStatStopWatch();
            }
        }
    }

    private void HandleTextBoxReadOnlyToggle()
    {
        ConfigManager configManager = ConfigManager.Instance;
        configManager.TextBoxIsReadOnly = !configManager.TextBoxIsReadOnly;
        MainTextBox.SetIsReadOnly(configManager.TextBoxIsReadOnly);
    }

    private void HandleToggleMinimizedState()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (FirstPopupWindow.Opacity is not 0)
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
                WinApi.ActivateWindow(FirstPopupWindow.WindowHandle);
                WinApi.RestoreWindow(WindowHandle);
            }
            else
            {
                WinApi.MinimizeWindow(WindowHandle);
                if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                {
                    MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
                }
            }
        }
    }

    private Task HandleSelectedTextToSpeech()
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
        return Task.CompletedTask;
    }

    private Task HandleLookupTermAtCaret()
    {
        ConfigManager configManager = ConfigManager.Instance;
        string text = MainTextBox.Text;
        if (text.Length > 0)
        {
            if (configManager.LookupOnSelectOnly && MainTextBox.SelectionLength > 0 && MainTextBox.SelectionStart == MainTextBox.CaretIndex)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                {
                    MpvUtils.PausePlayback().SafeFireAndForget("Unexpected error while pausing playback");
                }
                return FirstPopupWindow.LookupOnSelect(MainTextBox);
            }

            if (text.Length > MainTextBox.CaretIndex)
            {
                if (configManager.AutoPauseOrResumeMpvOnHoverChange && !IsMouseOver)
                {
                    MpvUtils.PausePlayback().SafeFireAndForget("Unexpected error while pausing playback");
                }
                MoveWindowToScreen();
                return FirstPopupWindow.LookupOnCharPosition(MainTextBox, MainTextBox.CaretIndex, true, true, false);
            }
        }
        return Task.CompletedTask;
    }

    private Task HandleLookupFirstTerm()
    {
        if (MainTextBox.Text.Length > 0)
        {
            MoveWindowToScreen();
            return FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true, true, false);
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
            MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
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
        _lookupDelayTimer.Enabled = false;

        if (e.ChangedButton == ConfigManager.Instance.MiningModeMouseButton && FirstPopupWindow is { Opacity: not 0, MiningMode: false })
        {
            e.Handled = true;
            FirstPopupWindow.ShowMiningModeResults();
        }
        else if (e.ChangedButton is not MouseButton.Right)
        {
            if (FirstPopupWindow.Opacity is not 0)
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

        configManager.PopupYOffsetForVerticalText = Math.Round(configManager.PopupYOffsetForVerticalText / ratioY);
        configManager.PopupXOffsetForVerticalText = Math.Round(configManager.PopupXOffsetForVerticalText / ratioX);
        WindowsUtils.DpiAwareXOffsetForVerticalText = configManager.PopupXOffsetForVerticalText * dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffsetForVerticalText = configManager.PopupYOffsetForVerticalText * dpi.DpiScaleY;

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
            WindowsUtils.DpiAwareXOffsetForVerticalText = configManager.PopupXOffsetForVerticalText * dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffsetForVerticalText = configManager.PopupYOffsetForVerticalText * dpi.DpiScaleY;

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
        WindowsUtils.DpiAwareXOffsetForVerticalText = configManager.PopupXOffsetForVerticalText * e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffsetForVerticalText = configManager.PopupYOffsetForVerticalText * e.NewDpi.DpiScaleY;
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
        WindowsUtils.DpiAwareXOffsetForVerticalText = configManager.PopupXOffsetForVerticalText * dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffsetForVerticalText = configManager.PopupYOffsetForVerticalText * dpi.DpiScaleY;
    }

    private void Border_OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (FirstPopupWindow is { Opacity: not 0, MiningMode: false })
        {
            FirstPopupWindow.HidePopup();
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1d;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
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
            if (FirstPopupWindow.Opacity is not 0)
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
            double dpiUnawareHeight = Height * dpi.DpiScaleY;

            Rect referenceWindowRect = !MagpieUtils.IsMagpieScaling() || !MagpieUtils.MagpieWindowRect.IntersectsWith(new Rect(Left * dpi.DpiScaleX, Top * dpi.DpiScaleY, Width * dpi.DpiScaleX, Height * dpi.DpiScaleY))
                ? WindowsUtils.ActiveScreen.WorkingArea.ToRect()
                : MagpieUtils.MagpieWindowRect;

            double topPosition;
            if (configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true })
            {
                topPosition = GetDynamicYPosition(configManager.MainWindowFixedBottomPosition);
            }
            else if (configManager.PositionPopupAboveCursor)
            {
                topPosition = referenceWindowRect.Bottom - dpiUnawareHeight;
                if (topPosition < referenceWindowRect.Top)
                {
                    topPosition = referenceWindowRect.Top;
                }
            }
            else
            {
                topPosition = referenceWindowRect.Top;
            }

            double leftPosition = configManager is { RepositionMainWindowOnTextChangeByRightPosition: true, MainWindowDynamicWidth: true }
                ? GetDynamicXPosition(configManager.MainWindowFixedRightPosition)
                : referenceWindowRect.Left;

            WinApi.MoveWindowToPosition(WindowHandle, leftPosition, topPosition);

            double dpiAwareWidth = referenceWindowRect.Width / dpi.DpiScaleX;
            double width = !configManager.MainWindowDynamicWidth || Width > dpiAwareWidth
                ? dpiAwareWidth
                : Width;

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
        _lookupDelayTimer.Enabled = false;

        ManageDictionariesMenuItem.IsEnabled = DictUtils.DictsReady && DictUtils.Dicts.Values.ToArray().All(static dict => !dict.Updating);
        ManageFrequenciesMenuItem.IsEnabled = FreqUtils.FreqsReady && FreqUtils.FreqDicts.Values.ToArray().All(static freq => !freq.Updating);
        SearchMenuItem.IsEnabled = !string.IsNullOrWhiteSpace(MainTextBox.SelectedText);

        bool customNameDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomNameDictionary, out Dict? customNameDict) && customNameDict.Ready;
        bool profileCustomNameDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomNameDictionary, out Dict? profileCustomNameDict) && profileCustomNameDict.Ready;
        AddNameMenuItem.IsEnabled = customNameDictReady && profileCustomNameDictReady;

        bool customWordDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.CustomWordDictionary, out Dict? customWordDict) && customWordDict.Ready;
        bool profileCustomWordDictReady = DictUtils.SingleDictTypeDicts.TryGetValue(DictType.ProfileCustomWordDictionary, out Dict? profileCustomWordDict) && profileCustomWordDict.Ready;
        AddWordMenuItem.IsEnabled = customWordDictReady && profileCustomWordDictReady;

        EnableEditingMenuItem.IsChecked = !MainTextBox.IsReadOnly;

        int charIndex = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
        ContextMenuIsOpening = charIndex >= MainTextBox.SelectionStart && charIndex <= MainTextBox.SelectionStart + MainTextBox.SelectionLength;

        if (FirstPopupWindow.Opacity is not 0)
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
            || FirstPopupWindow.Opacity is not 0
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

        if (!_passThroughMode)
        {
            if (configManager.TextOnlyVisibleOnHover)
            {
                MainGrid.Opacity = 0d;
            }

            if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            {
                Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
            }

            if (configManager is { RestoreFocusToPreviouslyActiveWindow: true, Focusable: true }
                && (configManager.MainWindowFocusOnHover || configManager.PopupFocusOnLookup))
            {
                nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
                if (lastActiveWindowHandle is not 0 && lastActiveWindowHandle != WindowHandle)
                {
                    WinApi.GiveFocusToWindow(lastActiveWindowHandle);
                }
            }
        }

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
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

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
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
                && (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket || coreConfigManager.CaptureTextFromTsukikageWebsocket))
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
        if (FirstPopupWindow is { Opacity: not 0, MiningMode: false })
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
            || OpacitySlider.IsVisible)
        {
            return;
        }

        HideTitleBarButtons();
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
        _lookupDelayTimer?.Stop();

        if (FirstPopupWindow.Opacity is not 0)
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

        if (MagpieUtils.IsMagpieScaling())
        {
            Rect magpieWindowRect = MagpieUtils.MagpieWindowRect;
            if (rightPosition is 0)
            {
                rightPosition = (magpieWindowRect.Left + magpieWindowRect.Right - currentWidth) / 2;
            }
            else if (rightPosition is -1 or -2)
            {
                rightPosition = magpieWindowRect.Right - currentWidth;
            }
            else
            {
                return Math.Max(activeScreen.Bounds.Left, rightPosition);
            }

            return Math.Max(magpieWindowRect.Left, rightPosition);
        }

        if (rightPosition is 0)
        {
            rightPosition = (activeScreen.Bounds.Left + activeScreen.Bounds.Right - currentWidth) / 2;
        }
        else if (rightPosition is -1)
        {
            rightPosition = activeScreen.WorkingArea.Right - currentWidth;
        }
        else if (rightPosition is -2)
        {
            rightPosition = activeScreen.Bounds.Right - currentWidth;
        }

        return Math.Max(rightPosition is -1 ? activeScreen.WorkingArea.Left : activeScreen.Bounds.Left, rightPosition);
    }

    private double GetDynamicYPosition(double bottomPosition)
    {
        double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;
        Screen activeScreen = WindowsUtils.ActiveScreen;

        if (MagpieUtils.IsMagpieScaling())
        {
            Rect magpieWindowRect = MagpieUtils.MagpieWindowRect;
            if (bottomPosition is -2 or -1)
            {
                bottomPosition = magpieWindowRect.Bottom - currentHeight;
            }
            else if (bottomPosition is 0)
            {
                bottomPosition = (magpieWindowRect.Top + magpieWindowRect.Bottom - currentHeight) / 2;
            }
            else
            {
                return Math.Max(activeScreen.Bounds.Top, bottomPosition);
            }

            return Math.Max(magpieWindowRect.Top, bottomPosition);
        }

        if (bottomPosition is -2)
        {
            bottomPosition = activeScreen.Bounds.Bottom - currentHeight;
        }
        else if (bottomPosition is -1)
        {
            bottomPosition = activeScreen.WorkingArea.Bottom - currentHeight;
        }
        else if (bottomPosition is 0)
        {
            bottomPosition = (activeScreen.Bounds.Top + activeScreen.Bounds.Bottom - currentHeight) / 2;
        }

        return Math.Max(bottomPosition is -1 ? activeScreen.WorkingArea.Top : activeScreen.Bounds.Top, bottomPosition);
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
            || FirstPopupWindow.Opacity is not 0
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

        if (!_passThroughMode)
        {
            if (configManager.TextOnlyVisibleOnHover)
            {
                MainGrid.Opacity = 0d;
            }

            if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            {
                Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
            }

            if (configManager is { RestoreFocusToPreviouslyActiveWindow: true, Focusable: true }
                && (configManager.MainWindowFocusOnHover || configManager.PopupFocusOnLookup))
            {
                nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
                if (lastActiveWindowHandle is not 0 && lastActiveWindowHandle != WindowHandle)
                {
                    WinApi.GiveFocusToWindow(lastActiveWindowHandle);
                }
            }
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

    public void HandlePassThroughKeyGesture()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (!_passThroughMode)
        {
            if (configManager.GlobalHotKeys)
            {
                _passThroughMode = true;
                WinApi.SetTransparentStyle(WindowHandle);

                MainGrid.Opacity = 1d;
                FontSizeSlider.Visibility = Visibility.Collapsed;
                OpacitySlider.Visibility = Visibility.Collapsed;

                HideTitleBarButtons();

                if (configManager.Focusable)
                {
                    nint lastActiveWindowHandle = WindowsUtils.LastActiveWindowHandle;
                    if (configManager.RestoreFocusToPreviouslyActiveWindow
                        && (configManager.PopupFocusOnLookup || configManager.MainWindowFocusOnHover)
                        && lastActiveWindowHandle is not 0
                        && lastActiveWindowHandle != WindowHandle
                        && FirstPopupWindow.Opacity is 0)
                    {
                        WinApi.GiveFocusToWindow(lastActiveWindowHandle);
                    }
                    else
                    {
                        Keyboard.ClearFocus();
                    }
                }
            }
        }
        else
        {
            _passThroughMode = false;
            WinApi.UnsetTransparentStyle(WindowHandle);

            Background.Opacity = OpacitySlider.Value / 100;
            _ = MainTextBox.Focus();
            ChangeVisibility();
            ChangeVisibilityOfTitleBarButtons();
        }
    }

    private void ToggleIsReadOnly(object sender, RoutedEventArgs e)
    {
        HandleTextBoxReadOnlyToggle();
    }

    public void Dispose()
    {
        _lookupDelayTimer.Elapsed -= LookupDelayTimer_Elapsed;
        _lookupDelayTimer.Dispose();
    }
}
