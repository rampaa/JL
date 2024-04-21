using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddFrequencyWindow.xaml
/// </summary>
internal sealed partial class AddFrequencyWindow : Window
{
    private readonly FreqOptionsControl _freqOptionsControl;
    public AddFrequencyWindow()
    {
        InitializeComponent();
        _freqOptionsControl = new FreqOptionsControl();
        _ = FreqStackPanel.Children.Add(_freqOptionsControl);
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
            FreqTypeComboBox.ClearValue(BorderBrushProperty);
        }

        string path = TextBlockPath.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path)
            || !Path.Exists(fullPath)
            || FreqUtils.FreqDicts.Values.Any(freq => freq.Path == path))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.ClearValue(BorderBrushProperty);
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrWhiteSpace(name)
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || FreqUtils.FreqDicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (NameTextBox.BorderBrush == Brushes.Red)
        {
            NameTextBox.ClearValue(BorderBrushProperty);
        }

        if (isValid)
        {
            FreqType type = typeString!.GetEnum<FreqType>();

            FreqOptions options = _freqOptionsControl.GetFreqOptions(type);

            FreqUtils.FreqDicts.Add(name,
                new Freq(type, name, path, true, FreqUtils.FreqDicts.Count + 1, 0, 0, false, options));

            Close();
        }
    }

    private void BrowseForFrequencyFile(string filter)
    {
        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = Utils.ApplicationPath,
            Filter = filter
        };

        if (openFileDialog.ShowDialog() is true)
        {
            TextBlockPath.Text = Utils.GetPath(openFileDialog.FileName);
        }
    }

    private void BrowseForFrequencyFolder()
    {
        OpenFolderDialog openFolderDialog = new()
        {
            InitialDirectory = Utils.ApplicationPath
        };

        if (openFolderDialog.ShowDialog() is true)
        {
            TextBlockPath.Text = Utils.GetPath(openFolderDialog.FolderName);
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

    private void GenerateDictOptions()
    {
        string? typeString = FreqTypeComboBox.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(typeString))
        {
            FreqType type = typeString.GetEnum<FreqType>();
            _freqOptionsControl.GenerateFreqOptionsElements(type);
        }

        else
        {
            _freqOptionsControl.OptionsStackPanel.Visibility = Visibility.Collapsed;
            _freqOptionsControl.OptionsTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void FreqTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        GenerateDictOptions();
    }
}
