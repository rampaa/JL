using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Tools;
using JL.Core;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Windows.GUI;
using NAudio.Wave;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace JL.Windows.Utilities;

public static class WindowsUtils
{
    private static WaveOut? s_audioPlayer;

    public static Screen ActiveScreen { get; set; } =
        Screen.FromHandle(
            new WindowInteropHelper(MainWindow.Instance).Handle);

    public static DpiScale Dpi { get; set; } = VisualTreeHelper.GetDpi(MainWindow.Instance);
    public static double DpiAwareWorkAreaWidth { get; set; } = ActiveScreen.Bounds.Width / Dpi.DpiScaleX;
    public static double DpiAwareWorkAreaHeight { get; set; } = ActiveScreen.Bounds.Height / Dpi.DpiScaleY;
    public static double DpiAwarePopupMaxWidth { get; set; } = ConfigManager.PopupMaxWidth / Dpi.DpiScaleX;
    public static double DpiAwarePopupMaxHeight { get; set; } = ConfigManager.PopupMaxHeight / Dpi.DpiScaleY;
    public static double DpiAwareXOffset { get; set; } = ConfigManager.PopupXOffset / Dpi.DpiScaleX;
    public static double DpiAwareYOffset { get; set; } = ConfigManager.PopupYOffset / Dpi.DpiScaleY;
    public static double DpiAwareFixedPopupXPosition { get; set; } = ConfigManager.FixedPopupXPosition / Dpi.DpiScaleX;
    public static double DpiAwareFixedPopupYPosition { get; set; } = ConfigManager.FixedPopupYPosition / Dpi.DpiScaleY;

    public static bool KeyGestureComparer(KeyEventArgs e, KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers.Equals(ModifierKeys.Windows))
            return keyGesture.Key == e.Key && (Keyboard.Modifiers & ModifierKeys.Windows) == 0;
        else if (keyGesture.Modifiers == 0)
            return keyGesture.Key == e.Key;
        else
            return keyGesture.Matches(null, e);
    }

    public static bool KeyGestureComparer(KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers == ModifierKeys.Windows)
            return Keyboard.IsKeyDown(keyGesture.Key) && (Keyboard.Modifiers & ModifierKeys.Windows) == 0;
        else if (keyGesture.Modifiers == 0)
            return Keyboard.IsKeyDown(keyGesture.Key);
        else
            return Keyboard.IsKeyDown(keyGesture.Key) && Keyboard.Modifiers == keyGesture.Modifiers;
    }

    public static string KeyGestureToString(KeyGesture keyGesture)
    {
        StringBuilder keyGestureStringBuilder = new();

        if (keyGesture.Key == Key.LeftShift || keyGesture.Key == Key.RightShift
            || keyGesture.Key == Key.LeftCtrl || keyGesture.Key == Key.RightCtrl
            || keyGesture.Key == Key.LeftAlt || keyGesture.Key == Key.RightAlt)
        {
            keyGestureStringBuilder.Append(keyGesture.Key.ToString());
        }

        else
        {
            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
            {
                keyGestureStringBuilder.Append("Ctrl+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                keyGestureStringBuilder.Append("Alt+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift) && keyGestureStringBuilder.Length > 0)
            {
                keyGestureStringBuilder.Append("Shift+");
            }

            if (keyGesture.Key != Key.None)
            {
                keyGestureStringBuilder.Append(keyGesture.Key.ToString());
            }
        }

        return keyGestureStringBuilder.ToString();
    }

    public static KeyGesture KeyGestureSetter(string keyGestureName, KeyGesture keyGesture)
    {
        string? rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

        if (rawKeyGesture != null)
        {
            KeyGestureConverter keyGestureConverter = new();

            if (rawKeyGesture.Contains("Ctrl") || rawKeyGesture.Contains("Alt"))
            {
                return (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture)!;
            }

            else
            {
                return (KeyGesture)keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture)!;
            }
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

            if (fontFamily.FamilyNames!.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
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
                        if (glyphTypeFace!.CharacterToGlyphMap!.ContainsKey(20685))
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

    public static void ShowAddNameWindow(string? selectedText)
    {
        AddNameWindow addNameWindowInstance = AddNameWindow.Instance;
        addNameWindowInstance.SpellingTextBox.Text = selectedText;
        addNameWindowInstance.Owner = MainWindow.Instance;
        addNameWindowInstance.ShowDialog();
    }

    public static void ShowAddWordWindow(string? selectedText)
    {
        AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
        addWordWindowInstance.SpellingsTextBox!.Text = selectedText;
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
        if (!File.Exists(Path.Join(Storage.ConfigPath, "dicts.json")))
            Utils.CreateDefaultDictsConfig();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_words.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_words.txt").Dispose();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_names.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_names.txt").Dispose();

        ManageDictionariesWindow.Instance.Owner = MainWindow.Instance;
        ManageDictionariesWindow.Instance.ShowDialog();
    }

    public static void ShowManageFrequenciesWindow()
    {
        if (!File.Exists(Path.Join(Storage.ConfigPath, "freqs.json")))
            Utils.CreateDefaultFreqsConfig();

        ManageFrequenciesWindow.Instance.Owner = MainWindow.Instance;
        ManageFrequenciesWindow.Instance.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        StatsWindow.Instance.Owner = MainWindow.Instance;
        StatsWindow.Instance.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        if (selectedText?.Length > 0)
        {
            Process.Start(new ProcessStartInfo("cmd",
                $"/c start https://www.google.com/search?q={System.Web.HttpUtility.UrlEncode(selectedText)}^&hl=ja")
            { CreateNoWindow = true });
        }
    }

    public static async Task UpdateJL(Uri latestReleaseUrl)
    {
        HttpRequestMessage downloadRequest = new(HttpMethod.Get, latestReleaseUrl);
        HttpResponseMessage downloadResponse = await Storage.Client.SendAsync(downloadRequest).ConfigureAwait(false);

        if (downloadResponse.IsSuccessStatusCode)
        {
            Stream downloadResponseStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            ZipArchive archive = new(downloadResponseStream);

            string tmpDirectory = Path.Join(Storage.ApplicationPath, "tmp");

            if (Directory.Exists(tmpDirectory))
            {
                Directory.Delete(tmpDirectory, true);
            }

            Directory.CreateDirectory(tmpDirectory);
            archive.ExtractToDirectory(tmpDirectory);

            await MainWindow.Instance.Dispatcher!.BeginInvoke(ConfigManager.SaveBeforeClosing);

            Process.Start(
                new ProcessStartInfo("cmd",
                $"/c start \"JL Updater\" \"{Path.Join(Storage.ApplicationPath, "update-helper.cmd")}\""));
        }

        else
        {
            Utils.Logger.Error("Couldn't update JL. {StatusCode} {ReasonPhrase}", downloadResponse.StatusCode, downloadResponse.ReasonPhrase);
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't update JL");
        }
    }

    public static async void InitializeMainWindow()
    {
        Storage.Frontend = MainWindow.Instance;

        Utils.CoreInitialize();

        ConfigManager.Instance.ApplyPreferences();

        if (ConfigManager.CheckForJLUpdatesOnStartUp)
        {
            PreferencesWindow.Instance.CheckForJLUpdatesButton!.IsEnabled = false;
            await Networking.CheckForJLUpdates(true);
            PreferencesWindow.Instance.CheckForJLUpdatesButton.IsEnabled = true;
        }
    }

    public static void PlayAudio(byte[] audio, float volume)
    {
        try
        {
            Application.Current!.Dispatcher!.BeginInvoke(() =>
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
            Utils.Logger.Error(e, "Error playing audio: {Audio}", JsonSerializer.Serialize(audio));
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
            Stats.IncrementStat(StatType.Imoutos);
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

            string? variableStr;

            if (variable is double doubleVariable)
                variableStr = doubleVariable.ToString(CultureInfo.InvariantCulture);
            else if (variable is float floatVariable)
                variableStr = floatVariable.ToString(CultureInfo.InvariantCulture);
            else
                variableStr = variable.ToString();

            if (ConfigurationManager.AppSettings.Get(key) == null)
                config.AppSettings.Settings.Add(key, variableStr);
            else
                config.AppSettings.Settings[key].Value = variableStr;

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

        config.AppSettings.Settings[key].Value = rawKeyGesture.StartsWith("Win+")
            ? rawKeyGesture[4..]
            : rawKeyGesture;

        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        if (Application.Current != null)
        {
            Application.Current.Dispatcher!.InvokeAsync(async delegate
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
            new Typeface(ConfigManager.PopupFont.Source!),
            fontSize,
            Brushes.Transparent,
            new NumberSubstitution(),
            Dpi.PixelsPerDip);

        return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
    }

    public static void SetInputGestureText(MenuItem menuItem, KeyGesture keyGesture)
    {
        string keyGestureString = KeyGestureToString(keyGesture);

        menuItem.InputGestureText = keyGestureString != "None"
            ? keyGestureString
            : "";
    }

    public static void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        ColorPicker picker = SingleOpenHelper.CreateControl<ColorPicker>();
        var window = new HandyControl.Controls.PopupWindow { PopupElement = picker, };
        picker.Canceled += delegate { window.Close(); };
        picker.Confirmed += delegate { ColorSetter((Button)sender, picker.SelectedBrush, window); };

        window.ShowDialog(picker, false);
    }

    private static void ColorSetter(Button sender, SolidColorBrush selectedColor,
    HandyControl.Controls.PopupWindow window)
    {
        sender.Background = selectedColor;
        window.Close();
    }

    public static void Unselect(System.Windows.Controls.TextBox? tb)
    {
        if (tb == null)
            return;

        double verticalOffset = tb.VerticalOffset;
        tb.Select(0, 0);
        tb.ScrollToVerticalOffset(verticalOffset);
    }
}
