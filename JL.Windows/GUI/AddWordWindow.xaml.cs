using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomWordDict;
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

        if (!Storage.JapaneseRegex.IsMatch(SpellingsTextBox.Text))
        {
            SpellingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingsTextBox.BorderBrush == Brushes.Red)
        {
            SpellingsTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        if (ReadingsTextBox.Text is "")
        {
            ReadingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ReadingsTextBox.BorderBrush == Brushes.Red)
        {
            ReadingsTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        if (DefinitionsTextBox.Text is "")
        {
            DefinitionsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (DefinitionsTextBox.BorderBrush == Brushes.Red)
        {
            DefinitionsTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        if (isValid)
        {
            string rawSpellings = SpellingsTextBox.Text.Replace("\t", "  ");
            string rawReadings = ReadingsTextBox.Text.Replace("\t", "  ");
            string rawDefinitions = DefinitionsTextBox.Text.Replace("\t", "  ");
            string rawPartOfSpeech = PartOfSpeechStackPanel.Children.OfType<RadioButton>()
                .FirstOrDefault(static r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;
            string rawWordClasses = WordClassTextBox.Text.Replace("\t", "  ");


            string[] spellings = rawSpellings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static s => s.Trim()).ToArray();
            List<string> readings = rawReadings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static r => r.Trim()).ToList();
            List<string> definitions = rawDefinitions.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static s => s.Trim()).ToList();
            List<string>? wordClasses = string.IsNullOrWhiteSpace(rawWordClasses)
                ? null
                : rawWordClasses.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(static wc => wc.Trim()).ToList();

            CustomWordLoader.AddToDictionary(spellings, readings, definitions, rawPartOfSpeech, wordClasses);
            Storage.Frontend.InvalidateDisplayCache();

            Close();

            await WriteToFile(rawSpellings, rawReadings, rawDefinitions, rawPartOfSpeech, rawWordClasses).ConfigureAwait(false);
        }
    }

    private static async Task WriteToFile(string spellings, string readings, string definitions, string partOfSpeech, string wordClasses)
    {
        StringBuilder stringBuilder = new();
        stringBuilder = stringBuilder.Append(spellings)
            .Append('\t')
            .Append(readings)
            .Append('\t')
            .Append(definitions)
            .Append('\t')
            .Append(partOfSpeech);

        if (!string.IsNullOrWhiteSpace(wordClasses))
        {
            stringBuilder = stringBuilder.Append('\t')
                .Append(wordClasses);
        }

        _ = stringBuilder.Append(Environment.NewLine);

        string customWordDictPath = Storage.Dicts.Values.First(static dict => dict.Type is DictType.CustomWordDictionary).Path;
        await File.AppendAllTextAsync(customWordDictPath,
            stringBuilder.ToString(), Encoding.UTF8).ConfigureAwait(false);
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
        WordClassStackPasnel.Visibility = Visibility.Visible;
    }

    private void OtherRadioButtons_Checked(object sender, RoutedEventArgs e)
    {
        WordClassTextBox.Text = "";
        WordClassStackPasnel.Visibility = Visibility.Collapsed;
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
