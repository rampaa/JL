using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomDict;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for AddWordWindow.xaml
    /// </summary>
    public partial class AddWordWindow : Window
    {
        private static AddWordWindow? s_instance;

        public static AddWordWindow Instance
        {
            get
            {
                if (s_instance == null || !s_instance.IsLoaded)
                    s_instance = new();

                return s_instance;
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;

            if (!Storage.JapaneseRegex.IsMatch(SpellingsTextBox!.Text))
            {
                SpellingsTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (SpellingsTextBox.BorderBrush == Brushes.Red)
            {
                SpellingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
            }

            if (ReadingsTextBox!.Text == "")
            {
                ReadingsTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (ReadingsTextBox.BorderBrush == Brushes.Red)
            {
                ReadingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
            }

            if (DefinitionsTextBox!.Text == "")
            {
                DefinitionsTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (DefinitionsTextBox.BorderBrush == Brushes.Red)
            {
                DefinitionsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
            }

            if (isValid)
            {
                string rawSpellings = SpellingsTextBox.Text;
                string rawReadings = ReadingsTextBox.Text;
                string rawDefinitions = DefinitionsTextBox.Text;
                string rawWordClass = WordClassStackPanel!.Children.OfType<RadioButton>()
                    .FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;

                string[] spellings = rawSpellings.Split(';').Select(s => s.Trim()).ToArray();
                List<string> readings = rawReadings.Split(';').Select(r => r.Trim()).ToList();
                List<string> definitions = rawDefinitions.Split(';').Select(s => s.Trim()).ToList();

                CustomWordLoader.AddToDictionary(spellings, readings, definitions, rawWordClass);

                Close();

                await WriteToFile(rawSpellings, rawReadings, rawDefinitions, rawWordClass).ConfigureAwait(false);
            }
        }

        private static async Task WriteToFile(string spellings, string readings, string definitions, string wordClass)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(spellings);
            stringBuilder.Append('\t');
            stringBuilder.Append(readings);
            stringBuilder.Append('\t');
            stringBuilder.Append(definitions);
            stringBuilder.Append('\t');
            stringBuilder.Append(wordClass);
            stringBuilder.Append(Environment.NewLine);

            string customWordDictPath = Storage.Dicts[DictType.CustomWordDictionary].Path;
            await File.AppendAllTextAsync(customWordDictPath,
                stringBuilder.ToString(), Encoding.UTF8).ConfigureAwait(false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OtherRadioButton!.IsChecked = true;
        }
    }
}
