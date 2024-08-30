using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using DpiChangedEventArgs = System.Windows.DpiChangedEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Window = System.Windows.Window;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
#pragma warning disable CA1812 // Internal class that is apparently never instantiated
internal sealed partial class MainWindow : Window
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

    private static ulong s_clipboardSequenceNo;
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

        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(true))
        {
            await ConfigMigrationManager.MigrateConfig(connection).ConfigureAwait(true);
        }

        SqliteConnection readOnlyConnection = ConfigDBManager.CreateReadOnlyDBConnection();
        await using (readOnlyConnection.ConfigureAwait(true))
        {
            ProfileDBUtils.SetCurrentProfileFromDB(readOnlyConnection);
            StatsDBUtils.SetStatsFromDB(readOnlyConnection);
        }

        ConfigManager.ApplyPreferences();

        RegexReplacerUtils.PopulateRegexReplacements();

        if (ConfigManager.AlwaysOnTop)
        {
            WinApi.BringToFront(WindowHandle);
        }

        if (CoreConfigManager.CaptureTextFromClipboard)
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

                return CopyText(text);
            }
            catch (ExternalException ex)
            {
                Utils.Logger.Warning(ex, "CopyFromClipboard failed");
            }
        }

        return false;
    }

    public Task CopyFromWebSocket(string text)
    {
        return CopyText(text)
            ? Dispatcher.Invoke(() => ConfigManager.AutoLookupFirstTermWhenTextIsCopiedFromWebSocket
                                      && (!ConfigManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                                          || WindowState is WindowState.Minimized)
                ? FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true)
                : Task.CompletedTask)
            : Task.CompletedTask;
    }

    private bool CopyText(string text)
    {
        if (ConfigManager.OnlyCaptureTextWithJapaneseChars && !JapaneseUtils.JapaneseRegex().IsMatch(text))
        {
            return false;
        }

        string sanitizedText = TextUtils.SanitizeText(text);
        if (sanitizedText.Length is 0)
        {
            return false;
        }

        bool mergeTexts = false;
        string? subsequentText = null;

        if (ConfigManager.MergeSequentialTextsWhenTheyMatch)
        {
            DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());

            mergeTexts = (ConfigManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds is 0
                          || (preciseTimeNow - s_lastTextCopyTime).TotalMilliseconds < ConfigManager.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds)
                         && sanitizedText.StartsWith(MainTextBox.Text, StringComparison.Ordinal);

            s_lastTextCopyTime = preciseTimeNow;

            if (mergeTexts)
            {
                subsequentText = sanitizedText[MainTextBox.Text.Length..];
            }
        }

        Dispatcher.Invoke(() =>
        {
            if (mergeTexts)
            {
                MainTextBox.AppendText(subsequentText);
            }
            else
            {
                MainTextBox.Text = sanitizedText;
            }

            MainTextBox.Foreground = ConfigManager.MainWindowTextColor;

            if (!mergeTexts && SizeToContent is SizeToContent.Manual
                            && (ConfigManager.MainWindowDynamicHeight || ConfigManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContent(ConfigManager.MainWindowDynamicWidth, ConfigManager.MainWindowDynamicHeight, ConfigManager.MainWindowMaxDynamicWidth, ConfigManager.MainWindowMaxDynamicHeight, ConfigManager.MainWindowWidth, ConfigManager.MainWindowHeight, this);
            }

            TitleBarContextMenu.IsOpen = false;
            MainTextBoxContextMenu.IsOpen = false;

            if (ConfigManager.HidePopupsOnTextChange)
            {
                PopupWindowUtils.HidePopups(FirstPopupWindow);
            }

            BringToFront();
        }, DispatcherPriority.Send);

        WindowsUtils.HandlePostCopy(sanitizedText, subsequentText);

        return true;
    }

    public void BringToFront()
    {
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
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ClipboardChanged(object? sender, EventArgs? e)
    {
        ulong currentClipboardSequenceNo = WinApi.GetClipboardSequenceNo();
        if (s_clipboardSequenceNo == currentClipboardSequenceNo)
        {
            return;
        }

        s_clipboardSequenceNo = currentClipboardSequenceNo;
        bool gotTextFromClipboard = CopyFromClipboard();

        if (gotTextFromClipboard
            && ConfigManager.AutoLookupFirstTermWhenTextIsCopiedFromClipboard
            && (!ConfigManager.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized
                || WindowState is WindowState.Minimized))
        {
            await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true).ConfigureAwait(false);
        }
    }

    public Task HandleMouseMove(MouseEventArgs? e)
    {
        return ConfigManager.InactiveLookupMode
               || ConfigManager.LookupOnSelectOnly
               || ConfigManager.LookupOnMouseClickOnly
               || e?.LeftButton is MouseButtonState.Pressed
               || MainTextBoxContextMenu.IsVisible
               || TitleBarContextMenu.IsVisible
               || FontSizeSlider.IsVisible
               || OpacitySlider.IsVisible
               || FirstPopupWindow.MiningMode
               || (!ConfigManager.TextBoxIsReadOnly && InputMethod.Current?.ImeState is InputMethodState.On)
               || (ConfigManager.RequireLookupKeyPress && !ConfigManager.LookupKeyKeyGesture.IsPressed())
            ? Task.CompletedTask
            : FirstPopupWindow.LookupOnMouseMoveOrClick(MainTextBox);
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

    // ReSharper disable once AsyncVoidMethod
    private async void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        await HandleAppClosing().ConfigureAwait(false);
    }

    public async Task HandleAppClosing()
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        MagpieUtils.UnmarkWindowAsMagpieToolWindow(WindowHandle);
        ConfigManager.SaveBeforeClosing();
        Stats.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks);

        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(false))
        {
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

        if (keyGesture.IsEqual(ConfigManager.SteppedBacklogBackwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowPreviousBacklogItem();
        }

        else if (keyGesture.IsEqual(ConfigManager.SteppedBacklogForwardsKeyGesture))
        {
            handled = true;

            BacklogUtils.ShowNextBacklogItem();
        }

        else if (keyGesture.IsEqual(ConfigManager.ShowPreferencesWindowKeyGesture))
        {
            handled = true;

            WindowsUtils.ShowPreferencesWindow();
        }

        else if (keyGesture.IsEqual(ConfigManager.MousePassThroughModeKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.KanjiModeKeyGesture))
        {
            handled = true;

            CoreConfigManager.KanjiMode = !CoreConfigManager.KanjiMode;
            FirstPopupWindow.LastText = "";
            MainTextBox_MouseMove(null, null);
        }

        else if (keyGesture.IsEqual(ConfigManager.ShowAddNameWindowKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.ShowAddWordWindowKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.ShowManageDictionariesWindowKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.ShowManageFrequenciesWindowKeyGesture))
        {
            handled = true;

            if (FreqUtils.FreqsReady)
            {
                await WindowsUtils.ShowManageFrequenciesWindow().ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.SearchWithBrowserKeyGesture))
        {
            handled = true;

            WindowsUtils.SearchWithBrowser(MainTextBox.SelectedText);
            WindowsUtils.UpdateMainWindowVisibility();
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

        else if (keyGesture.IsEqual(ConfigManager.ClosePopupKeyGesture))
        {
            handled = true;

            FirstPopupWindow.HidePopup();
        }

        else if (keyGesture.IsEqual(ConfigManager.ShowStatsKeyGesture))
        {
            handled = true;

            WindowsUtils.ShowStatsWindow();
        }

        else if (keyGesture.IsEqual(ConfigManager.ShowManageAudioSourcesWindowKeyGesture))
        {
            handled = true;

            await WindowsUtils.ShowManageAudioSourcesWindow().ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(ConfigManager.AlwaysOnTopKeyGesture))
        {
            handled = true;

            ConfigManager.AlwaysOnTop = !ConfigManager.AlwaysOnTop;

            Topmost = ConfigManager.AlwaysOnTop;
        }

        else if (keyGesture.IsEqual(ConfigManager.CaptureTextFromClipboardKeyGesture))
        {
            handled = true;

            CoreConfigManager.CaptureTextFromClipboard = !CoreConfigManager.CaptureTextFromClipboard;
            if (CoreConfigManager.CaptureTextFromClipboard)
            {
                WinApi.SubscribeToClipboardChanged(WindowHandle);
            }
            else
            {
                WinApi.UnsubscribeFromClipboardChanged(WindowHandle);
            }

            if (!CoreConfigManager.CaptureTextFromWebSocket && !CoreConfigManager.CaptureTextFromClipboard)
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

        else if (keyGesture.IsEqual(ConfigManager.CaptureTextFromWebSocketKeyGesture))
        {
            handled = true;

            CoreConfigManager.CaptureTextFromWebSocket = !CoreConfigManager.CaptureTextFromWebSocket;
            WebSocketUtils.HandleWebSocket();

            if (!CoreConfigManager.CaptureTextFromWebSocket && !CoreConfigManager.CaptureTextFromClipboard)
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

        else if (keyGesture.IsEqual(ConfigManager.ReconnectToWebSocketServerKeyGesture))
        {
            handled = true;

            if (!WebSocketUtils.Connected)
            {
                CoreConfigManager.CaptureTextFromWebSocket = true;

                if (!StatsUtils.StatsStopWatch.IsRunning)
                {
                    StatsUtils.StatsStopWatch.Start();
                    StatsUtils.StartStatsTimer();
                }

                WebSocketUtils.HandleWebSocket();
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.TextBoxIsReadOnlyKeyGesture))
        {
            handled = true;

            ConfigManager.TextBoxIsReadOnly = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.UndoLimit = ConfigManager.TextBoxIsReadOnly ? 0 : -1;
        }

        else if (keyGesture.IsEqual(ConfigManager.DeleteCurrentLineKeyGesture))
        {
            handled = true;

            BacklogUtils.DeleteCurrentLine();
        }

        else if (keyGesture.IsEqual(ConfigManager.ToggleMinimizedStateKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.SelectedTextToSpeechKeyGesture))
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

        else if (keyGesture.IsEqual(ConfigManager.MoveCaretLeftKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Left);
        }

        else if (keyGesture.IsEqual(ConfigManager.MoveCaretRightKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Right);
        }

        else if (keyGesture.IsEqual(ConfigManager.MoveCaretUpKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Up);
        }

        else if (keyGesture.IsEqual(ConfigManager.MoveCaretDownKeyGesture))
        {
            handled = true;

            MoveCaret(Key.Down);
        }

        else if (keyGesture.IsEqual(ConfigManager.LookupTermAtCaretIndexKeyGesture))
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
                    await FirstPopupWindow.LookupOnCharPosition(MainTextBox, MainTextBox.CaretIndex, true).ConfigureAwait(false);
                }
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.LookupFirstTermKeyGesture))
        {
            handled = true;

            if (MainTextBox.Text.Length > 0)
            {
                await FirstPopupWindow.LookupOnCharPosition(MainTextBox, 0, true).ConfigureAwait(false);
            }
        }

        else if (keyGesture.IsEqual(ConfigManager.LookupSelectedTextKeyGesture))
        {
            handled = true;

            await FirstPopupWindow.LookupOnSelect(MainTextBox).ConfigureAwait(false);
        }

        else if (keyGesture.IsEqual(ConfigManager.ToggleAlwaysShowMainTextBoxCaretKeyGesture))
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

    // ReSharper disable once AsyncVoidMethod
    private async void MainTextBox_PreviewMouseUp(object? sender, MouseButtonEventArgs e)
    {
        if (ConfigManager.InactiveLookupMode
            || (ConfigManager.RequireLookupKeyPress && !ConfigManager.LookupKeyKeyGesture.IsPressed())
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

    private void AdjustWindowsSize(Screen previousActiveScreen, DpiScale previousDpi)
    {
        double oldDpiAwareScreenWidth = previousActiveScreen.Bounds.Width / previousDpi.DpiScaleX;
        double oldDpiAwareScreenHeight = previousActiveScreen.Bounds.Height / previousDpi.DpiScaleY;

        double newDpiAwareScreenWidth = WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX;
        double newDpiAwareScreenHeight = WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY;

        double ratioX = oldDpiAwareScreenWidth / newDpiAwareScreenWidth;
        double ratioY = oldDpiAwareScreenHeight / newDpiAwareScreenHeight;

        if (ratioX is 1 && ratioY is 1)
        {
            return;
        }

        ConfigManager.MainWindowMaxDynamicHeight = Math.Round(ConfigManager.MainWindowMaxDynamicHeight / ratioY);
        ConfigManager.MainWindowMaxDynamicWidth = Math.Round(ConfigManager.MainWindowMaxDynamicWidth / ratioX);
        if (SizeToContent is not SizeToContent.Manual)
        {
            if (ConfigManager.MainWindowDynamicHeight)
            {
                MaxHeight = ConfigManager.MainWindowMaxDynamicHeight;
            }
            if (ConfigManager.MainWindowDynamicWidth)
            {
                MaxWidth = ConfigManager.MainWindowMaxDynamicWidth;
            }
        }

        Width = Math.Round(Math.Min(WidthBeforeResolutionChange / ratioX, newDpiAwareScreenWidth));
        WidthBeforeResolutionChange = Width;

        Height = Math.Round(Math.Min(HeightBeforeResolutionChange / ratioY, newDpiAwareScreenHeight));
        HeightBeforeResolutionChange = Height;

        double newLeft = Math.Round(LeftPositionBeforeResolutionChange * previousDpi.DpiScaleX / ratioX);
        if (WindowsUtils.ActiveScreen.Bounds.X > newLeft)
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.X;
        }
        else if (newLeft > WindowsUtils.ActiveScreen.Bounds.Right)
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.Right - (Width * WindowsUtils.Dpi.DpiScaleX);
        }

        double newTop = Math.Round(TopPositionBeforeResolutionChange * previousDpi.DpiScaleY / ratioY);
        if (WindowsUtils.ActiveScreen.Bounds.Y > newTop)
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Y;
        }
        else if (newTop > WindowsUtils.ActiveScreen.Bounds.Bottom)
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Bottom - (Height * WindowsUtils.Dpi.DpiScaleY);
        }

        WinApi.MoveWindowToPosition(WindowHandle, newLeft, newTop);

        ConfigManager.PopupMaxHeight = Math.Round(Math.Min(ConfigManager.PopupMaxHeight / ratioY, newDpiAwareScreenHeight));
        ConfigManager.PopupMaxWidth = Math.Round(Math.Min(ConfigManager.PopupMaxWidth / ratioX, newDpiAwareScreenWidth));

        ConfigManager.PopupYOffset = Math.Round(ConfigManager.PopupYOffset / ratioY);
        ConfigManager.PopupXOffset = Math.Round(ConfigManager.PopupXOffset / ratioX);
        WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset * WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset * WindowsUtils.Dpi.DpiScaleY;

        ConfigManager.FixedPopupYPosition = Math.Round(ConfigManager.FixedPopupYPosition / ratioY);
        if (WindowsUtils.ActiveScreen.Bounds.Y > ConfigManager.FixedPopupYPosition)
        {
            ConfigManager.FixedPopupYPosition = WindowsUtils.ActiveScreen.Bounds.Y;
        }
        else if (ConfigManager.FixedPopupYPosition > WindowsUtils.ActiveScreen.Bounds.Bottom)
        {
            ConfigManager.FixedPopupYPosition = WindowsUtils.ActiveScreen.Bounds.Bottom - (ConfigManager.PopupMaxHeight * WindowsUtils.Dpi.DpiScaleY);
        }

        ConfigManager.FixedPopupXPosition = Math.Round(ConfigManager.FixedPopupXPosition / ratioX);
        if (WindowsUtils.ActiveScreen.Bounds.X > ConfigManager.FixedPopupXPosition)
        {
            ConfigManager.FixedPopupXPosition = WindowsUtils.ActiveScreen.Bounds.X;
        }
        else if (ConfigManager.FixedPopupXPosition > WindowsUtils.ActiveScreen.Bounds.Right)
        {
            ConfigManager.FixedPopupXPosition = WindowsUtils.ActiveScreen.Bounds.Right - (ConfigManager.PopupMaxWidth * WindowsUtils.Dpi.DpiScaleX);
        }

        PopupWindow? currentPopupWindow = FirstPopupWindow;
        while (currentPopupWindow is not null)
        {
            WindowsUtils.SetSizeToContent(ConfigManager.PopupDynamicWidth, ConfigManager.PopupDynamicHeight, ConfigManager.PopupMaxWidth, ConfigManager.PopupMaxHeight, currentPopupWindow);
            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }

        if (ConfigManager.AutoAdjustFontSizesOnResolutionChange)
        {
            double fontScale = ratioX * ratioY > 1
                ? Math.Min(ratioX, ratioY) * 0.75
                : Math.Max(ratioX, ratioY) / 0.75;

            FontSizeSlider.Value = Math.Round(FontSizeSlider.Value / fontScale);

            ConfigManager.AlternativeSpellingsFontSize = Math.Round(ConfigManager.AlternativeSpellingsFontSize / fontScale);
            ConfigManager.DeconjugationInfoFontSize = Math.Round(ConfigManager.DeconjugationInfoFontSize / fontScale);
            ConfigManager.DefinitionsFontSize = Math.Round(ConfigManager.DefinitionsFontSize / fontScale);
            ConfigManager.DictTypeFontSize = Math.Round(ConfigManager.DictTypeFontSize / fontScale);
            ConfigManager.FrequencyFontSize = Math.Round(ConfigManager.FrequencyFontSize / fontScale);
            ConfigManager.PrimarySpellingFontSize = Math.Round(ConfigManager.PrimarySpellingFontSize / fontScale);
            ConfigManager.ReadingsFontSize = Math.Round(ConfigManager.ReadingsFontSize / fontScale);
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs? e)
    {
        _ = Dispatcher.Invoke(DispatcherPriority.Background, () =>
        {
            Screen previousActiveScreen = WindowsUtils.ActiveScreen;

            WindowsUtils.ActiveScreen = Screen.FromHandle(WindowHandle);
            WindowsUtils.Dpi = VisualTreeHelper.GetDpi(this);
            WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset * WindowsUtils.Dpi.DpiScaleX;
            WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset * WindowsUtils.Dpi.DpiScaleY;

            if (Math.Abs(previousActiveScreen.Bounds.Width - WindowsUtils.ActiveScreen.Bounds.Width) <= 1 && Math.Abs(previousActiveScreen.Bounds.Height - WindowsUtils.ActiveScreen.Bounds.Height) <= 1)
            {
                return;
            }

            DpiScale previousDpi = s_previousDpi ?? WindowsUtils.Dpi;
            s_previousDpi = null;

            AdjustWindowsSize(previousActiveScreen, previousDpi);
        });
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        WindowsUtils.Dpi = e.NewDpi;
        WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset * e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset * e.NewDpi.DpiScaleY;
        s_previousDpi = e.OldDpi;
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        if (WindowsUtils.ActiveScreen.DeviceName == Screen.FromHandle(WindowHandle).DeviceName)
        {
            return;
        }

        WindowsUtils.ActiveScreen = Screen.FromHandle(WindowHandle);
        WindowsUtils.Dpi = VisualTreeHelper.GetDpi(this);
        WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset * WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset * WindowsUtils.Dpi.DpiScaleY;
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
            if (MagpieUtils.IsMagpieScaling)
            {
                // If Magpie crashes or is killed during the process of scaling a window,
                // JL will not receive the MagpieScalingChangedWindowMessage.
                // Consequently, IsMagpieScaling may not be set to false.
                // To ensure Magpie is still running, we must re-check whether it is scaling a window.
                MagpieUtils.IsMagpieScaling = MagpieUtils.IsMagpieReallyScaling();
            }

            double width;
            if (!MagpieUtils.IsMagpieScaling)
            {
                WinApi.MoveWindowToPosition(WindowHandle, WindowsUtils.ActiveScreen.Bounds.X, WindowsUtils.ActiveScreen.Bounds.Y);
                width = WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX;
            }
            else
            {
                WinApi.MoveWindowToPosition(WindowHandle, MagpieUtils.MagpieWindowLeftEdgePosition, MagpieUtils.MagpieWindowTopEdgePosition);
                width = MagpieUtils.DpiAwareMagpieWindowWidth;
            }

            if (ConfigManager.MainWindowMaxDynamicWidth < width)
            {
                ConfigManager.MainWindowMaxDynamicWidth = width;
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
            if (ConfigManager.StopIncreasingTimeStatWhenMinimized
                && (CoreConfigManager.CaptureTextFromClipboard || (CoreConfigManager.CaptureTextFromWebSocket && WebSocketUtils.Connected)))
            {
                StatsUtils.StatsStopWatch.Start();
            }

            if (ConfigManager.GlobalHotKeys)
            {
                WinApi.RegisterAllHotKeys(WindowHandle);
            }

            if (ConfigManager.AlwaysOnTop)
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
}
