using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddFrequencyWindow.xaml
/// </summary>
internal sealed partial class AddFrequencyWindow : Window
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
            FreqTypeComboBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        string path = TextBlockPath.Text;
        if (string.IsNullOrEmpty(path)
            || (!Directory.Exists(path) && !File.Exists(path))
            || FreqUtils.FreqDicts.Values.Select(static freq => freq.Path).Contains(path))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrEmpty(name) || FreqUtils.FreqDicts.Values.Select(static freq => freq.Name).Contains(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (NameTextBox.BorderBrush == Brushes.Red)
        {
            NameTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        if (isValid)
        {
            FreqType type = typeString!.GetEnum<FreqType>();

            FreqUtils.FreqDicts.Add(name,
                new Freq(type, name, path, true, FreqUtils.FreqDicts.Count + 1, 0));

            Close();
        }
    }

    private void BrowseForFrequencyFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Utils.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() is true)
        {
            string relativePath = Path.GetRelativePath(Utils.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void BrowseForFrequencyFolder()
    {
        using var fbd = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Utils.ApplicationPath };

        if (fbd.ShowDialog() is System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            string relativePath = Path.GetRelativePath(Utils.ApplicationPath, fbd.SelectedPath);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FreqTypeComboBox.ItemsSource = Enum.GetValues<FreqType>().Select(static ft => ft.GetDescription() ?? ft.ToString());
    }
    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = FreqTypeComboBox.SelectionBoxItem.ToString()!;
        FreqType selectedFreqType = typeString.GetEnum<FreqType>();

        switch (selectedFreqType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case FreqType.Yomichan:
            case FreqType.YomichanKanji:
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
