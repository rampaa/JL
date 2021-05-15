using JapaneseLookup.GUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;

namespace JapaneseLookup
{
    internal static class ConfigManager
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();
        private static readonly List<string> japaneseFonts = FindJapaneseFonts().OrderBy(font => font).ToList();
        private static readonly string[] frequencyLists = { "VN", "Novel", "Narou" };
        public static string AnkiConnectUri;
        public static int MaxSearchLength;
        public static string FrequencyList;
        public static bool ForceSync;
        public static bool UseJMnedict;

        public static SolidColorBrush PopupBackgroundColor;
        public static int PopUpBackGroundOpacity;

        public static Brush FoundSpellingColor = Brushes.Chocolate;
        public static Brush ReadingsColor = Brushes.Goldenrod;
        public static Brush DefinitionsColor = Brushes.White;
        public static Brush ProcessColor = Brushes.White;
        public static Brush FrequencyColor = Brushes.White;
        public static Brush AlternativeSpellingsColor = Brushes.White;
        public static int FoundSpellingFontSize = 19;
        public static int ReadingsFontSize = 17;
        public static int DefinitionsFontSize = 15;
        public static int ProcessFontSize = 15;
        public static int FrequencyFontSize = 15;
        public static int AlternativeSpellingsFontSize = 15;

        public static void ApplySettings(MainWindow mainWindow)
        {
            MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength"));
            FrequencyList = ConfigurationManager.AppSettings.Get("FrequencyList");
            AnkiConnectUri = ConfigurationManager.AppSettings.Get("AnkiConnectUri");

            mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));
            mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize"));
            mainWindow.MainTextBox.FontFamily = new FontFamily(ConfigurationManager.AppSettings.Get("MainWindowFont"));
            mainWindow.MainTextBox.FontSize = mainWindow.FontSizeSlider.Value;
            mainWindow.Background =
                (SolidColorBrush) new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value/100;
            mainWindow.MainTextBox.Foreground =
                (SolidColorBrush) new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowTextColor"));
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight"));
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth"));
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPosition"));
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition"));
            
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.TextboxBackgroundColorButton.Background =
                (SolidColorBrush)new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            preferenceWindow.TextboxTextColorButton.Background = mainWindow.MainTextBox.Foreground;
            preferenceWindow.TextboxTextSizeNumericUpDown.Value = (decimal)mainWindow.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = (decimal)mainWindow.OpacitySlider.Value;
            preferenceWindow.FontComboBox.ItemsSource = japaneseFonts;
            preferenceWindow.FontComboBox.SelectedItem = mainWindow.MainTextBox.FontFamily.ToString();
            preferenceWindow.FrequencyListComboBox.ItemsSource = frequencyLists;
            preferenceWindow.FrequencyListComboBox.SelectedItem = FrequencyList;
        }
        public static void SavePreferences(PreferencesWindow preferenceWindow)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["MaxSearchLength"].Value =
                preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["AnkiConnectUri"].Value =
                preferenceWindow.AnkiUriTextBox.Text;
            config.AppSettings.Settings["MainWindowBackgroundColor"].Value =
                preferenceWindow.TextboxBackgroundColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowTextColor"].Value =
                preferenceWindow.TextboxTextColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowFontSize"].Value =
                preferenceWindow.TextboxTextSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value =
                preferenceWindow.TextboxOpacityNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowFont"].Value =
                preferenceWindow.FontComboBox.SelectedItem.ToString();
            config.AppSettings.Settings["FrequencyList"].Value =
                preferenceWindow.FrequencyListComboBox.SelectedItem.ToString();

            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            ApplySettings(mainWindow);
        }
        public static void SaveBeforeClosing(MainWindow mainWindow)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["MainWindowFontSize"].Value = mainWindow.FontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();

            config.Save(ConfigurationSaveMode.Modified);
        }
        private static List<string> FindJapaneseFonts()
        {
            List<string> japaneseFonts = new();
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                if (fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
                    japaneseFonts.Add(fontFamily.Source);

                else if (fontFamily.FamilyNames.Keys.Count == 1 && fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("en-US")))
                {
                    foreach (var typeFace in fontFamily.GetTypefaces())
                    {
                        if (typeFace.TryGetGlyphTypeface(out var glyphTypeFace))
                        {
                            if (glyphTypeFace.CharacterToGlyphMap.ContainsKey(20685))
                            {
                                japaneseFonts.Add(fontFamily.Source);
                                break;
                            }
                        }
                    }
                }
            }
            return japaneseFonts;
        }
    }
}