using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddWordWindow.xaml
/// </summary>
internal sealed partial class AddWordWindow : Window
{
    private static AddWordWindow? s_instance;

    public static AddWordWindow Instance => s_instance ??= new AddWordWindow();

    public AddWordWindow()
    {
        InitializeComponent();
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        if (!JapaneseUtils.JapaneseRegex.IsMatch(SpellingsTextBox.Text))
        {
            SpellingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingsTextBox.BorderBrush == Brushes.Red)
        {
            SpellingsTextBox.ClearValue(BorderBrushProperty);
        }

        if (ReadingsTextBox.Text is "")
        {
            ReadingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ReadingsTextBox.BorderBrush == Brushes.Red)
        {
            ReadingsTextBox.ClearValue(BorderBrushProperty);
        }

        if (DefinitionsTextBox.Text is "")
        {
            DefinitionsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (DefinitionsTextBox.BorderBrush == Brushes.Red)
        {
            DefinitionsTextBox.ClearValue(BorderBrushProperty);
        }

        if (isValid)
        {
            string rawSpellings = SpellingsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
            string rawReadings = ReadingsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
            string rawDefinitions = DefinitionsTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);
            string rawPartOfSpeech = PartOfSpeechStackPanel.Children.OfType<RadioButton>()
                .FirstOrDefault(static r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;
            string rawWordClasses = WordClassTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal);


            string[] spellings = rawSpellings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static s => s.Trim()).ToArray();
            List<string> readings = rawReadings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static r => r.Trim()).ToList();
            List<string> definitions = rawDefinitions.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static s => s.Trim()).ToList();
            List<string>? wordClasses = string.IsNullOrWhiteSpace(rawWordClasses)
                ? null
                : rawWordClasses.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static wc => wc.Trim()).ToList();

            CustomWordLoader.AddToDictionary(spellings, readings, definitions, rawPartOfSpeech, wordClasses);
            Utils.Frontend.InvalidateDisplayCache();

            Close();

            await WriteToFile(rawSpellings, rawReadings, rawDefinitions, rawPartOfSpeech, rawWordClasses).ConfigureAwait(false);
        }
    }

    private static async Task WriteToFile(string spellings, string readings, string definitions, string partOfSpeech, string wordClasses)
    {
        string line = string.IsNullOrWhiteSpace(wordClasses)
            ? string.Create(CultureInfo.InvariantCulture, $"{spellings}\t{readings}\t{definitions}\t{partOfSpeech}\n")
            : string.Create(CultureInfo.InvariantCulture, $"{spellings}\t{readings}\t{definitions}\t{partOfSpeech}\t{wordClasses}\n");

        string customWordDictPath = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.CustomWordDictionary).Path;
        await File.AppendAllTextAsync(customWordDictPath, line, Encoding.UTF8).ConfigureAwait(false);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        OtherRadioButton.IsChecked = true;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
    }

    private void VerbRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        WordClassStackPanel.Visibility = Visibility.Visible;
    }

    private void OtherRadioButtons_Checked(object sender, RoutedEventArgs e)
    {
        WordClassTextBox.Text = "";
        WordClassStackPanel.Visibility = Visibility.Collapsed;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        const string supportedWordClasses = @"v1: Ichidan verb
v1-s: Ichidan verb - kureru special class
v4r: Yodan verb with `ru' ending (archaic)
v5aru: Godan verb - -aru special class
v5b: Godan verb with 'bu' ending
v5g: Godan verb with 'gu' ending
v5k: Godan verb with 'ku' ending
v5k-s: Godan verb - Iku/Yuku special class
v5m: Godan verb with 'mu' ending
v5n: Godan verb with 'nu' ending
v5r: Godan verb with 'ru' ending
v5r-i: Godan verb with 'ru' ending (irregular verb)
v5s: Godan verb with 'su' ending
v5t: Godan verb with 'tsu' ending
v5u: Godan verb with 'u' ending
v5u-s: Godan verb with 'u' ending (special class)
vk: Kuru verb - special class
vs-c: su verb - precursor to the modern suru (limited support)
vs-i: suru verb - included
vs-s: suru verb - special class
vz: Ichidan verb - zuru verb (alternative form of -jiru verbs)";

        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = "Supported Word Classes",
            InfoTextBox = { Text = supportedWordClasses },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _ = infoWindow.ShowDialog();
    }
}
