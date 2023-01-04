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
public partial class AddWordWindow : Window
{
    private static AddWordWindow? s_instance;

    public static AddWordWindow Instance => s_instance ??= new();

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

        if (!Storage.JapaneseRegex.IsMatch(SpellingsTextBox!.Text))
        {
            SpellingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingsTextBox.BorderBrush == Brushes.Red)
        {
            SpellingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        if (ReadingsTextBox!.Text is "")
        {
            ReadingsTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ReadingsTextBox.BorderBrush == Brushes.Red)
        {
            ReadingsTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        if (DefinitionsTextBox!.Text is "")
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
            string rawSpellings = SpellingsTextBox.Text.Replace("\t", "  ");
            string rawReadings = ReadingsTextBox.Text.Replace("\t", "  ");
            string rawDefinitions = DefinitionsTextBox.Text.Replace("\t", "  ");
            string rawWordClass = WordClassStackPanel!.Children.OfType<RadioButton>()
                .FirstOrDefault(r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;

            string[] spellings = rawSpellings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            List<string> readings = rawReadings.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
            List<string> definitions = rawDefinitions.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            CustomWordLoader.AddToDictionary(spellings, readings, definitions, rawWordClass);
            Storage.Frontend.InvalidateDisplayCache();

            Close();

            await WriteToFile(rawSpellings, rawReadings, rawDefinitions, rawWordClass).ConfigureAwait(false);
        }
    }

    private static async Task WriteToFile(string spellings, string readings, string definitions, string wordClass)
    {
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(spellings)
            .Append('\t')
            .Append(readings)
            .Append('\t')
            .Append(definitions)
            .Append('\t')
            .Append(wordClass)
            .Append(Environment.NewLine);

        string customWordDictPath = Storage.Dicts.Values.First(dict => dict.Type is DictType.CustomWordDictionary).Path;
        await File.AppendAllTextAsync(customWordDictPath,
            stringBuilder.ToString(), Encoding.UTF8).ConfigureAwait(false);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        OtherRadioButton!.IsChecked = true;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
    }
}
