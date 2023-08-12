using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using JL.Core.Audio;
using JL.Core.Dicts;
using JL.Core.Freqs;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.SpeechSynthesis;
using NAudio.Vorbis;
using NAudio.Wave;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using PopupWindow = JL.Windows.GUI.PopupWindow;
using Window = System.Windows.Window;

namespace JL.Windows.Utilities;

internal static class WindowsUtils
{
    private static readonly Random s_random = new();
    private static DateTime s_lastAudioPlayTime = new();
    public static WaveOut? AudioPlayer { get; private set; }

    public static System.Windows.Forms.Screen ActiveScreen { get; set; } =
        System.Windows.Forms.Screen.FromHandle(MainWindow.Instance.WindowHandle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);
    public static double DpiAwareWorkAreaWidth { get; set; } = ActiveScreen.Bounds.Width / Dpi.DpiScaleX;
    public static double DpiAwareWorkAreaHeight { get; set; } = ActiveScreen.Bounds.Height / Dpi.DpiScaleY;
    public static double DpiAwarePopupMaxWidth { get; set; } = ConfigManager.PopupMaxWidth / Dpi.DpiScaleX;
    public static double DpiAwarePopupMaxHeight { get; set; } = ConfigManager.PopupMaxHeight / Dpi.DpiScaleY;
    public static double DpiAwareXOffset { get; set; } = ConfigManager.PopupXOffset / Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.PopupYOffset / Dpi.DpiScaleY;
    public static double DpiAwareFixedPopupXPosition { get; set; } = ConfigManager.FixedPopupXPosition / Dpi.DpiScaleX;
    public static double DpiAwareFixedPopupYPosition { get; set; } = ConfigManager.FixedPopupYPosition / Dpi.DpiScaleY;

    public static IEnumerable<ComboBoxItem> FindJapaneseFonts()
    {
        List<ComboBoxItem> japaneseFonts = new();

        foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
        {
            ComboBoxItem comboBoxItem = new()
            {
                Content = fontFamily.Source,
                FontFamily = fontFamily,
                Foreground = Brushes.White
            };

            if (fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
            {
                japaneseFonts.Add(comboBoxItem);
            }

            else if (fontFamily.FamilyNames.Keys is { Count: 1 } &&
                     fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("en-US")))
            {
                bool foundGlyph = false;
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
                    comboBoxItem.Foreground = Brushes.DimGray;
                    japaneseFonts.Add(comboBoxItem);
                }
            }
            else
            {
                comboBoxItem.Foreground = Brushes.DimGray;
                japaneseFonts.Add(comboBoxItem);
            }
        }

        return japaneseFonts;
    }

    public static void ShowAddNameWindow(string? selectedText, string reading = "")
    {
        AddNameWindow addNameWindowInstance = AddNameWindow.Instance;
        addNameWindowInstance.SpellingTextBox.Text = selectedText;
        addNameWindowInstance.ReadingTextBox.Text = reading;
        MainWindow mainWindow = MainWindow.Instance;
        addNameWindowInstance.Owner = mainWindow;
        addNameWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
        }

        _ = addNameWindowInstance.ShowDialog();
    }

    public static void ShowAddWordWindow(string? selectedText)
    {
        AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
        addWordWindowInstance.SpellingsTextBox.Text = selectedText;
        MainWindow mainWindow = MainWindow.Instance;
        addWordWindowInstance.Owner = mainWindow;
        addWordWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
        }

        _ = addWordWindowInstance.ShowDialog();
    }

    public static void ShowPreferencesWindow()
    {
        PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
        ConfigManager.LoadPreferences(preferencesWindow);
        MainWindow mainWindow = MainWindow.Instance;
        preferencesWindow.Owner = mainWindow;
        preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
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
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
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
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
        }

        _ = manageFrequenciesWindow.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        Stats.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks);
        StatsUtils.StatsStopWatch.Reset();

        StatsWindow statsWindow = StatsWindow.Instance;
        MainWindow mainWindow = MainWindow.Instance;
        statsWindow.Owner = mainWindow;
        statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
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
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
        }

        _ = manageAudioSourcesWindow.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        string browserPath = "";
        if (!string.IsNullOrWhiteSpace(ConfigManager.BrowserPath))
        {
            browserPath = string.Create(CultureInfo.InvariantCulture, $"\"{ConfigManager.BrowserPath}\"");
        }

        if (selectedText?.Length > 0)
        {
            _ = Process.Start(new ProcessStartInfo("cmd",
                string.Create(CultureInfo.InvariantCulture, $"/c start \"\" {browserPath} \"{ConfigManager.SearchUrl.Replace("{SearchTerm}", System.Web.HttpUtility.UrlEncode(selectedText), StringComparison.Ordinal)}\""))
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

            Application.Current.Dispatcher.Invoke(ConfigManager.SaveBeforeClosing);

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
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't update JL");
        }
    }

    public static async Task InitializeMainWindow()
    {
        Utils.Frontend = new WindowsFrontend();

        await Utils.CoreInitialize().ConfigureAwait(true);

        if (ConfigManager.CheckForJLUpdatesOnStartUp)
        {
            PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
            preferencesWindow.CheckForJLUpdatesButton.IsEnabled = false;
            await Networking.CheckForJLUpdates(true).ConfigureAwait(true);
            preferencesWindow.CheckForJLUpdatesButton.IsEnabled = true;
        }
    }

    public static void PlayAudio(byte[] audio, string audioFormat, float volume)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                AudioPlayer?.Dispose();
                AudioPlayer = new WaveOut { Volume = volume };

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
                Utils.Frontend.Alert(AlertLevel.Error, "Error playing audio");
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
                Utils.Frontend.Alert(AlertLevel.Warning, "Motivation folder is empty!");
                return;
            }

#pragma warning disable CA5394
            string randomFilePath = filePaths[s_random.Next(numFiles)];
#pragma warning restore CA5394

            byte[] audioData = await File.ReadAllBytesAsync(randomFilePath).ConfigureAwait(false);
            PlayAudio(audioData, "mp3", 1);
            Stats.IncrementStat(StatType.Imoutos);
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error motivating");
            Utils.Frontend.Alert(AlertLevel.Error, "Error motivating");
        }
    }

    public static Brush? BrushFromHex(string hexColorString)
    {
        return (Brush?)new BrushConverter().ConvertFrom(hexColorString);
    }

    public static Brush? FrozenBrushFromHex(string hexColorString)
    {
        var brush = (Brush?)new BrushConverter().ConvertFrom(hexColorString);
        brush?.Freeze();
        return brush;
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            List<AlertWindow> alertWindowList = Application.Current.Windows.OfType<AlertWindow>().ToList();

            AlertWindow alertWindow = new();

            alertWindow.Left = DpiAwareWorkAreaWidth - alertWindow.Width - 30;
            alertWindow.Top = alertWindowList.Sum(static aw => aw.ActualHeight + 2) + 30;

            alertWindow.SetAlert(alertLevel, message);
            alertWindow.Show();
            await Task.Delay(4004).ConfigureAwait(true);
            alertWindow.Close();
        });
    }

    public static Size MeasureTextSize(string text, int fontSize)
    {
        FormattedText formattedText = new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(ConfigManager.PopupFont.Source),
            fontSize,
            Brushes.Transparent,
            new NumberSubstitution(),
            Dpi.PixelsPerDip);

        return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }

    public static void ShowColorPicker(Button button)
    {
        ColorPicker picker = SingleOpenHelper.CreateControl<ColorPicker>();
        HandyControl.Controls.PopupWindow window = new() { PopupElement = picker };
        picker.SelectedBrush = (SolidColorBrush)button.Tag;

        picker.Canceled += delegate
        {
            window.Close();
        };

        picker.Confirmed += delegate
        {
            ConfirmColor(button, picker.SelectedBrush, window);
        };

        window.ShowDialog(picker, false);
    }

    private static void ConfirmColor(Button button, Brush selectedBrush, Window window)
    {
        SetButtonColor(button, selectedBrush);
        window.Close();
    }

    public static void SetButtonColor(Button button, Brush selectedBrush)
    {
        selectedBrush.Freeze();
        button.Tag = selectedBrush;

        Color selectedBrushColor = ((SolidColorBrush)selectedBrush).Color;

        button.Background = CreateOpaqueBrush(selectedBrushColor);
    }

    private static Brush CreateOpaqueBrush(Color color)
    {
        Brush opaqueBrush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        opaqueBrush.Freeze();
        return opaqueBrush;
    }

    public static void Unselect(System.Windows.Controls.TextBox? tb)
    {
        if (tb is null)
        {
            return;
        }

        double verticalOffset = tb.VerticalOffset;
        tb.Select(0, 0);
        tb.ScrollToVerticalOffset(verticalOffset);
    }

    public static void SetSizeToContentForPopup(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, Window window)
    {
        window.MaxHeight = maxHeight;
        window.MaxWidth = maxWidth;

        if (dynamicWidth && dynamicHeight)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            window.SizeToContent = SizeToContent.Height;
            window.Width = maxWidth;
        }

        else if (dynamicWidth)
        {
            window.SizeToContent = SizeToContent.Width;
            window.Height = maxHeight;
        }

        else
        {
            window.SizeToContent = SizeToContent.Manual;
            window.Height = maxHeight;
            window.Width = maxWidth;
        }
    }

    public static void SetSizeToContentForMainWindow(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, double width, double height, Window window)
    {
        if (dynamicWidth && dynamicHeight)
        {
            window.MaxHeight = maxHeight;
            window.MaxWidth = maxWidth;
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            window.MaxHeight = maxHeight;
            window.MaxWidth = double.PositiveInfinity;
            window.SizeToContent = SizeToContent.Height;
            window.Width = width;
        }

        else if (dynamicWidth)
        {
            window.MaxHeight = double.PositiveInfinity;
            window.MaxWidth = maxWidth;
            window.SizeToContent = SizeToContent.Width;
            window.Height = height;
        }

        else
        {
            window.SizeToContent = SizeToContent.Manual;
            window.MaxHeight = double.PositiveInfinity;
            window.MaxWidth = double.PositiveInfinity;
            window.Width = width;
            window.Height = height;
        }
    }

    public static void SetSizeToContent(bool dynamicWidth, bool dynamicHeight, Window window)
    {
        if (dynamicWidth && dynamicHeight)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            window.SizeToContent = SizeToContent.Height;
        }

        else if (dynamicWidth)
        {
            window.SizeToContent = SizeToContent.Width;
        }
    }

    public static void HidePopups(PopupWindow? rootPopup)
    {
        PopupWindow? currentPopupWindow = rootPopup;

        while (currentPopupWindow?.IsVisible ?? false)
        {
            currentPopupWindow.HidePopup();
            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    public static void CopyTextToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        bool retry = true;
        do
        {
            try
            {
                // Using Clipboard.SetText or setting the "copy" parameter of SetDataObject to true
                // Results in "System.Runtime.InteropServices.COMException (0x800401D0): OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))"
                Clipboard.SetDataObject(text, false);
                retry = false;
            }
            catch (Exception ex)
            {
                Utils.Logger.Warning(ex, "CopyTextToClipboard failed");
            }
        } while (retry);
    }

    public static void UpdateMainWindowVisibility()
    {
        MainWindow mainWindow = MainWindow.Instance;

        if (!mainWindow.FirstPopupWindow.IsVisible)
        {
            if (!mainWindow.IsMouseOver)
            {
                if (ConfigManager.TextOnlyVisibleOnHover)
                {
                    mainWindow.MainGrid.Opacity = 0;
                }

                if (ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
                {
                    mainWindow.Background.Opacity = ConfigManager.MainWindowBackgroundOpacityOnUnhover / 100;
                }
            }
        }

        StatsUtils.StatsStopWatch.Start();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.RegisterAllHotKeys(MainWindow.Instance.WindowHandle);
        }
    }

    public static void ChangeTheme(SkinType skin)
    {
        ResourceDictionary resources = Application.Current.Resources;

        resources.MergedDictionaries.Clear();
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

    public static byte[]? GetImageFromClipboardAsByteArray()
    {
        return Application.Current.Dispatcher.Invoke(static () =>
        {
            while (Clipboard.ContainsImage())
            {
                try
                {
                    BitmapSource? image = Clipboard.GetImage();

                    if (image is not null)
                    {
                        PngBitmapEncoder pngBitmapEncoder = new();
                        pngBitmapEncoder.Frames.Add(BitmapFrame.Create(image));

                        using MemoryStream stream = new();
                        pngBitmapEncoder.Save(stream);
                        return stream.ToArray();
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Utils.Logger.Warning(ex, "GetImageFromClipboard failed");
                }
            }

            return null;
        });
    }
}
