using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomDict;
using JL.Core.Utilities;
using Path = System.IO.Path;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for StatsWindow.xaml
    /// </summary>
    public partial class StatsWindow : Window
    {
        private static StatsWindow s_instance;

        public static StatsWindow Instance
        {
            get
            {
                if (s_instance is not { IsLoaded: true })
                    s_instance = new StatsWindow();

                return s_instance;
            }
        }

        public StatsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatsDisplay();
        }

        public void UpdateStatsDisplay()
        {
            TextBlockCharacters.Text = Storage.SessionStats.Characters.ToString();
            TextBlockLines.Text = Storage.SessionStats.Lines.ToString();
            TextBlockCardsMined.Text = Storage.SessionStats.CardsMined.ToString();
            TextBlockTimesPlayedAudio.Text = Storage.SessionStats.TimesPlayedAudio.ToString();
            TextBlockImoutos.Text = Storage.SessionStats.Imoutos.ToString();
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
