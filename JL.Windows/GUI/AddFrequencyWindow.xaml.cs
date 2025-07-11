using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddFrequencyWindow.xaml
/// </summary>
internal sealed partial class AddFrequencyWindow
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
        FreqTypeComboBox.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(CursorProperty);
        PathTextBlock.ClearValue(ToolTipProperty);
        NameTextBox.ClearValue(BorderBrushProperty);
        NameTextBox.ToolTip = "Name of the frequency dictionary must be unique";

        string? typeString = FreqTypeComboBox.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            FreqTypeComboBox.BorderBrush = Brushes.Red;
            return;
        }

        string path = PathTextBlock.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path) || !Path.Exists(fullPath))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Invalid path!";
            return;
        }

        if (FreqUtils.FreqDicts.Values.Any(freq => freq.Path == path))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Path of the frequency dictionary must be unique!";
            return;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > 128
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Invalid frequency dictionary name!";
            return;
        }

        if (FreqUtils.FreqDicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Name of the frequency dictionary must be unique!";
            return;
        }

        FreqType type = typeString.GetEnum<FreqType>();
        if (type is FreqType.Yomichan or FreqType.YomichanKanji)
        {
            bool validPath = Directory.EnumerateFiles(fullPath, "*_meta_bank_*.json", SearchOption.TopDirectoryOnly).Any();
            if (!validPath)
            {
                PathTextBlock.BorderBrush = Brushes.Red;
                PathTextBlock.Cursor = Cursors.Help;
                PathTextBlock.ToolTip = "No valid file was found at the specified path!";
                return;
            }
        }

        FreqOptions options = _freqOptionsControl.GetFreqOptions(type);

        FreqUtils.FreqDicts.Add(name,
            new Freq(type, name, path, true, FreqUtils.FreqDicts.Count + 1, 0, 0, options));

        Close();
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
            PathTextBlock.Text = Utils.GetPath(openFileDialog.FileName);
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
            PathTextBlock.Text = Utils.GetPath(openFolderDialog.FolderName);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        FreqTypeComboBox.ItemsSource = Enum.GetValues<FreqType>().Select(static ft => ft.GetDescription());
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? typeString = FreqTypeComboBox.SelectionBoxItem.ToString();
        Debug.Assert(typeString is not null);

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
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(FreqType), nameof(AddFrequencyWindow), nameof(BrowsePathButton_OnClick), selectedFreqType);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid frequency type: {selectedFreqType}");
                break;
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

    private void FreqTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FreqTypeComboBox.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(CursorProperty);
        PathTextBlock.ClearValue(ToolTipProperty);
        PathTextBlock.Clear();
        GenerateDictOptions();
    }
}
