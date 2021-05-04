using JapaneseLookup.GUI;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        public static float MainWindowOpacity = float.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));

        public static void SavePosition()
        {
        }

        public static void SaveSettings()
        {

        }

        public static void LoadSettings()
        {

        }


    }
}
