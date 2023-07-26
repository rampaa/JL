using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddNameWindow.xaml
/// </summary>
internal sealed partial class AddNameWindow : Window
{
    private static AddNameWindow? s_instance;

    public static AddNameWindow Instance => s_instance ??= new AddNameWindow();

    private static readonly Dictionary<string, List<IDictRecord>> s_customNameDictionary = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.CustomNameDictionary).Contents;

    public AddNameWindow()
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

        if (!JapaneseUtils.JapaneseRegex.IsMatch(SpellingTextBox.Text))
        {
            SpellingTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingTextBox.BorderBrush == Brushes.Red)
        {
            SpellingTextBox.ClearValue(BorderBrushProperty);
        }

        if (ReadingTextBox.Text is "")
        {
            ReadingTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ReadingTextBox.BorderBrush == Brushes.Red)
        {
            ReadingTextBox.ClearValue(BorderBrushProperty);
        }

        if (isValid)
        {
            string nameType =
                NameTypeStackPanel.Children.OfType<RadioButton>()
                    .FirstOrDefault(static r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;
            string spelling = SpellingTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();
            string reading = ReadingTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();
            CustomNameLoader.AddToDictionary(spelling, reading, nameType, s_customNameDictionary);
            Utils.Frontend.InvalidateDisplayCache();
            Close();
            await WriteToFile(spelling, reading, nameType).ConfigureAwait(false);
        }
    }

    private static async Task WriteToFile(string spelling, string reading, string type)
    {
        string line = string.Create(CultureInfo.InvariantCulture, $"{spelling}\t{reading}\t{type}\n");
        string customNameDictPath = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.CustomNameDictionary).Path;
        await File.AppendAllTextAsync(customNameDictPath, line, Encoding.UTF8).ConfigureAwait(false);
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
}
