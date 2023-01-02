﻿using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using JL.Core;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Windows.GUI;
using NAudio.Wave;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Window = System.Windows.Window;

namespace JL.Windows.Utilities;

public static class WindowsUtils
{
    private static WaveOut? s_audioPlayer;

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

    public static bool CompareKeyGesture(KeyEventArgs e, KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers is ModifierKeys.Windows)
            return keyGesture.Key == e.Key && (Keyboard.Modifiers & ModifierKeys.Windows) is 0;
        else if (keyGesture.Modifiers is 0)
            return keyGesture.Key == e.Key;
        else
            return keyGesture.Matches(null, e);
    }

    public static bool CompareKeyGesture(KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers is ModifierKeys.Windows)
            return Keyboard.IsKeyDown(keyGesture.Key) && (Keyboard.Modifiers & ModifierKeys.Windows) is 0;
        else if (keyGesture.Modifiers is 0)
            return Keyboard.IsKeyDown(keyGesture.Key);
        else
            return Keyboard.IsKeyDown(keyGesture.Key) && Keyboard.Modifiers == keyGesture.Modifiers;
    }

    public static string KeyGestureToString(KeyGesture keyGesture)
    {
        StringBuilder keyGestureStringBuilder = new();

        if (keyGesture.Key is Key.LeftShift or Key.RightShift
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt)
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

            if (keyGesture.Key is not Key.None)
            {
                keyGestureStringBuilder.Append(keyGesture.Key.ToString());
            }
        }

        return keyGestureStringBuilder.ToString();
    }

    public static KeyGesture SetKeyGesture(string keyGestureName, KeyGesture keyGesture)
    {
        string? rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

        if (rawKeyGesture is not null)
        {
            KeyGestureConverter keyGestureConverter = new();

            if (rawKeyGesture.Contains("Ctrl") || rawKeyGesture.Contains("Alt") || rawKeyGesture.Contains("Shift"))
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
        addNameWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Storage.StatsStopWatch.Stop();
        addNameWindowInstance.ShowDialog();
    }

    public static void ShowAddWordWindow(string? selectedText)
    {
        AddWordWindow addWordWindowInstance = AddWordWindow.Instance;
        addWordWindowInstance.SpellingsTextBox!.Text = selectedText;
        addWordWindowInstance.Owner = MainWindow.Instance;
        addWordWindowInstance.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Storage.StatsStopWatch.Stop();
        addWordWindowInstance.ShowDialog();
    }

    public static void ShowPreferencesWindow()
    {
        PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
        ConfigManager.Instance.LoadPreferences(preferencesWindow);
        preferencesWindow.Owner = MainWindow.Instance;
        preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Storage.StatsStopWatch.Stop();
        preferencesWindow.ShowDialog();
    }

    public static void ShowManageDictionariesWindow()
    {
        if (!File.Exists(Path.Join(Storage.ConfigPath, "dicts.json")))
            Utils.CreateDefaultDictsConfig();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_words.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_words.txt").Dispose();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_names.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_names.txt").Dispose();

        ManageDictionariesWindow manageDictionariesWindow = ManageDictionariesWindow.Instance;
        manageDictionariesWindow.Owner = MainWindow.Instance;
        manageDictionariesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Storage.StatsStopWatch.Stop();
        manageDictionariesWindow.ShowDialog();
    }

    public static void ShowManageFrequenciesWindow()
    {
        if (!File.Exists(Path.Join(Storage.ConfigPath, "freqs.json")))
            Utils.CreateDefaultFreqsConfig();

        ManageFrequenciesWindow manageFrequenciesWindow = ManageFrequenciesWindow.Instance;
        manageFrequenciesWindow.Owner = MainWindow.Instance;
        manageFrequenciesWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Storage.StatsStopWatch.Stop();
        manageFrequenciesWindow.ShowDialog();
    }

    public static void ShowStatsWindow()
    {
        Stats.IncrementStat(StatType.Time, Storage.StatsStopWatch.ElapsedTicks);
        Storage.StatsStopWatch.Reset();

        StatsWindow statsWindow = StatsWindow.Instance;
        statsWindow.Owner = MainWindow.Instance;
        statsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        statsWindow.ShowDialog();
    }

    public static void SearchWithBrowser(string? selectedText)
    {
        if (selectedText?.Length > 0)
        {
            Process.Start(new ProcessStartInfo("cmd",
                $"/c start \"\" \"{ConfigManager.SearchUrl.Replace("{SearchTerm}", System.Web.HttpUtility.UrlEncode(selectedText))}\"")
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
            await using (downloadResponseStream.ConfigureAwait(false))
            {
                using ZipArchive archive = new(downloadResponseStream);

                string tmpDirectory = Path.Join(Storage.ApplicationPath, "tmp");

                if (Directory.Exists(tmpDirectory))
                {
                    Directory.Delete(tmpDirectory, true);
                }

                Directory.CreateDirectory(tmpDirectory);
                archive.ExtractToDirectory(tmpDirectory);
            }

            await MainWindow.Instance.Dispatcher!.BeginInvoke(ConfigManager.SaveBeforeClosing);

            Process.Start(
                new ProcessStartInfo("cmd",
                $"/c start \"JL Updater\" \"{Path.Join(Storage.ApplicationPath, "update-helper.cmd")}\"")
                { UseShellExecute = true, Verb = "runas" });
        }

        else
        {
            Utils.Logger.Error("Couldn't update JL. {StatusCode} {ReasonPhrase}", downloadResponse.StatusCode, downloadResponse.ReasonPhrase);
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't update JL");
        }
    }

    public static async Task InitializeMainWindow()
    {
        Storage.Frontend = MainWindow.Instance;

        await Utils.CoreInitialize();

        if (ConfigManager.CheckForJLUpdatesOnStartUp)
        {
            PreferencesWindow preferencesWindow = PreferencesWindow.Instance;
            preferencesWindow.CheckForJLUpdatesButton!.IsEnabled = false;
            await Networking.CheckForJLUpdates(true);
            preferencesWindow.CheckForJLUpdatesButton.IsEnabled = true;
        }
    }

    public static void PlayAudio(byte[] audio, float volume)
    {
        try
        {
            Application.Current!.Dispatcher!.BeginInvoke(() =>
            {
                try
                {
                    s_audioPlayer?.Dispose();

                    s_audioPlayer = new WaveOut { Volume = volume };

                    s_audioPlayer.Init(new Mp3FileReader(new MemoryStream(audio)));
                    s_audioPlayer.Play();
                }
                catch (Exception ex)
                {
                    Utils.Logger.Error(ex, "Error playing audio: {Audio}", JsonSerializer.Serialize(audio));
                    Alert(AlertLevel.Error, "Error playing audio");
                }
            });
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error playing audio: {Audio}", JsonSerializer.Serialize(audio));
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

            if (numFiles is 0)
            {
                Utils.Logger.Warning("Motivation folder is empty!");
                Alert(AlertLevel.Warning, "Motivation folder is empty!");
                return;
            }

            string randomFilePath = filePaths[rand.Next(numFiles)];
            byte[] randomFile = File.ReadAllBytes(randomFilePath);
            PlayAudio(randomFile, 1);
            Stats.IncrementStat(StatType.Imoutos);
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error motivating");
            Alert(AlertLevel.Error, "Error motivating");
        }
    }

    public static Brush? BrushFromHex(string hexColorString)
    {
        return (Brush?)new BrushConverter().ConvertFrom(hexColorString);
    }

    public static void Alert(AlertLevel alertLevel, string message)
    {
        Application.Current?.Dispatcher.InvokeAsync(async () =>
        {
            List<AlertWindow> alertWindowList = Application.Current.Windows.OfType<AlertWindow>().ToList();

            AlertWindow alertWindow = new();

            alertWindow.Left = DpiAwareWorkAreaWidth - alertWindow.Width - 30;
            alertWindow.Top =
                alertWindowList.Count * ((alertWindowList.LastOrDefault()?.ActualHeight ?? 0) + 2) + 30;

            alertWindow.SetAlert(alertLevel, message);
            alertWindow.Show();
            await Task.Delay(4004);
            alertWindow.Close();
        });
    }

    public static Size MeasureTextSize(string text, int fontSize)
    {
        FormattedText formattedText = new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
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

        menuItem.InputGestureText = keyGestureString is not "None"
            ? keyGestureString
            : "";
    }

    public static void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        ColorPicker picker = SingleOpenHelper.CreateControl<ColorPicker>();
        var window = new HandyControl.Controls.PopupWindow { PopupElement = picker, };
        picker.SelectedBrush = (SolidColorBrush)((Button)sender).Tag;
        picker.Canceled += delegate { window.Close(); };
        picker.Confirmed += delegate { ConfirmColor((Button)sender, picker.SelectedBrush, window); };

        window.ShowDialog(picker, false);
    }

    private static void ConfirmColor(Button button, Brush selectedBrush,
    HandyControl.Controls.PopupWindow window)
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
            return;

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

        else if (dynamicWidth)
        {
            window.SizeToContent = SizeToContent.Width;
            window.Height = maxHeight;
        }

        else if (dynamicHeight)
        {
            window.SizeToContent = SizeToContent.Height;
            window.Width = maxWidth;
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

        else if (dynamicWidth)
        {
            window.MaxHeight = double.PositiveInfinity;
            window.MaxWidth = maxWidth;
            window.SizeToContent = SizeToContent.Width;
            window.Height = height;
        }

        else if (dynamicHeight)
        {
            window.MaxHeight = maxHeight;
            window.MaxWidth = double.PositiveInfinity;
            window.SizeToContent = SizeToContent.Height;
            window.Width = width;
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
                Clipboard.SetText(text);
                retry = false;
            }
            catch (Exception ex)
            {
                Utils.Logger.Warning(ex, "CopyTextToClipboard failed");
            }
        }
        while (retry);
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

        Storage.StatsStopWatch.Start();
    }

    public static void ChangeTheme(SkinType skin)
    {
        ResourceDictionary resources = Application.Current.Resources;

        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ResourceDictionary.xaml", UriKind.Relative) });
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/HandyControl;component/Themes/Skin{skin}.xaml")
        });
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
        });
    }
}
