using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddNameWindow.xaml
/// </summary>
internal sealed partial class AddNameWindow : Window
{
    private static AddNameWindow? s_instance;

    public static AddNameWindow Instance => s_instance ??= new AddNameWindow();

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

        if (!Storage.JapaneseRegex.IsMatch(SpellingTextBox.Text))
        {
            SpellingTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingTextBox.BorderBrush == Brushes.Red)
        {
            SpellingTextBox.BorderBrush = WindowsUtils.BrushFromHex("#FF3F3F46")!;
        }

        if (ReadingTextBox.Text is "")
        {
            ReadingTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ReadingTextBox.BorderBrush == Brushes.Red)
        {
            ReadingTextBox.BorderBrush = WindowsUtils.BrushFromHex("#FF3F3F46")!;
        }

        if (isValid)
        {
            string nameType =
                NameTypeStackPanel.Children.OfType<RadioButton>()
                    .FirstOrDefault(static r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;
            string spelling = SpellingTextBox.Text.Replace("\t", "  ").Trim();
            string reading = ReadingTextBox.Text.Replace("\t", "  ").Trim();
            CustomNameLoader.AddToDictionary(spelling, reading, nameType);
            Storage.Frontend.InvalidateDisplayCache();
            Close();
            await WriteToFile(spelling, reading, nameType).ConfigureAwait(false);
        }
    }

    private static async Task WriteToFile(string spelling, string reading, string type)
    {
        StringBuilder stringBuilder = new();
        _ = stringBuilder.Append(spelling)
            .Append('\t')
            .Append(reading)
            .Append('\t')
            .Append(type)
            .Append(Environment.NewLine);

        string customNameDictPath = Storage.Dicts.Values.First(static dict => dict.Type is DictType.CustomNameDictionary).Path;
        await File.AppendAllTextAsync(customNameDictPath,
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
}
