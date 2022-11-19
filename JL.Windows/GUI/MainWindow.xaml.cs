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
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Win32;
using MessageBox = HandyControl.Controls.MessageBox;
using Window = System.Windows.Window;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IFrontend
{
    #region IFrontend implementation

    public CoreConfig CoreConfig { get; set; } = ConfigManager.Instance;

    public void PlayAudio(byte[] sound, float volume) => WindowsUtils.PlayAudio(sound, volume);

    public void Alert(AlertLevel alertLevel, string message) => WindowsUtils.Alert(alertLevel, message);

    public bool ShowYesNoDialog(string text, string caption)
    {
        return MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public void ShowOkDialog(string text, string caption)
    {
        MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public Task UpdateJL(Uri downloadUrlOfLatestJLRelease) => WindowsUtils.UpdateJL(downloadUrlOfLatestJLRelease);

    public void InvalidateDisplayCache() => PopupWindow.StackPanelCache.Clear();

    #endregion

    private readonly List<string> _backlog = new();

    private int _currentTextIndex;

    private DateTime _lastClipboardChangeTime;
    private WinApi? _winApi;

    public PopupWindow FirstPopupWindow { get; init; } = new();

    private static MainWindow? s_instance;
    public static MainWindow Instance
    {
        get { return s_instance ??= new(); }
    }

    public double LeftPositionBeforeResolutionChange { get; set; }
    public double TopPositionBeforeResolutionChange { get; set; }
    public double HeightBeforeResolutionChange { get; set; }
    public double WidthBeforeResolutionChange { get; set; }

    private CancellationTokenSource PrecacheCancellationToken { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        s_instance = this;
        ConfigHelper.Instance.SetLang("en");
        MainTextBox.Focus();
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            Exception ex = (Exception)eventArgs.ExceptionObject;
            Utils.Logger.Error("{UnhandledExceptionMessage}", ex.ToString());
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            Utils.Logger.Error("{UnobservedTaskExceptionMessage}", eventArgs.Exception.ToString());
        };

        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;

        _winApi = new(this);
        _winApi.ClipboardChanged += ClipboardChanged;

        ConfigManager.Instance.ApplyPreferences();

        if (ConfigManager.CaptureTextFromClipboard)
        {
            CopyFromClipboard();
            _lastClipboardChangeTime = new(Stopwatch.GetTimestamp());
        }

        await WindowsUtils.InitializeMainWindow().ConfigureAwait(false);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (!ConfigManager.Focusable)
        {
            _winApi!.PreventFocus();
        }
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
                if (!ConfigManager.OnlyCaptureTextWithJapaneseCharsFromClipboard || Storage.JapaneseRegex.IsMatch(text))
                {
                    if (ConfigManager.TextBoxTrimWhiteSpaceCharacters)
                    {
                        text = text.Trim();
                    }

                    if (ConfigManager.TextBoxRemoveNewlines)
                    {
                        text = text.ReplaceLineEndings("");
                    }

                    MainTextBox!.Text = text;
                    MainTextBox.Foreground = ConfigManager.MainWindowTextColor;

                    _backlog.Add(text);
                    _currentTextIndex = _backlog.Count - 1;
                    Stats.IncrementStat(StatType.Characters, new StringInfo(text).LengthInTextElements);
                    Stats.IncrementStat(StatType.Lines);

                    if (Storage.DictsReady && !Storage.UpdatingJMdict && !Storage.UpdatingJMnedict && !Storage.UpdatingKanjidic && Storage.FreqsReady
                        && ConfigManager.Precaching && MainTextBox!.Text.Length < Storage.CacheSize)
                    {
                        Dispatcher.Invoke(DispatcherPriority.Render, delegate () { }); // let MainTextBox text update
                        await Precache(MainTextBox!.Text).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Warning(e, "CopyFromClipboard failed");
            }
        }
    }

    private async Task Precache(string input)
    {
        //if (Debugger.IsAttached)
        //    PrecacheProgress.Visibility = Visibility.Visible;

        int added = 0;
        for (int charPosition = 0; charPosition < input.Length; charPosition++)
        {
            if (PrecacheCancellationToken.IsCancellationRequested)
            {
                if (charPosition == 0)
                {
                    PrecacheCancellationToken = new CancellationTokenSource();
                }
                else
                {
                    PrecacheCancellationToken = new CancellationTokenSource();
                    return;
                }
            }

            if (charPosition > 0 && char.IsHighSurrogate(input[charPosition - 1]))
                --charPosition;

            //PrecacheProgress.Text = $"{charPosition + 1}/{input.Length} ({added} new)";
            if (charPosition % 10 == 0)
            {
                await Task.Delay(1); // let user interact with the GUI while this method is running
            }

            int endPosition = input.Length - charPosition > ConfigManager.MaxSearchLength
                ? Utils.FindWordBoundary(input[..(charPosition + ConfigManager.MaxSearchLength)], charPosition)
                : Utils.FindWordBoundary(input, charPosition);

            string text = input[charPosition..endPosition];

            if (!PopupWindow.StackPanelCache.Contains(text))
            {
                List<LookupResult>? lookupResults = Lookup.LookupText(text);
                if (lookupResults is { Count: > 0 })
                {
                    var stackPanels = new List<StackPanel>();
                    for (int i = 0; i < lookupResults.Count; i++)
                    {
                        if (i > ConfigManager.MaxNumResultsNotInMiningMode)
                        {
                            break;
                        }

                        LookupResult lookupResult = lookupResults[i];
                        StackPanel stackPanel = FirstPopupWindow.MakeResultStackPanel(lookupResult, i, lookupResults.Count);
                        stackPanels.Add(stackPanel);
                    }

                    PopupWindow.StackPanelCache.AddReplace(text, stackPanels.ToArray());
                    added += added == 0 ? 2 : 1;
                }
            }
        }
    }

    private void ClipboardChanged(object? sender, EventArgs? e)
    {
        if (!ConfigManager.CaptureTextFromClipboard)
            return;

        DateTime currentTime = new(Stopwatch.GetTimestamp());

        if ((currentTime - _lastClipboardChangeTime).TotalMilliseconds > 5)
        {
            _lastClipboardChangeTime = currentTime;
            CopyFromClipboard();

            if (SizeToContent == SizeToContent.Manual && (ConfigManager.MainWindowDynamicHeight || ConfigManager.MainWindowDynamicWidth))
            {
                WindowsUtils.SetSizeToContentForMainWindow(ConfigManager.MainWindowDynamicWidth, ConfigManager.MainWindowDynamicHeight,
                    ConfigManager.MainWindowMaxDynamicWidth, ConfigManager.MainWindowMaxDynamicHeight, ConfigManager.MainWindowWidth, ConfigManager.MainWindowHeight, this);
            }

            if (ConfigManager.AlwaysOnTop
                && !FirstPopupWindow.IsVisible
                && !ManageDictionariesWindow.IsItVisible()
                && !ManageFrequenciesWindow.IsItVisible()
                && !AddNameWindow.IsItVisible()
                && !AddWordWindow.IsItVisible()
                && !PreferencesWindow.IsItVisible()
                && !StatsWindow.IsItVisible()
                && !MainTextboxContextMenu.IsVisible)
            {
                _winApi!.KeepTopmost();
            }
        }
    }

    public void MainTextBox_MouseMove(object? sender, MouseEventArgs? e)
    {
        if (ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnLeftClickOnly
            || Background!.Opacity == 0
            || MainTextboxContextMenu!.IsVisible
            || FontSizeSlider!.IsVisible
            || OpacitySlider!.IsVisible
            || FirstPopupWindow.MiningMode
            || (ConfigManager.RequireLookupKeyPress && !WindowsUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        PrecacheCancellationToken.Cancel();
        FirstPopupWindow.TextBox_MouseMove(MainTextBox!);

        if (ConfigManager.FixedPopupPositioning)
        {
            FirstPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
        }

        else
        {
            FirstPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
        }
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        Application.Current!.Shutdown();
    }

    private void MainTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0 && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            string allBacklogText = string.Join("\n", _backlog);
            if (MainTextBox!.Text != allBacklogText)
            {
                if (MainTextBox.GetFirstVisibleLineIndex() == 0)
                {
                    int caretIndex = allBacklogText.Length - MainTextBox.Text.Length;

                    MainTextBox.Text = allBacklogText;
                    MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;

                    if (caretIndex >= 0)
                        MainTextBox.CaretIndex = caretIndex;

                    MainTextBox.ScrollToEnd();
                }
            }
        }

        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Delta > 0)
        {
            FontSizeSlider!.Value += 5;
        }

        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Delta < 0)
        {
            FontSizeSlider!.Value -= 5;
        }
    }

    private void MinimizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        OpacitySlider!.Visibility = Visibility.Collapsed;
        FontSizeSlider!.Visibility = Visibility.Collapsed;
        WindowState = WindowState.Minimized;
    }

    private void Button_MouseEnter(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).Foreground = Brushes.SteelBlue;
    }

    private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
    {
        if (FirstPopupWindow.MiningMode
            || ConfigManager.LookupOnSelectOnly
            || ConfigManager.LookupOnLeftClickOnly
            || ConfigManager.FixedPopupPositioning
            || (FirstPopupWindow.UnavoidableMouseEnter && FirstPopupWindow.IsMouseOver))
        {
            return;
        }

        FirstPopupWindow.Hide();
        FirstPopupWindow.LastText = "";

        if (ConfigManager.HighlightLongestMatch && !MainTextboxContextMenu.IsVisible)
        {
            WindowsUtils.Unselect(MainTextBox);
        }
    }

    private void Button_MouseLeave(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).Foreground = Brushes.White;
    }

    private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    private void OpacityButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        FontSizeSlider!.Visibility = Visibility.Collapsed;

        if (Background!.Opacity == 0)
        {
            Background.Opacity = OpacitySlider!.Value / 100;
            MainTextBox.Focus();
        }

        else if (OpacitySlider!.Visibility == Visibility.Collapsed)
        {
            OpacitySlider.Visibility = Visibility.Visible;
            OpacitySlider.Focus();
        }

        else
            OpacitySlider.Visibility = Visibility.Collapsed;
    }

    private void FontSizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpacitySlider!.Visibility = Visibility.Collapsed;

        if (FontSizeSlider!.Visibility == Visibility.Collapsed)
        {
            FontSizeSlider.Visibility = Visibility.Visible;
            FontSizeSlider.Focus();
        }

        else
            FontSizeSlider.Visibility = Visibility.Collapsed;
    }

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ConfigManager.SaveBeforeClosing();

        Stats.IncrementStat(StatType.Time, Storage.StatsStopWatch.ElapsedTicks);
        await Stats.UpdateLifetimeStats().ConfigureAwait(false);
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Background!.Opacity = OpacitySlider!.Value / 100;
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        MainTextBox!.FontSize = FontSizeSlider!.Value;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (WindowsUtils.CompareKeyGesture(e, ConfigManager.DisableHotkeysKeyGesture))
        {
            ConfigManager.DisableHotkeys = !ConfigManager.DisableHotkeys;
        }

        if (ConfigManager.DisableHotkeys)
            return;

        if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowPreferencesWindowKeyGesture))
        {
            WindowsUtils.ShowPreferencesWindow();
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.MousePassThroughModeKeyGesture))
        {
            if (!ConfigManager.InvisibleMode)
            {
                Background!.Opacity = 0;
                Keyboard.ClearFocus();
            }
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.InvisibleToggleModeKeyGesture))
        {
            ConfigManager.InvisibleMode = !ConfigManager.InvisibleMode;
            MainGrid!.Opacity = ConfigManager.InvisibleMode ? 0 : 1;
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.KanjiModeKeyGesture))
        {
            // fixes double toggling KanjiMode
            e.Handled = true;

            ConfigManager.Instance.KanjiMode = !ConfigManager.Instance.KanjiMode;
            FirstPopupWindow.LastText = "";
            Storage.Frontend.InvalidateDisplayCache();
            MainTextBox_MouseMove(null, null);
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowAddNameWindowKeyGesture))
        {
            if (Storage.DictsReady)
                WindowsUtils.ShowAddNameWindow(MainTextBox!.SelectedText);
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowAddWordWindowKeyGesture))
        {
            if (Storage.DictsReady)
                WindowsUtils.ShowAddWordWindow(MainTextBox!.SelectedText);
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowManageDictionariesWindowKeyGesture))
        {
            if (Storage.DictsReady
                && !Storage.UpdatingJMdict
                && !Storage.UpdatingJMnedict
                && !Storage.UpdatingKanjidic)
            {
                WindowsUtils.ShowManageDictionariesWindow();
            }
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowManageFrequenciesWindowKeyGesture))
        {
            if (Storage.FreqsReady)
            {
                WindowsUtils.ShowManageFrequenciesWindow();
            }
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.SearchWithBrowserKeyGesture))
        {
            WindowsUtils.SearchWithBrowser(MainTextBox!.SelectedText);
            WindowsUtils.UpdateMainWindowVisibility();
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.InactiveLookupModeKeyGesture))
        {
            ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.MotivationKeyGesture))
        {
            WindowsUtils.Motivate($"{Storage.ResourcesPath}/Motivation");
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ClosePopupKeyGesture))
        {
            FirstPopupWindow.MiningMode = false;
            FirstPopupWindow.TextBlockMiningModeReminder!.Visibility = Visibility.Collapsed;
            FirstPopupWindow.ItemsControlButtons.Visibility = Visibility.Collapsed;

            FirstPopupWindow.PopUpScrollViewer!.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            FirstPopupWindow.Hide();
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.ShowStatsKeyGesture))
        {
            WindowsUtils.ShowStatsWindow();
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.AlwaysOnTopKeyGesture))
        {
            ConfigManager.AlwaysOnTop = !ConfigManager.AlwaysOnTop;

            Topmost = ConfigManager.AlwaysOnTop;
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.TextOnlyVisibleOnHoverKeyGesture))
        {
            ConfigManager.TextOnlyVisibleOnHover = !ConfigManager.TextOnlyVisibleOnHover;

            if (ConfigManager.TextOnlyVisibleOnHover)
            {
                MainGrid.Opacity = IsMouseOver ? 1 : 0;
            }

            else
            {
                MainGrid.Opacity = 1;
            }
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.CaptureTextFromClipboardKeyGesture))
        {
            ConfigManager.CaptureTextFromClipboard = !ConfigManager.CaptureTextFromClipboard;
        }

        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.TextBoxIsReadOnlyKeyGesture))
        {
            ConfigManager.TextBoxIsReadOnly = !ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsReadOnly = ConfigManager.TextBoxIsReadOnly;
            MainTextBox.IsUndoEnabled = !ConfigManager.TextBoxIsReadOnly;
        }
    }

    private void AddName(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddNameWindow(MainTextBox!.SelectedText);
    }

    private void AddWord(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowAddWordWindow(MainTextBox!.SelectedText);
    }

    private void ShowPreferences(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowPreferencesWindow();
    }

    private void SearchWithBrowser(object sender, RoutedEventArgs e)
    {
        WindowsUtils.SearchWithBrowser(MainTextBox!.SelectedText);
        WindowsUtils.UpdateMainWindowVisibility();
    }

    private void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowManageDictionariesWindow();
    }

    private void ShowManageFrequenciesWindow(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowManageFrequenciesWindow();
    }

    private void ShowStats(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowStatsWindow();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        SteppedBacklog(e);
    }

    private void SteppedBacklog(KeyEventArgs e)
    {
        if (WindowsUtils.CompareKeyGesture(e, ConfigManager.SteppedBacklogBackwardsKeyGesture))
        {
            if (_currentTextIndex != 0)
            {
                _currentTextIndex--;
                MainTextBox!.Foreground = ConfigManager.MainWindowBacklogTextColor;
            }

            MainTextBox!.Text = _backlog[_currentTextIndex];
        }
        else if (WindowsUtils.CompareKeyGesture(e, ConfigManager.SteppedBacklogForwardsKeyGesture))
        {
            if (_currentTextIndex < _backlog.Count - 1)
            {
                _currentTextIndex++;
                MainTextBox!.Foreground = ConfigManager.MainWindowBacklogTextColor;
            }

            if (_currentTextIndex == _backlog.Count - 1)
            {
                MainTextBox!.Foreground = ConfigManager.MainWindowTextColor;
            }

            MainTextBox!.Text = _backlog[_currentTextIndex];
        }
    }

    private void OpacitySlider_LostMouseCapture(object sender, MouseEventArgs e)
    {
        OpacitySlider!.Visibility = Visibility.Collapsed;
    }

    private void OpacitySlider_LostFocus(object sender, RoutedEventArgs e)
    {
        OpacitySlider!.Visibility = Visibility.Collapsed;
    }

    private void FontSizeSlider_LostMouseCapture(object sender, MouseEventArgs e)
    {
        FontSizeSlider!.Visibility = Visibility.Collapsed;
    }

    private void FontSizeSlider_LostFocus(object sender, RoutedEventArgs e)
    {
        FontSizeSlider!.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (double.IsNaN(Height) || double.IsNaN(Width))
            return;

        ConfigManager.MainWindowHeight = Height;
        ConfigManager.MainWindowWidth = Width;
    }

    private void MainTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if ((!ConfigManager.LookupOnSelectOnly && !ConfigManager.LookupOnLeftClickOnly)
            || Background!.Opacity == 0
            || ConfigManager.InactiveLookupMode
            || FirstPopupWindow.MiningMode
            || (ConfigManager.RequireLookupKeyPress && !WindowsUtils.CompareKeyGesture(ConfigManager.LookupKeyKeyGesture)))
        {
            return;
        }

        if (ConfigManager.LookupOnSelectOnly)
        {
            FirstPopupWindow.LookupOnSelect(MainTextBox!);
        }

        else
        {
            FirstPopupWindow.TextBox_MouseMove(MainTextBox!);
        }

        if (ConfigManager.FixedPopupPositioning)
        {
            FirstPopupWindow.UpdatePosition(WindowsUtils.DpiAwareFixedPopupXPosition, WindowsUtils.DpiAwareFixedPopupYPosition);
        }

        else
        {
            FirstPopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        PopupWindow? currentPopupWindow = FirstPopupWindow;

        while (currentPopupWindow != null)
        {
            currentPopupWindow.MiningMode = false;
            currentPopupWindow.TextBlockMiningModeReminder!.Visibility = Visibility.Collapsed;
            currentPopupWindow.ItemsControlButtons.Visibility = Visibility.Collapsed;
            currentPopupWindow.PopUpScrollViewer.ScrollToTop();
            currentPopupWindow.Hide();

            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    private void DisplaySettingsChanged(object? sender, EventArgs? e)
    {
        Size oldResolution = new(WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX, WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY);
        WindowsUtils.ActiveScreen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        WindowsUtils.Dpi = VisualTreeHelper.GetDpi(this);
        WindowsUtils.DpiAwareWorkAreaWidth = WindowsUtils.ActiveScreen.Bounds.Width / WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareWorkAreaHeight = WindowsUtils.ActiveScreen.Bounds.Height / WindowsUtils.Dpi.DpiScaleY;

        if (oldResolution.Width != WindowsUtils.DpiAwareWorkAreaWidth || oldResolution.Height != WindowsUtils.DpiAwareWorkAreaHeight)
        {
            double ratioX = oldResolution.Width / WindowsUtils.DpiAwareWorkAreaWidth;
            double ratioY = oldResolution.Height / WindowsUtils.DpiAwareWorkAreaHeight;

            double fontScale = ratioX * ratioY > 1
                ? Math.Min(ratioX, ratioY) * 0.75
                : Math.Max(ratioX, ratioY) / 0.75;

            FontSizeSlider!.Value = (int)Math.Round(FontSizeSlider.Value / fontScale);

            Left = LeftPositionBeforeResolutionChange / ratioX;
            LeftPositionBeforeResolutionChange = Left;

            Top = TopPositionBeforeResolutionChange / ratioY;
            TopPositionBeforeResolutionChange = Top;

            Width = (int)Math.Round(WidthBeforeResolutionChange / ratioX);
            WidthBeforeResolutionChange = Width;

            Height = (int)Math.Round(HeightBeforeResolutionChange / ratioY);
            HeightBeforeResolutionChange = Height;

            PreferencesWindow.Instance.PopupMaxHeightNumericUpDown!.Maximum = WindowsUtils.ActiveScreen.Bounds.Height;
            PreferencesWindow.Instance.PopupMaxWidthNumericUpDown!.Maximum = WindowsUtils.ActiveScreen.Bounds.Width;
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

            ConfigManager.AlternativeSpellingsFontSize = (int)Math.Round(ConfigManager.AlternativeSpellingsFontSize / fontScale);
            ConfigManager.DeconjugationInfoFontSize = (int)Math.Round(ConfigManager.DeconjugationInfoFontSize / fontScale);
            ConfigManager.DefinitionsFontSize = (int)Math.Round(ConfigManager.DefinitionsFontSize / fontScale);
            ConfigManager.DictTypeFontSize = (int)Math.Round(ConfigManager.DictTypeFontSize / fontScale);
            ConfigManager.FrequencyFontSize = (int)Math.Round(ConfigManager.FrequencyFontSize / fontScale);
            ConfigManager.PrimarySpellingFontSize = (int)Math.Round(ConfigManager.PrimarySpellingFontSize / fontScale);
            ConfigManager.ReadingsFontSize = (int)Math.Round(ConfigManager.ReadingsFontSize / fontScale);
        }
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        WindowsUtils.Dpi = e.NewDpi;
        WindowsUtils.ActiveScreen = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);
        WindowsUtils.DpiAwareWorkAreaWidth = WindowsUtils.ActiveScreen.Bounds.Width / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareWorkAreaHeight = WindowsUtils.ActiveScreen.Bounds.Height / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwareXOffset = ConfigManager.PopupXOffset / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = ConfigManager.PopupYOffset / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwareFixedPopupXPosition = ConfigManager.FixedPopupXPosition / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwareFixedPopupYPosition = ConfigManager.FixedPopupYPosition / e.NewDpi.DpiScaleY;
        WindowsUtils.DpiAwarePopupMaxWidth = ConfigManager.PopupMaxWidth / e.NewDpi.DpiScaleX;
        WindowsUtils.DpiAwarePopupMaxHeight = ConfigManager.PopupMaxHeight / e.NewDpi.DpiScaleY;
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ConfigManager.LookupOnSelectOnly)
        {
            double verticalOffset = MainTextBox!.VerticalOffset;
            MainTextBox.Select(0, 0);
            MainTextBox.ScrollToVerticalOffset(verticalOffset);
        }
    }

    private void Border_OnMouseEnter(object sender, MouseEventArgs e)
    {
        // For some reason, when DragMove() is used Mouse.GetPosition() returns Point(0, 0)
        if (e.GetPosition(this) == new Point(0, 0))
            return;

        var border = (Border)sender;

        switch (border.Name)
        {
            case "LeftBorder":
                Mouse.OverrideCursor = Cursors.SizeWE;
                break;
            case "RightBorder":
                Mouse.OverrideCursor = Cursors.SizeWE;
                break;
            case "TopBorder":
                Mouse.OverrideCursor = Cursors.SizeNS;
                break;
            case "TopRightBorder":
                Mouse.OverrideCursor = Cursors.SizeNESW;
                break;
            case "BottomBorder":
                Mouse.OverrideCursor = Cursors.SizeNS;
                break;
            case "BottomLeftBorder":
                Mouse.OverrideCursor = Cursors.SizeNESW;
                break;
            case "BottomRightBorder":
                Mouse.OverrideCursor = Cursors.SizeNWSE;
                break;
            case "TopLeftBorder":
                Mouse.OverrideCursor = Cursors.SizeNWSE;
                break;
        }
    }
    private void Border_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Released)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

    //private void Resize(object sender, DragDeltaEventArgs e)
    //{
    //Left += e.HorizontalChange;
    //Top += e.VerticalChange;
    //            WindowResizer.SetWindowPos(_windowResizer.windowHandle, IntPtr.Zero, Convert.ToInt32(Left + e.HorizontalChange), Convert.ToInt32(Top + e.VerticalChange), Convert.ToInt32(Width), Convert.ToInt32(Height),
    //WindowResizer.SetWindowPosFlags.SWP_SHOWWINDOW | WindowResizer.SetWindowPosFlags.SWP_NOREDRAW);
    //}

    private void ResizeWindow(object sender, MouseButtonEventArgs e)
    {
        _winApi?.ResizeWindow(sender as Border ?? new());

        LeftPositionBeforeResolutionChange = Left;
        TopPositionBeforeResolutionChange = Top;
        HeightBeforeResolutionChange = Height;
        WidthBeforeResolutionChange = Width;
    }

    //public bool IsMouseOnTitleBar(int lParam)
    //{
    //    int x = lParam << 16 >> 16;
    //    int y = lParam >> 16;
    //    Point cursorPoint = PointFromScreen(new Point(x, y));

    //    HitTestResult? hitTestResult = VisualTreeHelper.HitTest(this, cursorPoint);

    //    if (hitTestResult != null)
    //    {
    //        return hitTestResult.VisualHit == TitleBar;
    //    }

    //    return false;
    //}

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }

        LeftPositionBeforeResolutionChange = Left;
        TopPositionBeforeResolutionChange = Top;
    }

    //private void Window_Deactivated(object sender, EventArgs e)
    //{
    //    //if (!FirstPopupWindow.IsVisible)
    //    //    FocusEllipse.Fill = Brushes.Transparent;
    //}

    //private void Window_Activated(object sender, EventArgs e)
    //{
    //    //FocusEllipse.Fill = Brushes.Green;
    //    //FocusEllipse.Opacity = Background.Opacity;
    //}

    private void MainTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ManageDictionariesButton!.IsEnabled = Storage.DictsReady
                                      && !Storage.UpdatingJMdict
                                      && !Storage.UpdatingJMnedict
                                      && !Storage.UpdatingKanjidic;

        ManageFrequenciesButton!.IsEnabled = Storage.FreqsReady;

        AddNameButton!.IsEnabled = Storage.DictsReady;
        AddWordButton!.IsEnabled = Storage.DictsReady;
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        if (Background.Opacity == 0 || ConfigManager.InvisibleMode)
            return;

        if (!FirstPopupWindow.IsVisible
            && !ManageDictionariesWindow.IsItVisible()
            && !ManageFrequenciesWindow.IsItVisible()
            && !AddNameWindow.IsItVisible()
            && !AddWordWindow.IsItVisible()
            && !PreferencesWindow.IsItVisible()
            && !StatsWindow.IsItVisible()
            && !MainTextboxContextMenu.IsVisible
            && e.LeftButton == MouseButtonState.Released)
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

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        if (Background.Opacity == 0 || ConfigManager.InvisibleMode)
            return;

        if (ConfigManager.TextOnlyVisibleOnHover)
        {
            MainGrid.Opacity = 1;
        }

        if (ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Storage.StatsStopWatch.Stop();
        }

        else
        {
            Storage.StatsStopWatch.Start();
        }
    }
}
