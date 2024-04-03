using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private AddNameWindow()
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

        if (!JapaneseUtils.JapaneseRegex().IsMatch(SpellingTextBox.Text))
        {
            SpellingTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (SpellingTextBox.BorderBrush == Brushes.Red)
        {
            SpellingTextBox.ClearValue(BorderBrushProperty);
        }

        if (isValid)
        {
            string nameType =
                NameTypeStackPanel.Children.OfType<RadioButton>()
                    .FirstOrDefault(static r => r.IsChecked.HasValue && r.IsChecked.Value)!.Content.ToString()!;

            string spelling = SpellingTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();

            string? reading = ReadingTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();
            if (reading.Length is 0 || reading == spelling)
            {
                reading = null;
            }

            string? extraInfo = ExtraInfoTextBox.Text.Replace("\t", "  ", StringComparison.Ordinal).Trim();
            if (extraInfo.Length is 0)
            {
                extraInfo = null;
            }

            DictType dictType = ComboBoxDictType.SelectedValue.ToString() is "Global"
                ? DictType.CustomNameDictionary
                : DictType.ProfileCustomNameDictionary;

            Dict dict = DictUtils.SingleDictTypeDicts[dictType];
            if (dict.Active)
            {
                CustomNameLoader.AddToDictionary(spelling, reading, nameType, extraInfo, dict.Contents);
            }

            PopupWindowUtils.HidePopups(MainWindow.Instance.FirstPopupWindow);
            Close();

            string path = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
            string line = $"{spelling}\t{reading}\t{nameType}\t{extraInfo}\n";
            await File.AppendAllTextAsync(path, line).ConfigureAwait(false);
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _ = Activate();
        if (string.IsNullOrEmpty(SpellingTextBox.Text))
        {
            _ = SpellingTextBox.Focus();
        }
        else if (string.IsNullOrEmpty(ReadingTextBox.Text))
        {
            _ = ReadingTextBox.Focus();
        }
        else // if (string.IsNullOrEmpty(ExtraInfoTextBox.Text))
        {
            _ = ExtraInfoTextBox.Focus();
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter && InputMethod.Current?.ImeState is not InputMethodState.On)
        {
            e.Handled = true;
            SaveButton_Click(null, null);
        }
    }
}
