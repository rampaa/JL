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

    private async void SaveButton_Click(object? sender, RoutedEventArgs? e)
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

        string reading = ReadingTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();
        if (reading.Length is 0)
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

            DictType dictType = ComboBoxDictType.SelectedValue.ToString() is "Global"
                ? DictType.CustomNameDictionary
                : DictType.ProfileCustomNameDictionary;

            Dict dict = DictUtils.Dicts.Values.First(dict => dict.Type == dictType);
            if (dict.Active)
            {
                CustomNameLoader.AddToDictionary(spelling, reading, nameType, dict.Contents);
                Utils.Frontend.InvalidateDisplayCache();
            }

            Close();

            string path = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
            string line = string.Create(CultureInfo.InvariantCulture, $"{spelling}\t{reading}\t{nameType}\n");
            await File.AppendAllTextAsync(path, line, Encoding.UTF8).ConfigureAwait(false);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = Activate();
        if (string.IsNullOrEmpty(SpellingTextBox.Text))
        {
            _ = SpellingTextBox.Focus();
        }
        else // if (string.IsNullOrEmpty(ReadingTextBox.Text))
        {
            _ = ReadingTextBox.Focus();
        }
    }

    private void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Enter)
        {
            e.Handled = true;
            SaveButton_Click(null, null);
        }
    }
}
