using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for AddWordWindow.xaml
    /// </summary>
    public partial class AddWordWindow : Window
    {
        private static AddWordWindow _instance;
        public static AddWordWindow Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                    _instance = new AddWordWindow();

                return _instance;
            }
        }

        public AddWordWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValidated = true;

            if (!MainWindowUtilities.JapaneseRegex.IsMatch(SpellingsTextBox.Text))
            {
                SpellingsTextBox.BorderBrush = Brushes.Red;
                isValidated = false;
            }

            else if (SpellingsTextBox.BorderBrush == Brushes.Red)
            {
                SpellingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46");
            }

            if (ReadingsTextBox.Text == "")
            {
                ReadingsTextBox.BorderBrush = Brushes.Red;
                isValidated = false;
            }

            else if (ReadingsTextBox.BorderBrush == Brushes.Red)
            {
                ReadingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46");
            }

            if (DefinitionsTextBox.Text == "")
            {
                DefinitionsTextBox.BorderBrush = Brushes.Red;
                isValidated = false;
            }

            else if (DefinitionsTextBox.BorderBrush == Brushes.Red)
            {
                DefinitionsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46");
            }

            if (isValidated)
            {
                WriteToFile(SpellingsTextBox.Text, ReadingsTextBox.Text, DefinitionsTextBox.Text);
                Close();
            }
        }

        private static void WriteToFile(string spellings, string readings, string definitions)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(spellings);
            stringBuilder.Append('\t');
            stringBuilder.Append(readings);
            stringBuilder.Append('\t');
            stringBuilder.Append(definitions);
            stringBuilder.Append(Environment.NewLine);

            File.AppendAllTextAsync(
                Path.Join(ConfigManager.ApplicationPath, "Resources/custom_words.txt"),
                stringBuilder.ToString(), Encoding.UTF8);
        }
    }
}
