using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;

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
            TextboxOpacityNumericUpDown.Increment = 0.05m;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.SavePreferences(this);
            Visibility = Visibility.Collapsed;
        }

        private void TextboxBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextboxBackgroundColorButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        private void TextboxTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextboxTextColorButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
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
    }
}
