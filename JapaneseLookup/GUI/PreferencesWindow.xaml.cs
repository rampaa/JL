using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;
using System.Diagnostics;
using JapaneseLookup.EDICT;
using System.Threading.Tasks;
using HandyControl.Tools;
using HandyControl.Controls;
using HandyControl.Properties;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PreferenceWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : System.Windows.Window
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
        private void ShowColowPicker(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            var window = new HandyControl.Controls.PopupWindow
            {
                PopupElement = picker,
            };
            picker.Canceled += delegate { window.Close(); };
            picker.Confirmed += delegate { ColorSetter((System.Windows.Controls.Button)sender, picker.SelectedBrush, window); };

            window.ShowDialog(picker, false);
        }

        private static void ColorSetter(System.Windows.Controls.Button sender, SolidColorBrush selectedColor, HandyControl.Controls.PopupWindow window)
        {
            sender.Background = selectedColor;
            window.Close();
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
