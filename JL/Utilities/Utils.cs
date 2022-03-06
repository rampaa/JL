using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using JL.Dicts;
using JL.GUI;
using NAudio.Wave;
using Serilog;
using Serilog.Core;

namespace JL.Utilities
{
    public static class Utils
    {
        public static readonly Logger Logger = new LoggerConfiguration().WriteTo.File("Logs/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(90),
                shared: true)
            .CreateLogger();

        private static WaveOut s_audioPlayer;

        public static IEnumerable<string> UnicodeIterator(this string s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                if (char.IsHighSurrogate(s, i)
                    && s.Length > i + 1
                    && char.IsLowSurrogate(s, i + 1))
                {
                    yield return char.ConvertFromUtf32(char.ConvertToUtf32(s, i));
                    ++i;
                }
                else
                {
                    yield return s[i].ToString();
                }
            }
        }

        public static List<ComboBoxItem> FindJapaneseFonts()
        {
            List<ComboBoxItem> japaneseFonts = new();

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                ComboBoxItem comboBoxItem = new()
                {
                    Content = fontFamily.Source, FontFamily = fontFamily, Foreground = Brushes.White
                };

                if (fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
                {
                    japaneseFonts.Add(comboBoxItem);
                }

                else if (fontFamily.FamilyNames.Keys is {Count: 1} &&
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

                    alertWindow.Left = SystemParameters.WorkArea.Width - alertWindow.Width - 30;
                    alertWindow.Top =
                        alertWindowList.Count * ((alertWindowList.LastOrDefault()?.ActualHeight ?? 0) + 2) + 30;

                    alertWindow.DisplayAlert(alertLevel, message);
                    alertWindow.Show();
                    await Task.Delay(4004);
                    alertWindow.Close();
                });
            }
        }

        public static void CreateDefaultDictsConfig()
        {
            var jso = new JsonSerializerOptions {WriteIndented = true, Converters = {new JsonStringEnumConverter(),}};

            try
            {
                Directory.CreateDirectory(Path.Join(Storage.ApplicationPath, "Config"));
                File.WriteAllText(Path.Join(Storage.ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(Storage.BuiltInDicts, jso));
            }
            catch (Exception e)
            {
                Alert(AlertLevel.Error, "Couldn't write default Dicts config");
                Logger.Error(e, "Couldn't write default Dicts config");
            }
        }

        public static void SerializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions
                {
                    WriteIndented = true, Converters = {new JsonStringEnumConverter(),}
                };

                File.WriteAllTextAsync(Path.Join(Storage.ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(Storage.Dicts, jso));
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "SerializeDicts failed");
                throw;
            }
        }

        public static async Task DeserializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions {Converters = {new JsonStringEnumConverter(),}};

                Dictionary<DictType, Dict> deserializedDicts = await JsonSerializer
                    .DeserializeAsync<Dictionary<DictType, Dict>>(
                        new StreamReader(Path.Join(Storage.ApplicationPath, "Config/dicts.json")).BaseStream, jso)
                    .ConfigureAwait(false);

                if (deserializedDicts != null)
                {
                    foreach (Dict dict in deserializedDicts.Values)
                    {
                        if (!Storage.Dicts.ContainsKey(dict.Type))
                        {
                            dict.Contents = new Dictionary<string, List<IResult>>();
                            Storage.Dicts.Add(dict.Type, dict);
                        }
                    }
                }
                else
                {
                    Utils.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                    Utils.Logger.Error("Couldn't load Config/dicts.json");
                }
            }
            catch (Exception e)
            {
                Utils.Logger.Fatal(e, "DeserializeDicts failed");
                throw;
            }
        }

        public static void ShowAddNameWindow(string selectedText)
        {
            AddNameWindow addNameWindowInstance = AddNameWindow.Instance;
            addNameWindowInstance.SpellingTextBox.Text = selectedText;
            addNameWindowInstance.ShowDialog();
        }

        public static void ShowAddWordWindow(string selectedText)
        {
            AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
            addWordWindowInstance.SpellingsTextBox.Text = selectedText;
            addWordWindowInstance.ShowDialog();
        }

        public static void ShowPreferencesWindow()
        {
            ConfigManager.LoadPreferences(PreferencesWindow.Instance);
            PreferencesWindow.Instance.ShowDialog();
        }

        public static void ShowManageDictionariesWindow()
        {
            if (!File.Exists(Path.Join(Storage.ApplicationPath, "Config/dicts.json")))
                Utils.CreateDefaultDictsConfig();

            if (!File.Exists("Resources/custom_words.txt"))
                File.Create("Resources/custom_words.txt").Dispose();

            if (!File.Exists("Resources/custom_names.txt"))
                File.Create("Resources/custom_names.txt").Dispose();

            ManageDictionariesWindow.Instance.ShowDialog();
        }

        public static void SearchWithBrowser(string selectedText)
        {
            if (selectedText?.Length > 0)
                Process.Start(new ProcessStartInfo("cmd",
                    $"/c start https://www.google.com/search?q={selectedText}^&hl=ja") {CreateNoWindow = true});
        }

        public static string GetMd5String(byte[] bytes)
        {
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5"))!.ComputeHash(bytes);
            string encoded = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

            return encoded;
        }

        public static void PlayAudio(byte[] audio, float volume)
        {
            try
            {
                if (s_audioPlayer != null)
                {
                    s_audioPlayer.Dispose();
                }

                s_audioPlayer = new WaveOut {Volume = volume};

                s_audioPlayer.Init(new Mp3FileReader(new MemoryStream(audio)));
                s_audioPlayer.Play();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error playing audio: " + JsonSerializer.Serialize(audio));
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
                    Logger.Error("Motivation folder is empty!");
                    Alert(AlertLevel.Error, "Motivation folder is empty!");
                    return;
                }

                string randomFilePath = filePaths[rand.Next(numFiles)];
                byte[] randomFile = File.ReadAllBytes(randomFilePath);
                PlayAudio(randomFile, 1);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error motivating");
                Alert(AlertLevel.Error, "Error motivating");
            }
        }

        public static async void CheckForJLUpdates(bool isAutoCheck)
        {
            try
            {
                HttpResponseMessage response = await Storage.Client.GetAsync(Storage.RepoUrl + "releases/latest");
                string responseUri = response.RequestMessage!.RequestUri!.ToString();
                Version latestVersion =
                    new(responseUri[(responseUri.LastIndexOf("/", StringComparison.Ordinal) + 1)..]);
                if (latestVersion > Storage.Version)
                {
                    if (MessageBox.Show("A new version of JL is available. Would you like to download it now?", "",
                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes,
                            MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                    {
                        MessageBox.Show(
                            $"This may take a while. Please don't manually shut down the program until it's updated.",
                            "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                            MessageBoxOptions.DefaultDesktopOnly);

                        await UpdateJL(latestVersion).ConfigureAwait(false);
                    }
                }

                else if (!isAutoCheck)
                {
                    MessageBox.Show("JL is up to date", "",
                        MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes,
                        MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            catch
            {
                Logger.Warning("Couldn't update JL");
                Alert(AlertLevel.Warning, "Couldn't update JL");
            }
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

                Process.Start(new ProcessStartInfo("cmd", $"/c start {Path.Join(Storage.ApplicationPath, "update-helper.cmd")} & exit") { UseShellExecute = false, CreateNoWindow = true });
            }
        }
    }
}
