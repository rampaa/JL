using JapaneseLookup.GUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            config.AppSettings.Settings["MainWindowFontSize"].Value = mainWindow.fontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.opacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPositon"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();
            config.Save(ConfigurationSaveMode.Modified);
        }
        public static void LoadSettings(MainWindow mainWindow)
        {
            mainWindow.fontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize"));
            mainWindow.opacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight"));
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth"));
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPositon"));
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition"));
            Debug.WriteLine("S");
            Debug.WriteLine(mainWindow.Height);
            Debug.WriteLine(mainWindow.Width);
            Debug.WriteLine(mainWindow.Top);
            Debug.WriteLine(mainWindow.Left);
            Debug.WriteLine("B");
        }
    }
}
