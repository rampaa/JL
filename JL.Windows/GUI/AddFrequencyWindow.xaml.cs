using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core;
using JL.Core.Frequency;
using JL.Core.Utilities;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddFrequencyWindow.xaml
/// </summary>
public partial class AddFrequencyWindow : Window
{
    public AddFrequencyWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        string? typeString = FreqTypeComboBox.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            FreqTypeComboBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (FreqTypeComboBox.BorderBrush == Brushes.Red)
        {
            FreqTypeComboBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        string path = TextBlockPath.Text;
        if (string.IsNullOrEmpty(path)
            || (!Directory.Exists(path) && !File.Exists(path))
            || Storage.FreqDicts.Values.Select(freq => freq.Path).Contains(path))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrEmpty(name) || Storage.FreqDicts.Values.Select(freq => freq.Name).Contains(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (NameTextBox.BorderBrush == Brushes.Red)
        {
            NameTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        if (isValid)
        {
            FreqType type = typeString!.GetEnum<FreqType>();

            // lowest priority means highest number
            int lowestPriority = Storage.FreqDicts.Select(freq => freq.Value.Priority).Max();

            Storage.FreqDicts.Add(name,
                new Freq(type, name, path, true, lowestPriority + 1));

            Close();
        }
    }

    private void BrowseForFrequencyFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Storage.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() == true)
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath;
        }
    }

    private void BrowseForFrequencyFolder()
    {
        using var fbd = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Storage.ApplicationPath };

        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, fbd.SelectedPath);
            TextBlockPath.Text = relativePath;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FreqTypeComboBox.ItemsSource = Enum.GetValues(typeof(FreqType)).Cast<FreqType>().Select(ft => ft.GetDescription() ?? ft.ToString());
    }
    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = FreqTypeComboBox.SelectionBoxItem.ToString()!;
        FreqType selectedFreqType = typeString.GetEnum<FreqType>();

        switch (selectedFreqType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case FreqType.Yomichan:
                BrowseForFrequencyFolder();
                break;

            case FreqType.Nazeka:
                BrowseForFrequencyFile("Nazeka file|*.json");
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid FreqType (Add)");
        }
    }
}
