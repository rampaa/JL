using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;
using JapaneseLookup.EDICT;
using System.Threading.Tasks;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PreferenceWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        private static PreferencesWindow _instance;

        public static PreferencesWindow Instance
        {
            get { return _instance ??= new PreferencesWindow(); }
        }

        public PreferencesWindow()
        {
            InitializeComponent();
        }
        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ((System.Windows.Controls.Button)sender).Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.SavePreferences(this);
            Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Collapsed;
        }
        private void UpdateJMdictButton_Click(object sender, RoutedEventArgs e)
        {
            EdictUpdater.UpdateJMdict();
        }

        private void UpdateJMnedictButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(EdictUpdater.UpdateJMnedict);
        }

        private void AnkiConfigButton_Click(object sender, RoutedEventArgs e)
        {
            MiningSetupWindow.Instance.ShowDialog();
        }
    }
}
