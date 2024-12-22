using System.ComponentModel;
using System.Diagnostics;
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
using JL.Core.Freqs;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
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

    private Point _swipeStartPoint;

    private static DateTime s_lastTextCopyTime;

    private static DpiScale? s_previousDpi;

    private MainWindow()
    {
        s_instance = this;
        InitializeComponent();
        ConfigHelper.Instance.SetLang("en");
        FirstPopupWindow = new PopupWindow();
    }

    // ReSharper disable once AsyncVoidMethod
    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        WindowHandle = new WindowInteropHelper(this).Handle;
        _winApi = new WinApi();
        _winApi.ClipboardChanged += ClipboardChanged;
        _winApi.SubscribeToWndProc(this);
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
                Utils.Logger.Warning(ex, "CopyFromClipboard failed");
                await Task.Delay(5).ConfigureAwait(true);
            }
        }

        return false;
    }

    public Task CopyFromWebSocket(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        return CopyText(text)
            ? Dispatcher.Invoke(() => configManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket
                                      && (!configManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                                          || WindowState is WindowState.Minimized)
                ? FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true)
                : Task.CompletedTask)
            : Task.CompletedTask;
    }

    private bool CopyText(string text)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.OnlyCaptureTextWithJapaneseChars && !JapaneseUtils.JapaneseRegex.IsMatch(text))
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

        bool result = Dispatcher.Invoke(() =>
        {
            string previousText = MainTextBox.Text;
            if (configManager.DiscardIdenticalText && sanitizedNewText == previousText)
            {
                if (configManager.MergeSequentialTextsWhenTheyMatch)
                {
                    s_lastTextCopyTime = new DateTime(Stopwatch.GetTimestamp());
                }

                return false;
            }

            if (configManager.MergeSequentialTextsWhenTheyMatch)
            {
                DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
                mergeTexts = (configManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds is 0
                              || (preciseTimeNow - s_lastTextCopyTime).TotalMilliseconds <= configManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds)
                              && previousText.Length > 0;

                s_lastTextCopyTime = preciseTimeNow;

                if (mergeTexts)
                {
                    if (!configManager.DiscardIdenticalText && previousText == sanitizedNewText)
                    {
                        return false;
                    }

                    if (!configManager.AllowPartialMatchingForTextMerge)
                    {
                        if (sanitizedNewText.StartsWith(previousText, StringComparison.Ordinal))
                        {
                            subsequentText = sanitizedNewText[previousText.Length..];
                        }
                    }
                    else
                    {
                        int startIndex = Math.Max(previousText.Length - sanitizedNewText.Length, 0);
                        for (int i = startIndex; i < previousText.Length; i++)
                        {
                            if (sanitizedNewText.StartsWith(previousText[i..], StringComparison.Ordinal))
                            {
                                subsequentText = sanitizedNewText[(previousText.Length - i)..];
                                if (subsequentText.Length is 0 && sanitizedNewText != previousText)
                                {
                                    subsequentText = null;
                                }

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

            if (!mergeTexts && SizeToContent is SizeToContent.Manual && WindowState is not WindowState.Minimized
                            && (configManager.MainWindowDynamicHeight || configManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContent(configManager.MainWindowDynamicWidth, configManager.MainWindowDynamicHeight, configManager.MainWindowMaxDynamicWidth, configManager.MainWindowMaxDynamicHeight, configManager.MainWindowMinDynamicWidth, configManager.MainWindowMinDynamicHeight, configManager.MainWindowWidth, configManager.MainWindowHeight, this);
            }

            TitleBarContextMenu.IsOpen = false;
            MainTextBoxContextMenu.IsOpen = false;

            if (configManager.HidePopupsOnTextChange)
            {
                PopupWindowUtils.HidePopups(FirstPopupWindow);
            }

            UpdatePosition();
            BringToFront();

            return true;
        }, DispatcherPriority.Send);

        if (result)
        {
            WindowsUtils.HandlePostCopy(sanitizedNewText, subsequentText, mergedText);
        }

        return result;
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
            await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true).ConfigureAwait(false);
        }
    }

    private Task HandleMouseMove(MouseEventArgs? e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        return configManager.InactiveLookupMode
               || configManager.LookupOnSelectOnly
               || configManager.LookupOnMouseClickOnly
               || e?.LeftButton is MouseButtonState.Pressed
               || MainTextBoxContextMenu.IsVisible
               || TitleBarContextMenu.IsVisible
               || FontSizeSlider.IsVisible
               || OpacitySlider.IsVisible
               || FirstPopupWindow.MiningMode
               || (!configManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On)
               || (configManager.RequireLookupKeyPress && !configManager.LookupKeyKeyGesture.IsPressed())
            ? Task.CompletedTask
            : FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox, false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MainTextBox_MouseMove(object? sender, MouseEventArgs? e)
    {
        await HandleMouseMove(e).ConfigureAwait(false);
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

    // ReSharper disable once AsyncVoidMethod
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        await HandleAppClosing().ConfigureAwait(false);
    }

    public async Task HandleAppClosing()
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        MagpieUtils.UnmarkWindowAsMagpieToolWindow(WindowHandle);

        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(false))
        {
            ConfigManager.Instance.SaveBeforeClosing(connection);
            StatsUtils.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks);
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
        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    public async Task HandleHotKey(KeyGesture keyGesture, KeyEventArgs? e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
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
                    if (KeyGestureUtils.GlobalKeyGestureNameToIntDict.TryGetValue(nameof(configManager.DisableHotkeys), out int id))
                    {
                        WinApi.UnregisterAllGlobalHotKeys(WindowHandle, id);
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

        if (configManager.DisableHotkeys || handled)
        {
            return;
        }

        if (keyGesture.IsEqual(configManager.SteppedBacklogBackwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowPreviousBacklogItem();
        }

        else if (keyGesture.IsEqual(configManager.SteppedBacklogForwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowNextBacklogItem();
        }

        else if (keyGesture.IsEqual(configManager.ShowPreferencesWindowKeyGesture))
        {
            handled = true;

            if (PreferencesMenuItem.IsEnabled)
            {
                WindowsUtils.ShowPreferencesWindow();
            }
        }

        else if (keyGesture.IsEqual(configManager.MousePassThroughModeKeyGesture))
        {
            handled = true;

            if (Background.Opacity is not 0)
            {
                Background.Opacity = 0;
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
            }
        }

        else if (keyGesture.IsEqual(configManager.ShowAddNameWindowKeyGesture))
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

        else if (keyGesture.IsEqual(configManager.ShowAddWordWindowKeyGesture))
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

        else if (keyGesture.IsEqual(configManager.ShowManageDictionariesWindowKeyGesture))
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

        else if (keyGesture.IsEqual(configManager.ShowManageFrequenciesWindowKeyGesture))
        {
            handled = true;

            if (FreqUtils.FreqsReady)
            {
                await WindowsUtils.ShowManageFrequenciesWindow().ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(configManager.SearchWithBrowserKeyGesture))
        {
            handled = true;

            WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
            WindowsUtils.UpdateMainWindowVisibility();
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

        else if (keyGesture.IsEqual(configManager.ClosePopupKeyGesture))
        {
            handled = true;

            FirstPopupWindow.HidePopup();
        }

        else if (keyGesture.IsEqual(configManager.ShowStatsKeyGesture))
        {
            handled = true;

            WindowsUtils.ShowStatsWindow();
        }

        else if (keyGesture.IsEqual(configManager.ShowManageAudioSourcesWindowKeyGesture))
        {
            handled = true;

            await WindowsUtils.ShowManageAudioSourcesWindow().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(configManager.AlwaysOnTopKeyGesture))
        {
            handled = true;

            configManager.AlwaysOnTop = !configManager.AlwaysOnTop;

            Topmost = configManager.AlwaysOnTop;
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromClipboardKeyGesture))
        {
            handled = true;

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
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else if (!configManager.StopIncreasingTimeStatWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (keyGesture.IsEqual(configManager.CaptureTextFromWebSocketKeyGesture))
        {
            handled = true;

            coreConfigManager.CaptureTextFromWebSocket = !coreConfigManager.CaptureTextFromWebSocket;
            WebSocketUtils.HandleWebSocket();

            if (coreConfigManager is { CaptureTextFromWebSocket: false, CaptureTextFromClipboard: false })
            {
                StatsUtils.StatsStopWatch.Stop();
                StatsUtils.StopStatsTimer();
            }
            else if (!configManager.StopIncreasingTimeStatWhenMinimized || WindowState is not WindowState.Minimized)
            {
                StatsUtils.StatsStopWatch.Start();
                StatsUtils.StartStatsTimer();
            }
        }

        else if (keyGesture.IsEqual(configManager.ReconnectToWebSocketServerKeyGesture))
        {
            handled = true;

            if (!WebSocketUtils.Connected)
            {
                coreConfigManager.CaptureTextFromWebSocket = true;

                if (!StatsUtils.StatsStopWatch.IsRunning)
                {
                    StatsUtils.StatsStopWatch.Start();
                    StatsUtils.StartStatsTimer();
                }

                WebSocketUtils.HandleWebSocket();
            }
        }

        else if (keyGesture.IsEqual(configManager.TextBoxIsReadOnlyKeyGesture))
        {
            handled = true;

            configManager.TextBoxIsReadOnly = !configManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = configManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !configManager.TextBoxIsReadOnly;
            MainTextBox.UndoLimit = configManager.TextBoxIsReadOnly ? 0 : -1;
        }

        else if (keyGesture.IsEqual(configManager.DeleteCurrentLineKeyGesture))
        {
            handled = true;

            BacklogUtils.DeleteCurrentLine();
        }

        else if (keyGesture.IsEqual(configManager.ToggleMinimizedStateKeyGesture))
        {
            handled = true;

            PopupWindowUtils.HidePopups(FirstPopupWindow);

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
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectedTextToSpeechKeyGesture))
        {
            handled = true;

            if (SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
            {
                string selectedText = MainTextBox.SelectionLength > 0
                        ? MainTextBox.SelectedText
                        : MainTextBox.Text;

                if (selectedText.Length > 0)
                {
                    await SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, selectedText).ConfigureAwait(false);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretLeftKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Left);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretRightKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Right);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretUpKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Up);
        }

        else if (keyGesture.IsEqual(configManager.MoveCaretDownKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Down);
        }

        else if (keyGesture.IsEqual(configManager.LookupTermAtCaretIndexKeyGesture))
        {
            handled = true;

            if (MainTextBox.Text.Length > 0)
            {
                if (configManager.LookupOnSelectOnly && MainTextBox.SelectionLength > 0 && MainTextBox.SelectionStart == MainTextBox.CaretIndex)
                {
                    await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
                }

                else
                {
                    await FirstPopupWindow.LookupOnCharPosition(MainTextBox, MainTextBox.CaretIndex, true).ConfigureAwait(false);
                }
            }
        }

        else if (keyGesture.IsEqual(configManager.LookupFirstTermKeyGesture))
        {
            handled = true;

            if (MainTextBox.Text.Length > 0)
            {
                await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true).ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(configManager.LookupSelectedTextKeyGesture))
        {
            handled = true;

            await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(configManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
        {
            handled = true;

            configManager.AlwaysShowMainTextBoxCaret = !configManager.AlwaysShowMainTextBoxCaret;
            MainTextBox.IsReadOnlyCaretVisible = configManager.AlwaysShowMainTextBoxCaret;
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

        if (ConfigManager.Instance.Focusable)
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
            : MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false) >= 0
              || (FirstPopupWindow.LastSelectedText is not null
                  && MainTextBox.Text.StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
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
                  && MainTextBox.Text.StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
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
                  && MainTextBox.Text.StartsWith(FirstPopupWindow.LastSelectedText, StringComparison.Ordinal))
                ? FirstPopupWindow.LastSelectedText
                : null;

        WindowsUtils.SearchWithBrowser(text);
        WindowsUtils.UpdateMainWindowVisibility();
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
            PopupWindowUtils.ShowMiningModeResults(FirstPopupWindow);
        }
        else if (e.ChangedButton is not MouseButton.Right)
        {
            PopupWindowUtils.HidePopups(FirstPopupWindow);
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
            WindowsUtils.SetSizeToContent(configManager.PopupDynamicWidth, configManager.PopupDynamicHeight, configManager.PopupMaxWidth, configManager.PopupMaxHeight, configManager.PopupMinWidth, configManager.PopupMinHeight, currentPopupWindow);
            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
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
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs? e)
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

            if (SizeToContent is not SizeToContent.Manual)
            {
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
                MinWidth = 100;
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
            PopupWindowUtils.HidePopups(FirstPopupWindow);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton is MouseButtonState.Pressed)
        {
            DragMove();
        }

        ConfigManager configManager = ConfigManager.Instance;
        if (e.ClickCount is 2
            && configManager is { MainWindowDynamicWidth: false, MainWindowDynamicHeight: true })
        {
            if (MagpieUtils.IsMagpieScaling)
            {
                // If Magpie crashes or is killed during the process of scaling a window,
                // JL will not receive the MagpieScalingChangedWindowMessage.
                // Consequently, IsMagpieScaling may not be set to false.
                // To ensure Magpie is still running, we must re-check whether it is scaling a window.
                MagpieUtils.IsMagpieScaling = MagpieUtils.IsMagpieReallyScaling();
            }

            DpiScale dpi = WindowsUtils.Dpi;
            double xPosition;
            double yPosition;
            double width;
            double maxDynamicHeight = configManager.MainWindowMaxDynamicHeight * dpi.DpiScaleY;
            if (!MagpieUtils.IsMagpieScaling)
            {
                Rectangle workingArea = WindowsUtils.ActiveScreen.WorkingArea;
                xPosition = workingArea.X;

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

                width = workingArea.Width / dpi.DpiScaleX;
            }
            else
            {
                xPosition = MagpieUtils.MagpieWindowLeftEdgePosition;

                if (configManager.PositionPopupAboveCursor)
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

                width = MagpieUtils.DpiAwareMagpieWindowWidth;
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

        int charIndex = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
        ContextMenuIsOpening = charIndex >= MainTextBox.SelectionStart && charIndex <= MainTextBox.SelectionStart + MainTextBox.SelectionLength;

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
            || (!configManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On))
        {
            return;
        }

        if (Background.Opacity is not 0)
        {
            if (configManager.TextOnlyVisibleOnHover)
            {
                MainGrid.Opacity = 0;
            }

            if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
            {
                Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
            }
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (IsMouseOver
            || FirstPopupWindow.MiningMode
            || (FirstPopupWindow.IsMouseOver
                && (ConfigManager.Instance.FixedPopupPositioning
                    || FirstPopupWindow.UnavoidableMouseEnter)))
        {
            return;
        }

        FirstPopupWindow.HidePopup();
    }

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1;
        }

        if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover && Background.Opacity is not 0)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }

        if (!FirstPopupWindow.IsVisible
            && configManager is { Focusable: true, MainWindowFocusOnHover: true })
        {
            _ = Activate();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        ConfigManager configManager = ConfigManager.Instance;
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (WindowState is WindowState.Minimized)
        {
            if (configManager.StopIncreasingTimeStatWhenMinimized)
            {
                StatsUtils.StatsStopWatch.Stop();
            }

            if (configManager.GlobalHotKeys)
            {
                List<int> keyGestureIdsToIgnore = new(KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized.Length);
                for (int i = 0; i < KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized.Length; i++)
                {
                    if (KeyGestureUtils.GlobalKeyGestureNameToIntDict.TryGetValue(KeyGestureUtils.NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized[i], out int id))
                    {
                        keyGestureIdsToIgnore.Add(id);
                    }
                }

                if (keyGestureIdsToIgnore.Count > 0)
                {
                    WinApi.UnregisterAllGlobalHotKeys(WindowHandle, keyGestureIdsToIgnore);
                }
                else
                {
                    WinApi.UnregisterAllGlobalHotKeys(WindowHandle);
                }
            }
        }

        else
        {
            if (configManager.StopIncreasingTimeStatWhenMinimized
                && (coreConfigManager.CaptureTextFromClipboard || (coreConfigManager.CaptureTextFromWebSocket && WebSocketUtils.Connected)))
            {
                StatsUtils.StatsStopWatch.Start();
            }

            if (configManager.GlobalHotKeys)
            {
                WinApi.RegisterAllGlobalHotKeys(WindowHandle);
            }

            if (SizeToContent is SizeToContent.Manual && (configManager.MainWindowDynamicHeight || configManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContent(configManager.MainWindowDynamicWidth, configManager.MainWindowDynamicHeight, configManager.MainWindowMaxDynamicWidth, configManager.MainWindowMaxDynamicHeight, configManager.MainWindowMinDynamicWidth, configManager.MainWindowMinDynamicHeight, configManager.MainWindowWidth, configManager.MainWindowHeight, this);
            }

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

        MainTextBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(MainTextBox)!, 0, key)
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
        if (!FirstPopupWindow.IsVisible && MainTextBox.SelectionLength is 0)
        {
            Swipe(e.GetTouchPoint(this).Position);
        }
    }

    private void TitleBar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        PopupWindowUtils.HidePopups(FirstPopupWindow);
    }

    public void UpdatePosition()
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true }
            or { RepositionMainWindowOnTextChangeByRightPosition: true, MainWindowDynamicWidth: true })
        {
            UpdateLayout();

            DpiScale dpi = WindowsUtils.Dpi;
            double newTop = Top * dpi.DpiScaleY;
            if (configManager is { RepositionMainWindowOnTextChangeByBottomPosition: true, MainWindowDynamicHeight: true })
            {
                newTop = GetDynamicYPosition(configManager.MainWindowFixedBottomPosition);
            }

            double newLeft = Left * dpi.DpiScaleX;
            if (configManager is { RepositionMainWindowOnTextChangeByRightPosition: true, MainWindowDynamicWidth: true })
            {
                newLeft = GetDynamicXPosition(configManager.MainWindowFixedRightPosition);
            }

            WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);

            LeftPositionBeforeResolutionChange = Left;
            TopPositionBeforeResolutionChange = Top;
            HeightBeforeResolutionChange = Height;
            WidthBeforeResolutionChange = Width;
        }
    }

    private double GetDynamicXPosition(double rightPosition)
    {
        double currentWidth = ActualWidth * WindowsUtils.Dpi.DpiScaleX;

        Screen activeScreen = WindowsUtils.ActiveScreen;
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

        return Math.Max(rightPosition is not -1 ? activeScreen.Bounds.Left : activeScreen.WorkingArea.Left, rightPosition - currentWidth);
    }

    private double GetDynamicYPosition(double bottomPosition)
    {
        double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;

        Screen activeScreen = WindowsUtils.ActiveScreen;
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

        return Math.Max(bottomPosition is not -1 ? activeScreen.Bounds.Top : activeScreen.WorkingArea.Top, bottomPosition - currentHeight);
    }
}
