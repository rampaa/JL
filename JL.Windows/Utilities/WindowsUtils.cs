using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using JL.Core;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Windows.GUI;
using NAudio.Wave;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace JL.Windows.Utilities;

public static class WindowsUtils
{
    private static WaveOut s_audioPlayer;

    public static Screen ActiveScreen { get; set; } =
        Screen.FromHandle(
            new WindowInteropHelper(MainWindow.Instance).Handle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);
    public static double DpiAwareWorkAreaWidth { get; set; } = ActiveScreen.Bounds.Width / Dpi.DpiScaleX;
    public static double DpiAwareWorkAreaHeight { get; set; } = ActiveScreen.Bounds.Height / Dpi.DpiScaleY;
    public static double DpiAwareXOffset { get; set; } = ConfigManager.PopupXOffset / Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.PopupYOffset / Dpi.DpiScaleY;
    public static double DpiAwareFixedPopupXPosition { get; set; } = ConfigManager.FixedPopupXPosition / Dpi.DpiScaleX;
    public static double DpiAwareFixedPopupYPosition { get; set; } = ConfigManager.FixedPopupYPosition / Dpi.DpiScaleX;

    public static bool KeyGestureComparer(KeyEventArgs e, KeyGesture keyGesture)
    {
        if (keyGesture == null)
            return false;

        if (keyGesture.Modifiers.Equals(ModifierKeys.Windows))
            return keyGesture.Key == e.Key && (Keyboard.Modifiers & ModifierKeys.Windows) == 0;
        else
            return keyGesture.Matches(null, e);
    }

    public static string KeyGestureToString(KeyGesture keyGesture)
    {
        StringBuilder keyGestureStringBuilder = new();

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
        {
            keyGestureStringBuilder.Append("Ctrl+");
        }

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            keyGestureStringBuilder.Append("Shift+");
        }

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            keyGestureStringBuilder.Append("Alt+");
        }

        keyGestureStringBuilder.Append(keyGesture.Key.ToString());

        return keyGestureStringBuilder.ToString();
    }

    public static KeyGesture KeyGestureSetter(string keyGestureName, KeyGesture keyGesture)
    {
        string rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

        if (rawKeyGesture != null)
        {
            KeyGestureConverter keyGestureConverter = new();
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                return (KeyGesture)keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                return (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture);
        }

        else
        {
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add(keyGestureName, KeyGestureToString(keyGesture));
            config.Save(ConfigurationSaveMode.Modified);

            return keyGesture;
        }
    }

    public static List<ComboBoxItem> FindJapaneseFonts()
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
                        if (glyphTypeFace.CharacterToGlyphMap.ContainsKey(20685))
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

    public static void ShowAddNameWindow(string selectedText)
    {
        AddNameWindow addNameWindowInstance = AddNameWindow.Instance;
        addNameWindowInstance.SpellingTextBox.Text = selectedText;
        addNameWindowInstance.Owner = MainWindow.Instance;
        addNameWindowInstance.ShowDialog();
    }

    public static void ShowAddWordWindow(string selectedText)
    {
        AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
        addWordWindowInstance.SpellingsTextBox.Text = selectedText;
        addWordWindowInstance.Owner = MainWindow.Instance;
        addWordWindowInstance.ShowDialog();
    }

    public static void ShowPreferencesWindow()
    {
        ConfigManager.Instance.LoadPreferences(PreferencesWindow.Instance);
        PreferencesWindow.Instance.Owner = MainWindow.Instance;
        PreferencesWindow.Instance.ShowDialog();
    }

    public static void ShowManageDictionariesWindow()
    {
        if (!File.Exists(Path.Join(Storage.ApplicationPath, "Config/dicts.json")))
            Utils.CreateDefaultDictsConfig();

        if (!File.Exists($"Resources/custom_words.txt"))
            File.Create($"Resources/custom_words.txt").Dispose();

        if (!File.Exists($"Resources/custom_names.txt"))
            File.Create($"Resources/custom_names.txt").Dispose();

        ManageDictionariesWindow.Instance.Owner = MainWindow.Instance;
        ManageDictionariesWindow.Instance.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        StatsWindow.Instance.Owner = MainWindow.Instance;
        StatsWindow.Instance.ShowDialog();
    }

    public static void SearchWithBrowser(string selectedText)
    {
        if (selectedText?.Length > 0)
            Process.Start(new ProcessStartInfo("cmd",
                $"/c start https://www.google.com/search?q={selectedText}^&hl=ja")
            { CreateNoWindow = true });
    }

    public static async Task UpdateJL(Version latestVersion)
    {
        string architecture = Environment.Is64BitProcess ? "x64" : "x86";
        string repoName =
            Storage.RepoUrl[(Storage.RepoUrl[..^1].LastIndexOf("/", StringComparison.Ordinal) + 1)..^1];
        Uri latestReleaseUrl = new(Storage.RepoUrl + "releases/download/" + latestVersion.ToString(2) + "/" +
                                   repoName + "-" + latestVersion.ToString(2) + "-win-" + architecture + ".zip");
        HttpRequestMessage request = new(HttpMethod.Get, latestReleaseUrl);
        HttpResponseMessage response = await Storage.Client.SendAsync(request).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ZipArchive archive = new(responseStream);

            string tmpDirectory = Path.Join(Storage.ApplicationPath, "tmp");

            if (Directory.Exists(tmpDirectory))
            {
                Directory.Delete(tmpDirectory, true);
            }

            Directory.CreateDirectory(tmpDirectory);
            archive.ExtractToDirectory(tmpDirectory);

            await MainWindow.Instance.Dispatcher.BeginInvoke(ConfigManager.SaveBeforeClosing);

            Process.Start(
                new ProcessStartInfo("cmd",
                    $"/c start {Path.Join(Storage.ApplicationPath, "update-helper.cmd")} & exit")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
        }
    }

    public static void InitializeMainWindow()
    {
        Storage.Frontend = MainWindow.Instance;

        if (!File.Exists(Path.Join(Storage.ApplicationPath, "Config/dicts.json")))
            Utils.CreateDefaultDictsConfig();

        if (!File.Exists($"Resources/custom_words.txt"))
            File.Create($"Resources/custom_words.txt").Dispose();

        if (!File.Exists($"Resources/custom_names.txt"))
            File.Create($"Resources/custom_names.txt").Dispose();

        Utils.DeserializeDicts().ContinueWith(_ =>
        {
            Storage.LoadDictionaries().ContinueWith(_ =>
                {
                    Storage.InitializePoS().ContinueWith(_ =>
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                    }).ConfigureAwait(false);
                }
            ).ConfigureAwait(false);
        }).ConfigureAwait(false);

        ConfigManager.Instance.ApplyPreferences();

        if (ConfigManager.CheckForJLUpdatesOnStartUp)
        {
            Networking.CheckForJLUpdates(true);
        }
    }

    public static void PlayAudio(byte[] audio, float volume)
    {
        try
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (s_audioPlayer != null)
                {
                    s_audioPlayer.Dispose();
                }

                s_audioPlayer = new WaveOut { Volume = volume };

                s_audioPlayer.Init(new Mp3FileReader(new MemoryStream(audio)));
                s_audioPlayer.Play();
            });
        }
        catch (Exception e)
        {
            Utils.Logger.Error(e, "Error playing audio: " + JsonSerializer.Serialize(audio));
            Alert(AlertLevel.Error, "Error playing audio");
        }
    }

    public static void Motivate(string motivationFolder)
    {
        try
        {
            Random rand = new();

            string[] filePaths = Directory.GetFiles(motivationFolder);
            int numFiles = filePaths.Length;

            if (numFiles == 0)
            {
                Utils.Logger.Error("Motivation folder is empty!");
                Alert(AlertLevel.Error, "Motivation folder is empty!");
                return;
            }

            string randomFilePath = filePaths[rand.Next(numFiles)];
            byte[] randomFile = File.ReadAllBytes(randomFilePath);
            PlayAudio(randomFile, 1);
            Storage.SessionStats.Imoutos += 1;
        }
        catch (Exception e)
        {
            Utils.Logger.Error(e, "Error motivating");
            Alert(AlertLevel.Error, "Error motivating");
        }
    }

    public static void Try(Action a, object variable, string key)
    {
        try
        {
            a();
        }
        catch
        {
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (ConfigurationManager.AppSettings.Get(key) == null)
                config.AppSettings.Settings.Add(key, variable.ToString());
            else
                config.AppSettings.Settings[key].Value = variable.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }

    public static void AddToConfig(string key, string value)
    {
        Configuration config =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Add(key, value);
        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
    }

    public static void KeyGestureSaver(string key, string rawKeyGesture)
    {
        Configuration config =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        if (rawKeyGesture.StartsWith("Win+"))
            config.AppSettings.Settings[key].Value = rawKeyGesture[4..];
        else
            config.AppSettings.Settings[key].Value = rawKeyGesture;

        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke((Action)async delegate
            {
                List<AlertWindow> alertWindowList = Application.Current.Windows.OfType<AlertWindow>().ToList();

                AlertWindow alertWindow = new();

                alertWindow.Left = DpiAwareWorkAreaWidth - alertWindow.Width - 30;
                alertWindow.Top =
                    alertWindowList.Count * ((alertWindowList.LastOrDefault()?.ActualHeight ?? 0) + 2) + 30;

                alertWindow.DisplayAlert(alertLevel, message);
                alertWindow.Show();
                await Task.Delay(4004);
                alertWindow.Close();
            });
        }
    }

    public static Size MeasureTextSize(string text, int fontSize)
    {
        FormattedText formattedText = new(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface(ConfigManager.PopupFont.Source),
            fontSize,
            Brushes.Transparent,
            new NumberSubstitution(),
            Dpi.PixelsPerDip);

        return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }
}
