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
        }
        private static void PickColor(System.Windows.Controls.Button sender)
        {
            ColorDialog colorDialog = new();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sender.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.SavePreferences(this);
            Visibility = Visibility.Collapsed;
        }

        private void TextboxBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void TextboxTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
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

        private void PopupBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupPrimarySpellingColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupReadingColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupAlternativeSpellingColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupFrequencyColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupDeconjugationInfoColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }

        private void PopupDefinitionColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickColor((System.Windows.Controls.Button)sender);
        }
    }
}
