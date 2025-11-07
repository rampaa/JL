using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Windows.GUI.Options;
using JL.Windows.Utilities;
using Microsoft.Win32;

namespace JL.Windows.GUI.Frequency;

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
        string fullPath = Path.GetFullPath(path, AppInfo.ApplicationPath);

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

        bool autoUpdatable = _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.IsVisible;
        Uri? indexUrl = (Uri?)_freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Tag;
        string? revision = (string?)NameTextBox.Tag;

        FreqOptions options = _freqOptionsControl.GetFreqOptions(type, autoUpdatable);

        FreqUtils.FreqDicts.Add(name,
            new Freq(type, name, path, true, FreqUtils.FreqDicts.Count + 1, 0, 0, options, autoUpdatable, indexUrl, revision));

        Close();
    }

    private void BrowseForFrequencyFile(string filter)
    {
        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = AppInfo.ApplicationPath,
            Filter = filter
        };

        if (openFileDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = PathUtils.GetPortablePath(openFileDialog.FileName);
        }
    }

    private void BrowseForFrequencyFolder()
    {
        OpenFolderDialog openFolderDialog = new()
        {
            InitialDirectory = AppInfo.ApplicationPath
        };

        if (openFolderDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = PathUtils.GetPortablePath(openFolderDialog.FolderName);

            string indexJsonPath = Path.Join(openFolderDialog.FolderName, "index.json");
            if (File.Exists(indexJsonPath))
            {
                using FileStream fileStream = new(indexJsonPath, FileStreamOptionsPresets.SyncReadFso);
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(fileStream, JsonOptions.DefaultJso);

                string? dictionaryTitle = jsonElement.GetProperty("title").GetString();
                Debug.Assert(dictionaryTitle is not null);
                NameTextBox.Text = dictionaryTitle;

                NameTextBox.Tag = jsonElement.GetProperty("revision").GetString();

                if (jsonElement.TryGetProperty("frequencyMode", out JsonElement frequencyModeJsonElement))
                {
                    _freqOptionsControl.HigherValueMeansHigherFrequencyCheckBox.IsChecked = frequencyModeJsonElement.GetString() is "occurrence-based";
                }
                else
                {
                    _freqOptionsControl.HigherValueMeansHigherFrequencyCheckBox.IsChecked = false;
                }

                bool isUpdatable = jsonElement.TryGetProperty("isUpdatable", out JsonElement isUpdatableJsonElement) && isUpdatableJsonElement.GetBoolean();
                if (isUpdatable)
                {
                    _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;

                    string? indexUrl = jsonElement.GetProperty("indexUrl").GetString();
                    Debug.Assert(indexUrl is not null);
                    _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Tag = new Uri(indexUrl);
                }
                else
                {
                    _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
                _freqOptionsControl.HigherValueMeansHigherFrequencyCheckBox.IsChecked = false;
            }
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
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(FreqType), nameof(AddFrequencyWindow), nameof(BrowsePathButton_OnClick), selectedFreqType);
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
            _freqOptionsControl.GenerateFreqOptionsElements(type, null);
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
