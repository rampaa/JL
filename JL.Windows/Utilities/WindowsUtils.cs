using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Data;
using HandyControl.Tools;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.SpeechSynthesis;
using NAudio.Vorbis;
using NAudio.Wave;
using ColorPicker = HandyControl.Controls.ColorPicker;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;

namespace JL.Windows.Utilities;

internal static class WindowsUtils
{
    public static Typeface PopupFontTypeFace { get; set; } = new(ConfigManager.Instance.PopupFont.Source);
    private static DateTime s_lastAudioPlayTime;
    public static WaveOut? AudioPlayer { get; private set; }

    public static Screen ActiveScreen { get; set; } = Screen.FromHandle(MainWindow.Instance.WindowHandle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);
    public static double DpiAwareXOffset { get; set; } = ConfigManager.Instance.PopupXOffset * Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.Instance.PopupYOffset * Dpi.DpiScaleY;

    public static nint LastActiveWindowHandle { get; set; }

    public static ComboBoxItem[] FindJapaneseFonts()
    {
        XmlLanguage japaneseXmlLanguage = XmlLanguage.GetLanguage("ja-JP");
        XmlLanguage englishXmlLanguage = XmlLanguage.GetLanguage("en-US");

        List<ComboBoxItem> japaneseFonts = new(Fonts.SystemFontFamilies.Count);
        foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
        {
            ComboBoxItem comboBoxItem = new()
            {
                Content = fontFamily.Source,
                FontFamily = fontFamily
            };

            if (fontFamily.FamilyNames.ContainsKey(japaneseXmlLanguage))
            {
                japaneseFonts.Add(comboBoxItem);
            }

            else if (fontFamily.FamilyNames.Keys!.Count is 1
                     && fontFamily.FamilyNames.ContainsKey(englishXmlLanguage))
            {
                bool foundGlyph = false;

                // If the Cascadia Code font is installed calling GetTypefaces might throw UnauthorizedAccessException
                // See: https://github.com/microsoft/cascadia-code/issues/691
                try
                {
                    foreach (Typeface typeFace in fontFamily.GetTypefaces())
                    {
                        if (typeFace.TryGetGlyphTypeface(out GlyphTypeface glyphTypeFace))
                        {
                            // 0x30F8 -> ヸ
                            // 0x67A0 -> 枠, kokuji
                            if (glyphTypeFace.CharacterToGlyphMap.ContainsKey(0x30F8))
                            {
                                japaneseFonts.Add(comboBoxItem);
                                foundGlyph = true;
                                break;
                            }
                        }
                    }

                    if (!foundGlyph)
                    {
                        comboBoxItem.Foreground = Brushes.LightSlateGray;
                        japaneseFonts.Add(comboBoxItem);
                    }
                }

                catch (UnauthorizedAccessException ex)
                {
                    Utils.Logger.Error(ex, "GetTypefaces failed for {FontFamily}", fontFamily);
                }
            }
            else
            {
                comboBoxItem.Foreground = Brushes.LightSlateGray;
                japaneseFonts.Add(comboBoxItem);
            }
        }

        return japaneseFonts
            .OrderBy(static f => f.Foreground == Brushes.LightSlateGray)
            .ThenBy(static font => (string)font.Content, StringComparer.InvariantCulture)
            .ToArray();
    }

    public static ComboBoxItem[] CloneComboBoxItems(ComboBoxItem[] comboBoxItems)
    {
        ComboBoxItem[] clone = new ComboBoxItem[comboBoxItems.Length];
        for (int i = 0; i < comboBoxItems.Length; i++)
        {
            ComboBoxItem comboBoxItem = comboBoxItems[i];

            clone[i] = new ComboBoxItem
            {
                Content = comboBoxItem.Content,
                FontFamily = comboBoxItem.FontFamily
            };

            if (comboBoxItem.Foreground == Brushes.LightSlateGray)
            {
                clone[i].Foreground = Brushes.LightSlateGray;
            }
        }

        return clone;
    }

    public static void ShowAddNameWindow(Window owner, string? selectedText, string reading = "")
    {
        AddNameWindow addNameWindowInstance = AddNameWindow.Instance;
        addNameWindowInstance.SpellingTextBox.Text = selectedText ?? "";
        addNameWindowInstance.ReadingTextBox.Text = reading;
        addNameWindowInstance.Owner = owner;
        addNameWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = addNameWindowInstance.ShowDialog();
    }

    public static void ShowAddWordWindow(Window owner, string? selectedText)
    {
        AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
        addWordWindowInstance.SpellingsTextBox.Text = selectedText ?? "";
        addWordWindowInstance.Owner = owner;
        addWordWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = addWordWindowInstance.ShowDialog();
    }

    public static void ShowPreferencesWindow()
    {
        PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
        ConfigManager configManager = ConfigManager.Instance;
        configManager.LoadPreferenceWindow(preferencesWindow);
        MainWindow mainWindow = MainWindow.Instance;
        preferencesWindow.Owner = mainWindow;
        preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        _ = preferencesWindow.ShowDialog();
    }

    public static async Task ShowManageDictionariesWindow()
    {
        if (!File.Exists(Path.Join(Utils.ConfigPath, "dicts.json")))
        {
            await DictUtils.CreateDefaultDictsConfig().ConfigureAwait(true);
        }

        string customWordsPath = Path.Join(Utils.ResourcesPath, "custom_words.txt");
        if (!File.Exists(customWordsPath))
        {
            await File.Create(customWordsPath).DisposeAsync().ConfigureAwait(true);
        }

        string customNamesPath = Path.Join(Utils.ResourcesPath, "custom_names.txt");
        if (!File.Exists(customNamesPath))
        {
            await File.Create(customNamesPath).DisposeAsync().ConfigureAwait(true);
        }

        ManageDictionariesWindow manageDictionariesWindow = ManageDictionariesWindow.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        manageDictionariesWindow.Owner = mainWindow;
        manageDictionariesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        _ = manageDictionariesWindow.ShowDialog();
    }

    public static async Task ShowManageFrequenciesWindow()
    {
        if (!File.Exists(Path.Join(Utils.ConfigPath, "freqs.json")))
        {
            await FreqUtils.CreateDefaultFreqsConfig().ConfigureAwait(true);
        }

        ManageFrequenciesWindow manageFrequenciesWindow = ManageFrequenciesWindow.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        manageFrequenciesWindow.Owner = mainWindow;
        manageFrequenciesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        _ = manageFrequenciesWindow.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        StatsUtils.IncrementStat(StatType.Time, StatsUtils.TimeStatStopWatch.ElapsedTicks);
        StatsUtils.TimeStatStopWatch.Reset();
        StatsUtils.StopIdleItemTimer();

        StatsWindow statsWindow = StatsWindow.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        statsWindow.Owner = mainWindow;
        statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        _ = statsWindow.ShowDialog();
    }

    public static async Task ShowManageAudioSourcesWindow()
    {
        if (!File.Exists(Path.Join(Utils.ConfigPath, "AudioSources.json")))
        {
            await AudioUtils.CreateDefaultAudioSourceConfig().ConfigureAwait(true);
        }

        ManageAudioSourcesWindow manageAudioSourcesWindow = ManageAudioSourcesWindow.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        manageAudioSourcesWindow.Owner = mainWindow;
        manageAudioSourcesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        _ = manageAudioSourcesWindow.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        string browserPath = "";
        ConfigManager configManager = ConfigManager.Instance;
        if (!string.IsNullOrWhiteSpace(configManager.BrowserPath))
        {
            browserPath = $"\"{configManager.BrowserPath}\"";
        }

        if (selectedText?.Length > 0)
        {
            string urlToBeSearched = Uri.IsWellFormedUriString(selectedText, UriKind.Absolute)
                ? selectedText
                : configManager.SearchUrl.Replace("{SearchTerm}", HttpUtility.UrlEncode(selectedText), StringComparison.OrdinalIgnoreCase);

            _ = Process.Start(new ProcessStartInfo("cmd",
                $"/c start \"\" {browserPath} \"{urlToBeSearched}\"")
            {
                CreateNoWindow = true
            });
        }
    }

    public static async Task UpdateJL(Uri latestReleaseUrl)
    {
        using HttpResponseMessage downloadResponse = await Networking.Client.GetAsync(latestReleaseUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (downloadResponse.IsSuccessStatusCode)
        {
            string tmpDirectory = Path.Join(Utils.ApplicationPath, "tmp");

            if (Directory.Exists(tmpDirectory))
            {
                Directory.Delete(tmpDirectory, true);
            }

            _ = Directory.CreateDirectory(tmpDirectory);

            Stream downloadResponseStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using (downloadResponseStream.ConfigureAwait(false))
            {
                using ZipArchive archive = new(downloadResponseStream);
                archive.ExtractToDirectory(tmpDirectory);
            }

            await Application.Current.Dispatcher.Invoke(static () => MainWindow.Instance.HandleAppClosing()).ConfigureAwait(false);

            _ = Process.Start(
                new ProcessStartInfo("cmd",
                    string.Create(CultureInfo.InvariantCulture, $"/c start \"JL Updater\" \"{Path.Join(Utils.ApplicationPath, "update-helper.cmd")}\" {Environment.ProcessId}"))
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });
        }

        else
        {
            Utils.Logger.Error("Couldn't update JL. {StatusCode} {ReasonPhrase}", downloadResponse.StatusCode, downloadResponse.ReasonPhrase);
            Alert(AlertLevel.Error, "Couldn't update JL");
        }
    }

    public static async Task InitializeMainWindow()
    {
        Utils.Frontend = new WindowsFrontend();

        await Utils.CoreInitialize().ConfigureAwait(true);

        if (CoreConfigManager.Instance.CheckForJLUpdatesOnStartUp)
        {
            PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
            preferencesWindow.CheckForJLUpdatesButton.IsEnabled = false;
            await Networking.CheckForJLUpdates(true).ConfigureAwait(true);
            preferencesWindow.CheckForJLUpdatesButton.IsEnabled = true;
        }
    }

    public static void PlayAudio(byte[] audio, string audioFormat)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                AudioPlayer?.Dispose();
                AudioPlayer = new WaveOut();

                MemoryStream audioStream = new(audio);
                await using (audioStream.ConfigureAwait(false))
                {
                    IWaveProvider waveProvider = audioFormat is "ogg" or "oga"
                        ? new VorbisWaveReader(audioStream)
                        : new StreamMediaFoundationReader(audioStream);

                    AudioPlayer.Init(waveProvider);
                    AudioPlayer.Play();

                    while (AudioPlayer.PlaybackState is PlaybackState.Playing)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                }
            }

            catch (Exception ex)
            {
                Utils.Logger.Error(ex, "Error playing audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio), audioFormat);
                Alert(AlertLevel.Error, "Error playing audio");
            }
        });
    }

    public static async Task Motivate()
    {
        DateTime currentTime = DateTime.Now;
        if (AudioPlayer?.PlaybackState is PlaybackState.Playing && (currentTime - s_lastAudioPlayTime).TotalMilliseconds < 300)
        {
            s_lastAudioPlayTime = currentTime;
            return;
        }

        s_lastAudioPlayTime = currentTime;

        await SpeechSynthesisUtils.StopTextToSpeech().ConfigureAwait(false);

        try
        {
            string[] filePaths = Directory.GetFiles(Path.Join(Utils.ResourcesPath, "Motivation"));
            int numFiles = filePaths.Length;

            if (numFiles is 0)
            {
                Utils.Logger.Warning("Motivation folder is empty!");
                Alert(AlertLevel.Warning, "Motivation folder is empty!");
                return;
            }

#pragma warning disable CA5394 // Do not use insecure randomness
            string randomFilePath = filePaths[Random.Shared.Next(numFiles)];
#pragma warning restore CA5394 // Do not use insecure randomness

            byte[] audioData = await File.ReadAllBytesAsync(randomFilePath).ConfigureAwait(false);
            PlayAudio(audioData, "mp3");
            StatsUtils.IncrementStat(StatType.Imoutos);
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error motivating");
            Alert(AlertLevel.Error, "Error motivating");
        }
    }

    public static Brush BrushFromHex(string hexColorString)
    {
        return (Brush)new BrushConverter().ConvertFromInvariantString(hexColorString)!;
    }

    public static Brush FrozenBrushFromHex(string hexColorString)
    {
        Brush brush = (Brush)new BrushConverter().ConvertFromInvariantString(hexColorString)!;
        brush.Freeze();
        return brush;
    }

    public static Color ColorFromHex(string hexColorString)
    {
        return (Color)ColorConverter.ConvertFromString(hexColorString);
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            List<AlertWindow> alertWindowList = Application.Current.Windows.OfType<AlertWindow>().ToList();

            AlertWindow alertWindow = new();
            alertWindow.Show();

            double offset = 30 * Dpi.DpiScaleX;
            double offsetBetweenAlerts = 2 * Dpi.DpiScaleY;
            double x = ActiveScreen.WorkingArea.Right - offset - (alertWindow.Width * Dpi.DpiScaleX);
            double y = ActiveScreen.WorkingArea.Top + offset + alertWindowList.Sum(aw => (aw.ActualHeight * Dpi.DpiScaleX) + offsetBetweenAlerts);
            WinApi.MoveWindowToPosition(alertWindow.WindowHandle, x, y);

            alertWindow.SetAlert(alertLevel, message);

            await Task.Delay(4004).ConfigureAwait(true);
            alertWindow.Close();
        });
    }

    public static Size MeasureTextSize(string text, double fontSize)
    {
        FormattedText formattedText = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            PopupFontTypeFace,
            fontSize,
            Brushes.Transparent,
            Dpi.PixelsPerDip);

        return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }

    public static void ShowColorPicker(Button button)
    {
        ColorPicker picker = SingleOpenHelper.CreateControl<ColorPicker>();
        HandyControl.Controls.PopupWindow window = new()
        {
            PopupElement = picker,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        picker.SelectedBrush = (SolidColorBrush)button.Tag;
        picker.Tag = (window, button);
        picker.Canceled += ColorPicker_Cancelled;
        picker.Confirmed += ColorPicker_Confirmed;

        window.ShowDialog(picker, false);
    }

    private static void ColorPicker_Cancelled(object? sender, EventArgs e)
    {
        if (sender is not ColorPicker colorPicker)
        {
            return;
        }

        (HandyControl.Controls.PopupWindow window, _) = ((HandyControl.Controls.PopupWindow, Button))colorPicker.Tag;

        window.Close();
        colorPicker.Canceled -= ColorPicker_Cancelled;
        colorPicker.Confirmed -= ColorPicker_Confirmed;
        colorPicker.Dispose();
    }

    private static void ColorPicker_Confirmed(object? sender, FunctionEventArgs<Color> e)
    {
        if (sender is not ColorPicker colorPicker)
        {
            return;
        }

        (HandyControl.Controls.PopupWindow window, Button button) = ((HandyControl.Controls.PopupWindow, Button))colorPicker.Tag;
        SetButtonColor(button, colorPicker.SelectedBrush);
        window.Close();
        colorPicker.Canceled -= ColorPicker_Cancelled;
        colorPicker.Confirmed -= ColorPicker_Confirmed;
        colorPicker.Dispose();
    }

    public static void SetButtonColor(Button button, Brush selectedBrush)
    {
        selectedBrush.Freeze();
        button.Tag = selectedBrush;

        Color selectedBrushColor = ((SolidColorBrush)selectedBrush).Color;

        button.Background = CreateFrozenOpaqueBrush(selectedBrushColor);
    }

    public static void SetButtonColor(Button button, Color selectedColor)
    {
        button.Tag = new SolidColorBrush(selectedColor);
        button.Background = CreateFrozenOpaqueBrush(selectedColor);
    }

    private static Brush CreateFrozenOpaqueBrush(Color color)
    {
        Brush opaqueBrush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        opaqueBrush.Freeze();
        return opaqueBrush;
    }

    public static void Unselect(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        double verticalOffset = textBox.VerticalOffset;
        textBox.Select(0, 0);
        textBox.ScrollToVerticalOffset(verticalOffset);
    }

    public static void UpdateMainWindowVisibility()
    {
        ConfigManager configManager = ConfigManager.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        bool mainWindowIsNotMinimized = mainWindow.WindowState is not WindowState.Minimized;
        if (mainWindowIsNotMinimized)
        {
            if (!mainWindow.FirstPopupWindow.IsVisible)
            {
                if (!mainWindow.IsMouseOver)
                {
                    if (configManager.TextOnlyVisibleOnHover)
                    {
                        mainWindow.MainGrid.Opacity = 0;
                    }

                    if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
                    {
                        mainWindow.Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
                    }

                    nint lastActiveWindowHandle = LastActiveWindowHandle;
                    if (configManager.RestoreFocusToPreviouslyActiveWindow
                        && (configManager.PopupFocusOnLookup || configManager.MainWindowFocusOnHover)
                        && lastActiveWindowHandle is not 0
                        && lastActiveWindowHandle != mainWindow.WindowHandle)
                    {
                        WinApi.GiveFocusToWindow(lastActiveWindowHandle);
                    }
                }
            }

            CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
            if (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket)
            {
                StatsUtils.StartTimeStatStopWatch();
            }

            if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
            {
                WinApi.RegisterAllGlobalHotKeys(mainWindow.WindowHandle);
            }
        }
        else
        {
            nint lastActiveWindowHandle = LastActiveWindowHandle;
            if (configManager.RestoreFocusToPreviouslyActiveWindow
                && (configManager.PopupFocusOnLookup || configManager.MainWindowFocusOnHover)
                && lastActiveWindowHandle is not 0
                && lastActiveWindowHandle != mainWindow.WindowHandle)
            {
                WinApi.GiveFocusToWindow(lastActiveWindowHandle);
            }

            if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized)
            {
                CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
                if (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket)
                {
                    StatsUtils.StartTimeStatStopWatch();
                }
            }
        }
    }

    public static void ChangeTheme(SkinType skin)
    {
        ResourceDictionary resources = Application.Current.Resources;

        //resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ResourceDictionary.xaml", UriKind.Relative) });
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(string.Create(CultureInfo.InvariantCulture, $"pack://application:,,,/HandyControl;component/Themes/Skin{skin}.xaml"))
        });
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
        });
    }

    public static Task<byte[]?> GetImageFromClipboardAsByteArray()
    {
        return Application.Current.Dispatcher.Invoke(static async () =>
        {
            while (Clipboard.ContainsImage())
            {
                try
                {
                    BitmapSource? image = Clipboard.GetImage();
                    if (image is null)
                    {
                        return null;
                    }

                    PngBitmapEncoder pngBitmapEncoder = new();
                    pngBitmapEncoder.Frames.Add(BitmapFrame.Create(image));

                    using MemoryStream stream = new();
                    pngBitmapEncoder.Save(stream);
                    return stream.ToArray();
                }
                catch (Exception ex)
                {
                    Utils.Logger.Warning(ex, "GetImageFromClipboard failed");
                    await Task.Delay(5).ConfigureAwait(true);
                }
            }

            return null;
        });
    }

    public static bool ShowYesNoDialog(string text, string caption)
    {
        return HandyControl.Controls.MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) is MessageBoxResult.Yes;
    }

    public static void UpdatePositionForSelectionWindows(Window window, nint windowHandle, Point cursorPosition)
    {
        double mouseX = cursorPosition.X;
        double mouseY = cursorPosition.Y;

        DpiScale dpi = Dpi;
        double currentWidth = window.ActualWidth * dpi.DpiScaleX;
        double currentHeight = window.ActualHeight * dpi.DpiScaleY;

        double dpiAwareXOffSet = 5 * dpi.DpiScaleX;
        double dpiAwareYOffset = 15 * dpi.DpiScaleY;

        Rectangle bounds = ActiveScreen.Bounds;
        bool needsFlipX = mouseX + currentWidth > bounds.Right;
        bool needsFlipY = mouseY + currentHeight > bounds.Bottom;

        double newLeft;
        double newTop;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - currentWidth - dpiAwareXOffSet;
            if (newLeft < bounds.X)
            {
                newLeft = bounds.X;
            }
        }
        else
        {
            // no flip
            newLeft = mouseX - dpiAwareXOffSet;
        }

        if (needsFlipY)
        {
            // flip Upwards while preventing -OOB
            newTop = mouseY - (currentHeight + dpiAwareYOffset);
            if (newTop < bounds.Y)
            {
                newTop = bounds.Y;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + dpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + currentWidth > bounds.Right)
        {
            newLeft = bounds.Right - currentWidth;
        }

        if (newTop + currentHeight > bounds.Bottom)
        {
            newTop = bounds.Bottom - currentHeight;
        }

        WinApi.MoveWindowToPosition(windowHandle, newLeft, newTop);
    }

    public static void SelectPreviousListViewItem(ListView listView)
    {
        int nextItemIndex = listView.SelectedIndex - 1 >= 0
            ? listView.SelectedIndex - 1
            : listView.Items.Count - 1;

        listView.SelectedIndex = nextItemIndex;

        listView.ScrollIntoView(listView.Items.GetItemAt(nextItemIndex));
    }

    public static void SelectNextListViewItem(ListView listView)
    {
        int nextItemIndex = listView.SelectedIndex + 1 < listView.Items.Count
            ? listView.SelectedIndex + 1
            : 0;

        listView.SelectedIndex = nextItemIndex;

        listView.ScrollIntoView(listView.Items.GetItemAt(nextItemIndex));
    }
}
