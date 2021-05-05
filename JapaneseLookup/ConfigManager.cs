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

namespace JapaneseLookup
{
    class ConfigManager
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();
        public static int MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength"));
        public static void SaveBeforeClosing(MainWindow mainWindow)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["MainWindowFontSize"].Value = mainWindow.FontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPositon"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();
            config.Save(ConfigurationSaveMode.Modified);
        }
        public static void ApplySettings(MainWindow mainWindow)
        {
            MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength"));
            mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));
            mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize"));
            mainWindow.MainTextBox.FontSize = mainWindow.FontSizeSlider.Value;
            mainWindow.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value;
            mainWindow.MainTextBox.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowTextColor"));
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight"));
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth"));
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPositon"));
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition"));
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static void SavePreferences(PreferencesWindow preferenceWindow)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["MaxSearchLength"].Value = preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowBackgroundColor"].Value = preferenceWindow.TextboxBackgroundColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowTextColor"].Value = preferenceWindow.TextboxTextColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowFontSize"].Value = preferenceWindow.TextboxTextSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = preferenceWindow.TextboxOpacityNumericUpDown.Value.ToString();

            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPositon"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            ApplySettings(mainWindow);
        }
        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.TextboxBackgroundColorButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            preferenceWindow.TextboxTextColorButton.Background = mainWindow.MainTextBox.Foreground;
            preferenceWindow.TextboxTextSizeNumericUpDown.Value = (decimal) mainWindow.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = (decimal) mainWindow.OpacitySlider.Value;
        }
    }
}
