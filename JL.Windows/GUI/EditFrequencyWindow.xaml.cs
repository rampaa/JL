using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditFrequencyWindow.xaml
/// </summary>
internal sealed partial class EditFrequencyWindow
{
    private readonly Freq _freq;
    private readonly FreqOptionsControl _freqOptionsControl;

    public EditFrequencyWindow(Freq freq)
    {
        _freq = freq;
        _freqOptionsControl = new FreqOptionsControl();
        InitializeComponent();
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

        string path = PathTextBlock.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);
        if (string.IsNullOrWhiteSpace(path) || (_freq.Path != path && !Path.Exists(fullPath)))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Invalid path!";
            return;
        }

        if (_freq.Path != path && FreqUtils.FreqDicts.Values.Any(dict => dict.Path == path))
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

        if (!_freq.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && FreqUtils.FreqDicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Name of the frequency dictionary must be unique!";
            return;
        }

        string dbPath = DBUtils.GetFreqDBPath(_freq.Name);
        bool dbExists = File.Exists(dbPath);

        string? revision = (string?)NameTextBox.Tag;
        bool pathChanged = _freq.Path != path;
        if (pathChanged || _freq.Revision == revision)
        {
            if (pathChanged && _freq.Type is FreqType.Yomichan or FreqType.YomichanKanji)
            {
                bool hasValidFiles = Directory.EnumerateFiles(fullPath, "*_meta_bank_*.json", SearchOption.TopDirectoryOnly).Any();
                if (!hasValidFiles)
                {
                    PathTextBlock.BorderBrush = Brushes.Red;
                    PathTextBlock.Cursor = Cursors.Help;
                    PathTextBlock.ToolTip = "No valid file was found at the specified path!";
                    return;
                }
            }

            _freq.Path = path;
            _freq.Revision = revision;
            _freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
            _freq.Ready = false;

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        FreqOptions options = _freqOptionsControl.GetFreqOptions(_freq.Type, _freq.AutoUpdatable);

        if (_freq.Options.UseDB.Value != options.UseDB.Value)
        {
            _freq.Ready = false;
            //if (dbExists && !(options.UseDB?.Value ?? false))
            //{
            //    DBUtils.DeleteDB(dbPath);
            //    dbExists = false;
            //}
        }

        _freq.AutoUpdatable = _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.IsVisible;
        if (!_freq.AutoUpdatable)
        {
            _freq.Url = null;
        }
        else
        {
            string? url = (string?)_freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Tag;
            if (url is not null)
            {
                _freq.Url = new Uri(url);
            }
        }

        _freq.Options = options;
        if (_freq.Name != name)
        {
            if (dbExists)
            {
                DBUtils.SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Move(dbPath, DBUtils.GetFreqDBPath(name));
            }

            _ = FreqUtils.FreqDicts.Remove(_freq.Name);
            FreqUtils.FreqDicts.Add(name, new Freq(_freq.Type, name, _freq.Path, _freq.Active, _freq.Priority, _freq.Size, _freq.MaxValue, _freq.Options, _freq.AutoUpdatable, _freq.Url, _freq.Revision));
        }

        Close();
    }

    private void BrowseForFrequencyFile(string filter)
    {
        string? initialDirectory = Path.GetDirectoryName(_freq.Path);
        if (!Directory.Exists(initialDirectory))
        {
            initialDirectory = Utils.ApplicationPath;
        }

        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = initialDirectory,
            Filter = filter
        };

        if (openFileDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = Utils.GetPath(openFileDialog.FileName);
        }
    }

    private void BrowseForFrequencyFolder()
    {
        string initialDirectory = _freq.Path;
        if (!Directory.Exists(initialDirectory))
        {
            initialDirectory = Utils.ApplicationPath;
        }

        OpenFolderDialog openFolderDialog = new()
        {
            InitialDirectory = initialDirectory
        };

        if (openFolderDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = Utils.GetPath(openFolderDialog.FolderName);
            string indexJsonPath = Path.Join(openFolderDialog.FolderName, "index.json");
            if (File.Exists(indexJsonPath))
            {
                JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(indexJsonPath), Utils.Jso);
                NameTextBox.Tag = jsonElement.GetProperty("revision").GetString();

                if (jsonElement.TryGetProperty("frequencyMode", out JsonElement frequencyModeJsonElement))
                {
                    _freqOptionsControl.HigherValueMeansHigherFrequencyCheckBox.IsChecked = frequencyModeJsonElement.GetString() is "occurrence-based";
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
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string type = _freq.Type.GetDescription();
        _ = FreqTypeComboBox.Items.Add(type);
        FreqTypeComboBox.SelectedValue = type;
        PathTextBlock.Text = _freq.Path;
        NameTextBox.Text = _freq.Name;

        _freqOptionsControl.GenerateFreqOptionsElements(_freq.Type, _freq.Options);
        _freqOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = _freq.AutoUpdatable
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? typeString = FreqTypeComboBox.SelectionBoxItem.ToString();
        Debug.Assert(typeString is not null);

        FreqType selectedFreqType = typeString.GetEnum<FreqType>();

        switch (selectedFreqType)
        {
            case FreqType.Yomichan:
            case FreqType.YomichanKanji:
                BrowseForFrequencyFolder();
                break;

            // Not providing a description for the filter causes the filename returned to be empty
            case FreqType.Nazeka:
                BrowseForFrequencyFile("Nazeka file|*.json");
                break;

            default:
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(FreqType), nameof(EditFrequencyWindow), nameof(BrowsePathButton_OnClick), selectedFreqType);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid frequency type: {selectedFreqType}");
                break;
        }
    }
}
