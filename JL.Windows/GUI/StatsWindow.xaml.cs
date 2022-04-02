using System.Windows;
using System.Windows.Controls;
using JL.Core;

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
