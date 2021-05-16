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
        public static string AnkiConnectUri;
        public static int MaxSearchLength;
        public static string FrequencyList;
        public static bool UseJMnedict;
        public static bool ForceSync;
        public static SolidColorBrush FoundSpellingColor;
        public static SolidColorBrush ReadingsColor;
        public static SolidColorBrush DefinitionsColor;
        public static SolidColorBrush ProcessColor;
        public static SolidColorBrush FrequencyColor;
        public static SolidColorBrush AlternativeSpellingsColor;
        public static int FoundSpellingFontSize;
        public static int ReadingsFontSize;
        public static int DefinitionsFontSize;
        public static int ProcessFontSize;
        public static int FrequencyFontSize;
        public static int AlternativeSpellingsFontSize;

        private static readonly List<string> japaneseFonts = FindJapaneseFonts().OrderBy(font => font).ToList();
        private static readonly string[] frequencyLists = { "VN", "Novel", "Narou" };

        public static void ApplySettings(MainWindow mainWindow)
        {
            MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength"));
            FrequencyList = ConfigurationManager.AppSettings.Get("FrequencyList");
            AnkiConnectUri = ConfigurationManager.AppSettings.Get("AnkiConnectUri");
            UseJMnedict = bool.Parse(ConfigurationManager.AppSettings.Get("UseJMnedict"));
            ForceSync = bool.Parse(ConfigurationManager.AppSettings.Get("ForceAnkiSync"));

            FoundSpellingColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupPrimarySpellingColor"));
            ReadingsColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupReadingColor"));
            AlternativeSpellingsColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupAlternativeSpellingColor"));
            DefinitionsColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDefinitionColor"));
            FrequencyColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupFrequencyColor"));
            ProcessColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDeconjugationInfoColor"));

            FoundSpellingFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupPrimarySpellingFontSize"));
            ReadingsFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupReadingFontSize"));
            AlternativeSpellingsFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupAlternativeSpellingFontSize"));
            DefinitionsFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupDefinitionFontSize"));
            FrequencyFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupFrequencyFontSize"));
            ProcessFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupDeconjugationInfoFontSize"));

            mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));
            mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize"));
            mainWindow.MainTextBox.FontFamily = new FontFamily(ConfigurationManager.AppSettings.Get("MainWindowFont"));
            mainWindow.MainTextBox.FontSize = mainWindow.FontSizeSlider.Value;
            mainWindow.Background =
                (SolidColorBrush)new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value / 100;
            mainWindow.MainTextBox.Foreground =
                (SolidColorBrush)new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowTextColor"));
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight"));
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth"));
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPosition"));
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition"));

            var popupWindow = PopupWindow.Instance;
            popupWindow.Background = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));
            popupWindow.Background.Opacity = int.Parse(ConfigurationManager.AppSettings.Get("PopupOpacity")) / 100;
        }
        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();

            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.ForceAnkiSyncCheckBox.IsChecked = ForceSync;
            preferenceWindow.UseJMnedictCheckBox.IsChecked = UseJMnedict;
            preferenceWindow.FrequencyListComboBox.ItemsSource = frequencyLists;
            preferenceWindow.FrequencyListComboBox.SelectedItem = FrequencyList;

            preferenceWindow.FontComboBox.ItemsSource = japaneseFonts;
            preferenceWindow.FontComboBox.SelectedItem = mainWindow.MainTextBox.FontFamily.ToString();
            preferenceWindow.TextboxBackgroundColorButton.Background =
                (SolidColorBrush)new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            preferenceWindow.TextboxTextColorButton.Background = mainWindow.MainTextBox.Foreground;
            preferenceWindow.TextboxTextSizeNumericUpDown.Value = (decimal)mainWindow.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = (decimal)mainWindow.OpacitySlider.Value;

            preferenceWindow.PopupAlternativeSpellingColorButton.Background = AlternativeSpellingsColor;
            preferenceWindow.PopupDeconjugationInfoColorButton.Background = ProcessColor;
            preferenceWindow.PopupDefinitionColorButton.Background = DefinitionsColor;
            preferenceWindow.PopupFrequencyColorButton.Background = FrequencyColor;
            preferenceWindow.PopupPrimarySpellingColorButton.Background = FoundSpellingColor;
            preferenceWindow.PopupReadingColorButton.Background = ReadingsColor;
            preferenceWindow.PopupAlternativeSpellingFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
            preferenceWindow.PopupDeconjugationInfoFontSizeNumericUpDown.Value = ProcessFontSize;
            preferenceWindow.PopupDefinitionFontSizeNumericUpDown.Value = DefinitionsFontSize;
            preferenceWindow.PopupFrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
            preferenceWindow.PopupPrimarySpellingSizeNumericUpDown.Value = FoundSpellingFontSize;
            preferenceWindow.PopupReadingFontSizeNumericUpDown.Value = ReadingsFontSize;

            preferenceWindow.PopupBackgroundColorButton.Background = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));
            preferenceWindow.PopupOpacityNumericUpDown.Value = int.Parse(ConfigurationManager.AppSettings.Get("PopupOpacity"));
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

            config.AppSettings.Settings["UseJMnedict"].Value =
                preferenceWindow.UseJMnedictCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["ForceAnkiSync"].Value =
                preferenceWindow.ForceAnkiSyncCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["PopupBackgroundColor"].Value =
                preferenceWindow.PopupBackgroundColorButton.Background.ToString();
            config.AppSettings.Settings["PopupPrimarySpellingColor"].Value =
                preferenceWindow.PopupPrimarySpellingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupReadingColor"].Value =
                preferenceWindow.PopupReadingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupAlternativeSpellingColor"].Value =
                preferenceWindow.PopupAlternativeSpellingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupDefinitionColor"].Value =
                preferenceWindow.PopupDefinitionColorButton.Background.ToString();
            config.AppSettings.Settings["PopupFrequencyColor"].Value =
                preferenceWindow.PopupFrequencyColorButton.Background.ToString();
            config.AppSettings.Settings["PopupDeconjugationInfoColor"].Value =
                preferenceWindow.PopupDeconjugationInfoColorButton.Background.ToString();
            config.AppSettings.Settings["PopupOpacity"].Value =
                preferenceWindow.PopupOpacityNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupPrimarySpellingFontSize"].Value =
                preferenceWindow.PopupPrimarySpellingSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupReadingFontSize"].Value =
                preferenceWindow.PopupReadingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupAlternativeSpellingFontSize"].Value =
                preferenceWindow.PopupAlternativeSpellingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupDefinitionFontSize"].Value =
                preferenceWindow.PopupDefinitionFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupFrequencyFontSize"].Value =
                preferenceWindow.PopupFrequencyFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupDeconjugationInfoFontSize"].Value =
                preferenceWindow.PopupDeconjugationInfoFontSizeNumericUpDown.Value.ToString();

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