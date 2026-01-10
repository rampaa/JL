using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Data;
using HandyControl.Tools;
using JL.Core;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.External;
using JL.Core.Freqs;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Config;
using JL.Windows.External.Magpie;
using JL.Windows.GUI;
using JL.Windows.GUI.Audio;
using JL.Windows.GUI.Dictionary;
using JL.Windows.GUI.Frequency;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis;
using NAudio.Vorbis;
using NAudio.Wave;
using ColorPicker = HandyControl.Controls.ColorPicker;
using Rectangle = System.Drawing.Rectangle;
using Screen = System.Windows.Forms.Screen;

namespace JL.Windows.Utilities;

internal static class WindowsUtils
{
    public static readonly Version JLVersion =
        Version.Parse(Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion.Split('-', '+')[0]);

    private static readonly SemaphoreSlim s_dialogSemaphore = new(1, 1);
    public static readonly SemaphoreSlim AudioPlayerSemaphoreSlim = new(1, 1);

    private static long s_lastAudioPlayTimestamp;
    private static WaveOutEvent? s_audioPlayer;

    public static WaveOutEvent? AudioPlayer => Volatile.Read(ref s_audioPlayer);

    public static Typeface PopupFontTypeFace { get; set; } = new(ConfigManager.Instance.PopupFont.Source);
    public static GlyphTypeface? PopupGlyphTypeface { get; set; } = PopupFontTypeFace.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface) ? glyphTypeface : null;

    public static Screen ActiveScreen { get; set; } = Screen.FromHandle(MainWindow.Instance.WindowHandle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);

    public static double DpiAwareXOffset { get; set; } = ConfigManager.Instance.PopupXOffset * Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.Instance.PopupYOffset * Dpi.DpiScaleY;

    public static double DpiAwareXOffsetForVerticalText { get; set; } = ConfigManager.Instance.PopupXOffsetForVerticalText * Dpi.DpiScaleX;
    public static double DpiAwareYOffsetForVerticalText { get; set; } = ConfigManager.Instance.PopupYOffsetForVerticalText * Dpi.DpiScaleY;

    public static nint LastActiveWindowHandle { get; set; }

    public static bool LastWebSocketTextWasVertical { get; set; } // = false;

    public static ComboBoxItem[] FindJapaneseFonts()
    {
        XmlLanguage japaneseXmlLanguage = XmlLanguage.GetLanguage("ja-JP");
        XmlLanguage englishXmlLanguage = XmlLanguage.GetLanguage("en-US");

        List<ComboBoxItem> japaneseFonts = new(Fonts.SystemFontFamilies.Count);
        foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
        {
            if (fontFamily.FamilyNames.Keys is null)
            {
                continue;
            }

            ComboBoxItem comboBoxItem = new()
            {
                Content = fontFamily.Source,
                FontFamily = fontFamily
            };

            if (fontFamily.FamilyNames.ContainsKey(japaneseXmlLanguage))
            {
                japaneseFonts.Add(comboBoxItem);
            }

            else if (fontFamily.FamilyNames.Keys.Count is 1
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
                    LoggerManager.Logger.Error(ex, "GetTypefaces failed for {FontFamily}", fontFamily);
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
        preferencesWindow.Owner = MainWindow.Instance;
        preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = preferencesWindow.ShowDialog();
    }

    public static async Task ShowManageDictionariesWindow()
    {
        await DictUtils.CreateDefaultDictsConfig().ConfigureAwait(true);

        PathUtils.CreateFileIfNotExists(DictUtils.CustomWordDictPath);
        PathUtils.CreateFileIfNotExists(DictUtils.CustomNameDictPath);

        ManageDictionariesWindow manageDictionariesWindow = ManageDictionariesWindow.Instance;
        manageDictionariesWindow.Owner = MainWindow.Instance;
        manageDictionariesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = manageDictionariesWindow.ShowDialog();
    }

    public static async Task ShowManageFrequenciesWindow()
    {
        await FreqUtils.CreateDefaultFreqsConfig().ConfigureAwait(true);

        ManageFrequenciesWindow manageFrequenciesWindow = ManageFrequenciesWindow.Instance;
        manageFrequenciesWindow.Owner = MainWindow.Instance;
        manageFrequenciesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = manageFrequenciesWindow.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        StatsUtils.IncrementStat(StatType.Time, StatsUtils.TimeStatStopWatch.ElapsedTicks);
        StatsUtils.TimeStatStopWatch.Reset();
        StatsUtils.StopIdleItemTimer();

        StatsWindow statsWindow = StatsWindow.Instance;
        statsWindow.Owner = MainWindow.Instance;
        statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = statsWindow.ShowDialog();
    }

    public static async Task ShowManageAudioSourcesWindow()
    {
        if (!File.Exists(Path.Join(AppInfo.ConfigPath, "AudioSources.json")))
        {
            await AudioUtils.CreateDefaultAudioSourceConfig().ConfigureAwait(true);
        }

        ManageAudioSourcesWindow manageAudioSourcesWindow = ManageAudioSourcesWindow.Instance;
        manageAudioSourcesWindow.Owner = MainWindow.Instance;
        manageAudioSourcesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        StatsUtils.StopTimeStatStopWatch();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = manageAudioSourcesWindow.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return;
        }

        ConfigManager configManager = ConfigManager.Instance;
        string urlToBeSearched = Uri.IsWellFormedUriString(selectedText, UriKind.Absolute)
            ? selectedText
            : configManager.SearchUrl.Replace("{SearchTerm}", HttpUtility.UrlEncode(selectedText), StringComparison.OrdinalIgnoreCase);

        using Process? process = string.IsNullOrWhiteSpace(configManager.BrowserPath) || !File.Exists(configManager.BrowserPath)
            ? Process.Start(new ProcessStartInfo(urlToBeSearched)
            {
                UseShellExecute = true
            })
            : Process.Start(new ProcessStartInfo
            {
                FileName = configManager.BrowserPath,
                Arguments = urlToBeSearched,
                UseShellExecute = false,
                CreateNoWindow = true
            });

        if (configManager.AutoPauseOrResumeMpvOnHoverChange)
        {
            MpvUtils.PausePlayback().SafeFireAndForget("Unexpected error while pausing playback");
        }
    }

    public static async Task UpdateJL(Uri latestReleaseUrl)
    {
        using HttpResponseMessage downloadResponse = await NetworkUtils.Client.GetAsync(latestReleaseUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (downloadResponse.IsSuccessStatusCode)
        {
            string tmpDirectory = Path.Join(AppInfo.ApplicationPath, "tmp");

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

            Application? application = Application.Current;
            if (application is not null)
            {
                await application.Dispatcher.Invoke(static () => MainWindow.Instance.HandleAppClosing()).ConfigureAwait(false);
                using Process? process = Process.Start(new ProcessStartInfo
                {
                    WorkingDirectory = AppInfo.ApplicationPath,
                    FileName = "update-helper.cmd",
                    Arguments = Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
                    UseShellExecute = true
                });
            }
        }

        else
        {
            LoggerManager.Logger.Error("Couldn't update JL. {StatusCode} {ReasonPhrase}", downloadResponse.StatusCode, downloadResponse.ReasonPhrase);
            Alert(AlertLevel.Error, "Couldn't update JL");
        }
    }

    public static async Task InitializeMainWindow()
    {
        await Task.Run(CoreInitializer.CoreInitialize).ConfigureAwait(false);

        if (CoreConfigManager.Instance.CheckForJLUpdatesOnStartUp)
        {
            Application? application = Application.Current;
            if (application is not null)
            {
                application.Dispatcher.Invoke(static () =>
                {
                    PreferencesWindow.Instance.CheckForJLUpdatesButton.IsEnabled = false;
                });

                await Task.Run(static () => NetworkUtils.CheckForJLUpdates(true)).ConfigureAwait(false);

                application.Dispatcher.Invoke(static () =>
                {
                    PreferencesWindow.Instance.CheckForJLUpdatesButton.IsEnabled = true;
                    if (!PreferencesWindow.Instance.IsVisible)
                    {
                        PreferencesWindow.Instance.Close();
                    }
                });
            }
        }
    }

    public static async Task PlayAudio(byte[] audio, string audioFormat)
    {
        await AudioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);

        try
        {
            WaveOutEvent? oldPlayer = Interlocked.Exchange(ref s_audioPlayer, null);
            oldPlayer?.Stop();

            MemoryStream memoryStream = new(audio);

#pragma warning disable CA2000 // Dispose objects before losing scope
            WaveStream waveProvider = audioFormat is "ogg" or "oga"
                ? new VorbisWaveReader(memoryStream)
                : new StreamMediaFoundationReader(memoryStream);
#pragma warning restore CA2000 // Dispose objects before losing scope

            WaveOutEvent audioPlayer = new();

            try
            {
                audioPlayer.Init(waveProvider);
                _ = Interlocked.Exchange(ref s_audioPlayer, audioPlayer);
                audioPlayer.Play();
            }
            catch (Exception ex)
            {
                audioPlayer.Dispose();
                await waveProvider.DisposeAsync().ConfigureAwait(false);
                await memoryStream.DisposeAsync().ConfigureAwait(false);
                _ = Interlocked.Exchange(ref s_audioPlayer, null);

                LoggerManager.Logger.Error(ex, "Error playing audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
                Alert(AlertLevel.Error, "Error playing audio");
            }

            audioPlayer.PlaybackStopped += async (_, _) =>
            {
                await AudioPlayerSemaphoreSlim.WaitAsync().ConfigureAwait(false);
                try
                {
                    audioPlayer.Dispose();
                    await waveProvider.DisposeAsync().ConfigureAwait(false);
                    await memoryStream.DisposeAsync().ConfigureAwait(false);

                    if (AudioPlayer == audioPlayer)
                    {
                        _ = Interlocked.Exchange(ref s_audioPlayer, null);
                    }
                }
                catch (Exception ex)
                {
                    LoggerManager.Logger.Error(ex, "Error while disposing audio player");
                }
                finally
                {
                    _ = AudioPlayerSemaphoreSlim.Release();
                }
            };
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error playing audio: {Audio}, audio format: {AudioFormat}", JsonSerializer.Serialize(audio, JsonOptions.DefaultJso), audioFormat);
            Alert(AlertLevel.Error, "Error playing audio");
        }
        finally
        {
            _ = AudioPlayerSemaphoreSlim.Release();
        }
    }

    public static async Task Motivate()
    {
        if (AudioPlayer?.PlaybackState is PlaybackState.Playing && Stopwatch.GetElapsedTime(s_lastAudioPlayTimestamp).TotalMilliseconds < 300)
        {
            s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();
            return;
        }

        s_lastAudioPlayTimestamp = Stopwatch.GetTimestamp();

        try
        {
            string[] filePaths = Directory.GetFiles(Path.Join(AppInfo.ResourcesPath, "Motivation"));
            if (filePaths.Length is 0)
            {
                LoggerManager.Logger.Warning("Motivation folder is empty!");
                Alert(AlertLevel.Warning, "Motivation folder is empty!");
                return;
            }

#pragma warning disable CA5394 // Do not use insecure randomness
            string randomFilePath = filePaths[Random.Shared.Next(filePaths.Length)];
#pragma warning restore CA5394 // Do not use insecure randomness

            byte[] audioData = await File.ReadAllBytesAsync(randomFilePath).ConfigureAwait(false);

            await Task.Run(async () =>
            {
                SpeechSynthesisUtils.StopTextToSpeech();
                await PlayAudio(audioData, "mp3").ConfigureAwait(false);
            }).ConfigureAwait(false);

            StatsUtils.IncrementStat(StatType.Imoutos);
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error motivating");
            Alert(AlertLevel.Error, "Error motivating");
        }
    }

    public static SolidColorBrush BrushFromHex(ReadOnlySpan<char> hex)
    {
        return new SolidColorBrush(ColorFromHex(hex));
    }

    public static SolidColorBrush FrozenBrushFromHex(ReadOnlySpan<char> hex, double brushOpacity)
    {
        SolidColorBrush brush = new(ColorFromHex(hex))
        {
            Opacity = brushOpacity
        };

        brush.Freeze();
        return brush;
    }

    public static SolidColorBrush FrozenBrushFromHex(ReadOnlySpan<char> hex)
    {
        SolidColorBrush brush = new(ColorFromHex(hex));
        brush.Freeze();
        return brush;
    }

    public static Color ColorFromHex(ReadOnlySpan<char> hex)
    {
        Debug.Assert(hex.Length is 9 && hex[0] is '#');

        char lastDigit = hex[8];
        byte b = HexToByte(hex[7], lastDigit);
        byte g = HexToByte(hex[5], hex[6]);
        byte r = HexToByte(hex[3], hex[4]);
        byte a = HexToByte(hex[1], hex[2]);

        return Color.FromArgb(a, r, g, b);
    }

    private static byte HexToByte(char highNibble, char lowNibble)
    {
        return (byte)((HexDigitToInt(highNibble) * 16) + HexDigitToInt(lowNibble));
    }

    private static int HexDigitToInt(char c)
    {
        return c is >= '0' and <= '9'
            ? c - '0'
            : c is >= 'A' and <= 'F'
                ? c - 'A' + 10
                : c is >= 'a' and <= 'f'
                    ? c - 'a' + 10
                    : throw new FormatException($"Invalid hex digit: {c}");
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        Application? application = Application.Current;
        if (application is null)
        {
            return;
        }

        _ = application.Dispatcher.InvokeAsync(async () =>
        {
            List<AlertWindow> alertWindowList = application.Windows.OfType<AlertWindow>().ToList();

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

    private static double MeasureTextWidth(double fontSize, string text)
    {
        FormattedText formattedText = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            PopupFontTypeFace,
            fontSize,
            Brushes.Transparent,
            Dpi.PixelsPerDip);

        return formattedText.WidthIncludingTrailingWhitespace;
    }

    private static double MeasureTextHeight(double fontSize, string text)
    {
        FormattedText formattedText = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            PopupFontTypeFace,
            fontSize,
            Brushes.Transparent,
            Dpi.PixelsPerDip);

        return formattedText.Height;
    }

    public static double MeasureTextWidthWithGlyph(double fontSize, char character)
    {
        return PopupGlyphTypeface is not null && PopupGlyphTypeface.CharacterToGlyphMap.TryGetValue(character, out ushort glyphKey)
            ? PopupGlyphTypeface.AdvanceWidths[glyphKey] * fontSize
            : MeasureTextWidth(fontSize, character.ToString());
    }

    public static double MeasureTextWidthWithGlyph(double fontSize, ReadOnlySpan<char> text)
    {
        if (PopupGlyphTypeface is null)
        {
            return MeasureTextWidth(fontSize, text.ToString());
        }

        double totalWidth = 0;
        foreach (char c in text)
        {
            if (PopupGlyphTypeface.CharacterToGlyphMap.TryGetValue(c, out ushort glyphKey))
            {
                totalWidth += PopupGlyphTypeface.AdvanceWidths[glyphKey] * fontSize;
            }
            else
            {
                return MeasureTextWidth(fontSize, text.ToString());
            }
        }

        return totalWidth;
    }

    public static double MeasureMaxHeightWithGlyph(double fontSize)
    {
        return (PopupGlyphTypeface?.Height * fontSize) ?? MeasureTextHeight(fontSize, "ヲ");
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

    private static void RestoreFocusToPreviouslyActiveWindow()
    {
        ConfigManager configManager = ConfigManager.Instance;
        nint lastActiveWindowHandle = LastActiveWindowHandle;
        if (configManager is { Focusable: true, RestoreFocusToPreviouslyActiveWindow: true }
            && (configManager.PopupFocusOnLookup || configManager.MainWindowFocusOnHover)
            && lastActiveWindowHandle is not 0
            && lastActiveWindowHandle != MainWindow.Instance.WindowHandle)
        {
            WinApi.GiveFocusToWindow(lastActiveWindowHandle);
        }
    }

    public static void UpdateMainWindowVisibility()
    {
        ConfigManager configManager = ConfigManager.Instance;
        bool mainWindowIsNotMinimized = MainWindow.Instance.WindowState is not WindowState.Minimized;
        if (mainWindowIsNotMinimized)
        {
            MainWindow mainWindow = MainWindow.Instance;
            if (mainWindow.FirstPopupWindow.Opacity is 0)
            {
                if (!mainWindow.IsMouseOver)
                {
                    if (configManager.TextOnlyVisibleOnHover)
                    {
                        mainWindow.MainGrid.Opacity = 0d;
                    }

                    if (configManager.ChangeMainWindowBackgroundOpacityOnUnhover)
                    {
                        mainWindow.Background.Opacity = configManager.MainWindowBackgroundOpacityOnUnhover / 100;
                    }

                    RestoreFocusToPreviouslyActiveWindow();

                    if (configManager.AutoPauseOrResumeMpvOnHoverChange)
                    {
                        MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
                    }
                }
            }

            CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
            if (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket || coreConfigManager.CaptureTextFromTsukikageWebsocket)
            {
                StatsUtils.StartTimeStatStopWatch();
            }

            if (configManager is { GlobalHotKeys: true, DisableHotkeys: false })
            {
                WinApi.RegisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
            }
        }
        else
        {
            RestoreFocusToPreviouslyActiveWindow();

            if (configManager.AutoPauseOrResumeMpvOnHoverChange)
            {
                MpvUtils.ResumePlayback().SafeFireAndForget("Unexpected error while resuming playback");
            }

            if (!configManager.StopIncreasingTimeAndCharStatsWhenMinimized)
            {
                CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
                if (coreConfigManager.CaptureTextFromClipboard || coreConfigManager.CaptureTextFromWebSocket || coreConfigManager.CaptureTextFromTsukikageWebsocket)
                {
                    StatsUtils.StartTimeStatStopWatch();
                }
            }
        }
    }

    public static void ChangeTheme(SkinType skin)
    {
        ResourceDictionary? resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        //resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("GUI/Styles/ResourceDictionary.xaml", UriKind.Relative) });
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
        Application? application = Application.Current;
        return application is null
            ? Task.FromResult<byte[]?>(null)
            : application.Dispatcher.Invoke(static async () =>
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
                    LoggerManager.Logger.Warning(ex, "GetImageFromClipboard failed");
                    await Task.Delay(5).ConfigureAwait(true);
                }
            }

            return null;
        });
    }

    public static bool ShowYesNoDialog(string text, string caption, Window owner)
    {
        return HandyControl.Controls.MessageBox.Show(owner, text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) is MessageBoxResult.Yes;
    }

    public static async Task<bool> ShowYesNoDialogAsync(string text, string caption, Window owner)
    {
        await s_dialogSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            return await owner.Dispatcher.InvokeAsync(() => HandyControl.Controls.MessageBox.Show(owner, text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) is MessageBoxResult.Yes);
        }
        finally
        {
            _ = s_dialogSemaphore.Release();
        }
    }

    public static async Task ShowOkDialogAsync(string text, string caption, Window owner)
    {
        await s_dialogSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await owner.Dispatcher.InvokeAsync(() =>
            {
                _ = HandyControl.Controls.MessageBox.Show(owner, text, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        finally
        {
            _ = s_dialogSemaphore.Release();
        }
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

    public static ComboBoxItem[] GetFontWeightNames(string fontName)
    {
        FontFamily fontFamily = new(fontName);
        List<FontWeight> fontWeights = new(fontFamily.FamilyTypefaces.Count);
        foreach (FamilyTypeface familyTypeface in fontFamily.FamilyTypefaces)
        {
            if (!fontWeights.Contains(familyTypeface.Weight))
            {
                fontWeights.Add(familyTypeface.Weight);
            }
        }

        return fontWeights.OrderBy(static fontWeight => fontWeight.ToOpenTypeWeight()).Select(fontWeight => new ComboBoxItem
        {
            Content = fontWeight.ToString(),
            FontFamily = fontFamily,
            FontWeight = fontWeight
        }).ToArray();
    }

    public static FontWeight GetFontWeightFromName(string fontWeightName)
    {
        return fontWeightName switch
        {
            nameof(FontWeights.Black) => FontWeights.Black,
            nameof(FontWeights.Bold) => FontWeights.Bold,
            nameof(FontWeights.DemiBold) => FontWeights.DemiBold,
            nameof(FontWeights.ExtraBlack) => FontWeights.ExtraBlack,
            nameof(FontWeights.ExtraBold) => FontWeights.ExtraBold,
            nameof(FontWeights.ExtraLight) => FontWeights.ExtraLight,
            nameof(FontWeights.Heavy) => FontWeights.Heavy,
            nameof(FontWeights.Light) => FontWeights.Light,
            nameof(FontWeights.Medium) => FontWeights.Medium,
            nameof(FontWeights.Normal) => FontWeights.Normal,
            nameof(FontWeights.Regular) => FontWeights.Regular,
            nameof(FontWeights.SemiBold) => FontWeights.SemiBold,
            nameof(FontWeights.Thin) => FontWeights.Thin,
            nameof(FontWeights.UltraBlack) => FontWeights.UltraBlack,
            nameof(FontWeights.UltraBold) => FontWeights.UltraBold,
            nameof(FontWeights.UltraLight) => FontWeights.UltraLight,
            _ => int.TryParse(fontWeightName, out int fontWeight) ? FontWeight.FromOpenTypeWeight(fontWeight) : FontWeights.Normal
        };
    }

    public static Point GetMousePosition(bool mayNeedCoordinateConversion)
    {
        return GetMousePosition(WinApi.GetMousePosition(), mayNeedCoordinateConversion);
    }

    public static Point GetMousePosition(Point mousePosition, bool mayNeedCoordinateConversion)
    {
        return !mayNeedCoordinateConversion
            ? mousePosition
            : MagpieUtils.IsMagpieScaling()
                ? MagpieUtils.GetMousePosition(mousePosition)
                : mousePosition;
    }

    public static async Task CopyTextToClipboard(string text)
    {
        bool copied = false;
        do
        {
            try
            {
                // Clipboard.SetText(text) and Clipboard.SetDataObject(text, true) often fail with CLIPBRD_E_CANT_OPEN.
                // Clipboard.SetDataObject(text, false) almost never fails, but we still keep it in a try-catch block to be safe.
                // See: https://github.com/dotnet/wpf/issues/9901
                Clipboard.SetDataObject(text, false);
                copied = true;
            }
            catch (ExternalException ex)
            {
                LoggerManager.Logger.Warning(ex, "CopyTextToClipboard failed");
                await Task.Delay(5).ConfigureAwait(false);
            }
        }
        while (!copied);
    }
}
