using System;
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

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for AddNameWindow.xaml
    /// </summary>
    public partial class AddNameWindow : Window
    {
        private static AddNameWindow? s_instance;

        public static AddNameWindow Instance
        {
            get
            {
                if (s_instance == null || !s_instance.IsLoaded)
                    s_instance = new();

                return s_instance;
            }
        }

        public AddNameWindow()
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

            if (!Storage.JapaneseRegex.IsMatch(SpellingTextBox.Text))
            {
                SpellingTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (SpellingTextBox.BorderBrush == Brushes.Red)
            {
                SpellingTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
            }

            if (ReadingTextBox.Text == "")
            {
                ReadingTextBox.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (ReadingTextBox.BorderBrush == Brushes.Red)
            {
                ReadingTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
            }

            if (isValid)
            {
                string nameType =
                    NameTypeStackPanel.Children.OfType<RadioButton>()
                        .FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;
                string spelling = SpellingTextBox.Text;
                string reading = ReadingTextBox.Text;
                CustomNameLoader.AddToDictionary(spelling.Trim(), reading.Trim(), nameType!.Trim());
                Close();
                await WriteToFile(spelling, reading, nameType).ConfigureAwait(false);
            }
        }

        private static async Task WriteToFile(string spelling, string reading, string type)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append(spelling);
            stringBuilder.Append('\t');
            stringBuilder.Append(reading);
            stringBuilder.Append('\t');
            stringBuilder.Append(type);
            stringBuilder.Append(Environment.NewLine);

            string customNameDictPath = Storage.Dicts[DictType.CustomNameDictionary].Path;
            await File.AppendAllTextAsync(customNameDictPath,
                stringBuilder.ToString(), Encoding.UTF8).ConfigureAwait(false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OtherRadioButton.IsChecked = true;
        }
    }
}
