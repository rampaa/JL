using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
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
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using FlowDirection = System.Windows.FlowDirection;
using PopupWindow = JL.Windows.GUI.PopupWindow;
using TextBox = System.Windows.Controls.TextBox;
using Window = System.Windows.Window;

namespace JL.Windows.Utilities;

internal static class WindowsUtils
{
    private static readonly Random s_random = new();
    public static Typeface PopupFontTypeFace { get; set; } = new(ConfigManager.PopupFont.Source);
    private static DateTime s_lastAudioPlayTime;
    public static WaveOut? AudioPlayer { get; private set; }

    public static Screen ActiveScreen { get; set; } = Screen.FromHandle(MainWindow.Instance.WindowHandle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);
    public static double DpiAwareXOffset { get; set; } = ConfigManager.PopupXOffset * Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.PopupYOffset * Dpi.DpiScaleY;

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

    public static ComboBoxItem[] CloneJapaneseFontComboBoxItems(ComboBoxItem[] japaneseFontsComboBoxItems)
    {
        ComboBoxItem[] clone = new ComboBoxItem[japaneseFontsComboBoxItems.Length];
        for (int i = 0; i < japaneseFontsComboBoxItems.Length; i++)
        {
            ComboBoxItem comboBoxItem = japaneseFontsComboBoxItems[i];

            clone[i] = new ComboBoxItem
            {
                Content = comboBoxItem.FontFamily.Source,
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
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
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
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = addWordWindowInstance.ShowDialog();
    }

    public static void ShowPreferencesWindow()
    {
        PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
        ConfigManager.LoadPreferenceWindow(preferencesWindow);
        preferencesWindow.Owner = MainWindow.Instance;
        preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
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
        manageDictionariesWindow.Owner = MainWindow.Instance;
        manageDictionariesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
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
        manageFrequenciesWindow.Owner = MainWindow.Instance;
        manageFrequenciesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = manageFrequenciesWindow.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        Stats.IncrementStat(StatType.Time, StatsUtils.StatsStopWatch.ElapsedTicks);
        StatsUtils.StatsStopWatch.Reset();

        StatsWindow statsWindow = StatsWindow.Instance;
        statsWindow.Owner = MainWindow.Instance;
        statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
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
        manageAudioSourcesWindow.Owner = MainWindow.Instance;
        manageAudioSourcesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        StatsUtils.StatsStopWatch.Stop();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        _ = manageAudioSourcesWindow.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        string browserPath = "";
        if (!string.IsNullOrWhiteSpace(ConfigManager.BrowserPath))
        {
            browserPath = $"\"{ConfigManager.BrowserPath}\"";
        }

        if (selectedText?.Length > 0)
        {
            string urlToBeSearched = Uri.IsWellFormedUriString(selectedText, UriKind.Absolute)
                ? selectedText
                : ConfigManager.SearchUrl.Replace("{SearchTerm}", HttpUtility.UrlEncode(selectedText), StringComparison.Ordinal);

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

        if (CoreConfigManager.CheckForJLUpdatesOnStartUp)
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
            string randomFilePath = filePaths[s_random.Next(numFiles)];
#pragma warning restore CA5394 // Do not use insecure randomness

            byte[] audioData = await File.ReadAllBytesAsync(randomFilePath).ConfigureAwait(false);
            PlayAudio(audioData, "mp3");
            Stats.IncrementStat(StatType.Imoutos);
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

    public static void SetSizeToContent(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, double minWidth, double minHeight, PopupWindow window)
    {
        window.MaxHeight = maxHeight;
        window.MaxWidth = maxWidth;
        window.MinHeight = minHeight;
        window.MinWidth = minWidth;

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

    public static void SetSizeToContent(bool dynamicWidth, bool dynamicHeight, double maxWidth, double maxHeight, double minWidth, double minHeight, double width, double height, MainWindow window)
    {
        if (dynamicWidth && dynamicHeight)
        {
            window.MaxHeight = maxHeight;
            window.MaxWidth = maxWidth;
            window.MinHeight = minHeight;
            window.MinWidth = minWidth;
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }

        else if (dynamicHeight)
        {
            window.MaxHeight = maxHeight;
            window.MinHeight = minHeight;
            window.MaxWidth = double.PositiveInfinity;
            window.MinWidth = 100;
            window.SizeToContent = SizeToContent.Height;
            window.Width = width;
        }

        else if (dynamicWidth)
        {
            window.MaxHeight = double.PositiveInfinity;
            window.MinHeight = 50;
            window.MaxWidth = maxWidth;
            window.MinWidth = minWidth;
            window.SizeToContent = SizeToContent.Width;
            window.Height = height;
        }

        else
        {
            window.SizeToContent = SizeToContent.Manual;
            window.MaxHeight = double.PositiveInfinity;
            window.MaxWidth = double.PositiveInfinity;
            window.MinHeight = 50;
            window.MinWidth = 100;
            window.Width = width;
            window.Height = height;
        }
    }

    public static void CopyTextToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        bool captureTextFromClipboard = CoreConfigManager.CaptureTextFromClipboard;
        if (captureTextFromClipboard)
        {
            WinApi.UnsubscribeFromClipboardChanged(MainWindow.Instance.WindowHandle);
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
            catch (ExternalException ex)
            {
                Utils.Logger.Warning(ex, "CopyTextToClipboard failed");
            }
        } while (retry);

        if (captureTextFromClipboard)
        {
            WinApi.SubscribeToClipboardChanged(MainWindow.Instance.WindowHandle);
        }
    }

    public static void HandlePostCopy(string text, string? subsequentText, string? mergedText)
    {
        bool newText = mergedText is null;
        if (ConfigManager.EnableBacklog)
        {
            if (newText)
            {
                BacklogUtils.AddToBacklog(text);
            }
            else
            {
                BacklogUtils.ReplaceLastBacklogText(mergedText!);
            }
        }

        if (ConfigManager.TextToSpeechOnTextChange
            && SpeechSynthesisUtils.InstalledVoiceWithHighestPriority is not null)
        {
            _ = SpeechSynthesisUtils.TextToSpeech(SpeechSynthesisUtils.InstalledVoiceWithHighestPriority, text).ConfigureAwait(false);
        }

        string strippedText = ConfigManager.StripPunctuationBeforeCalculatingCharacterCount
            ? JapaneseUtils.RemovePunctuation(subsequentText ?? text)
            : subsequentText ?? text;

        if (strippedText.Length > 0)
        {
            Stats.IncrementStat(StatType.Characters, new StringInfo(strippedText).LengthInTextElements);

            if (newText)
            {
                Stats.IncrementStat(StatType.Lines);
            }
        }
    }

    public static void UpdateMainWindowVisibility()
    {
        if (!MainWindow.Instance.FirstPopupWindow.IsVisible)
        {
            if (!MainWindow.Instance.IsMouseOver)
            {
                if (ConfigManager.TextOnlyVisibleOnHover)
                {
                    MainWindow.Instance.MainGrid.Opacity = 0;
                }

                if (ConfigManager.ChangeMainWindowBackgroundOpacityOnUnhover)
                {
                    MainWindow.Instance.Background.Opacity = ConfigManager.MainWindowBackgroundOpacityOnUnhover / 100;
                }
            }
        }

        StatsUtils.StatsStopWatch.Start();

        if (ConfigManager.GlobalHotKeys && !ConfigManager.DisableHotkeys)
        {
            WinApi.RegisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }
    }

    public static void ChangeTheme(SkinType skin)
    {
        ResourceDictionary resources = Application.Current.Resources;

        //resources.MergedDictionaries.Clear();
        //resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ResourceDictionary.xaml", UriKind.Relative) });
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
}
