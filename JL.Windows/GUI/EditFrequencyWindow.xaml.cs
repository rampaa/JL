using System.Collections.Frozen;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
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

        if (_freq.Path != path)
        {
            if (_freq.Type is FreqType.Yomichan or FreqType.YomichanKanji)
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
            _freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
            _freq.Ready = false;

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        FreqOptions options = _freqOptionsControl.GetFreqOptions(_freq.Type);

        if (_freq.Options.UseDB.Value != options.UseDB.Value)
        {
            _freq.Ready = false;
            //if (dbExists && !(options.UseDB?.Value ?? false))
            //{
            //    DBUtils.DeleteDB(dbPath);
            //    dbExists = false;
            //}
        }

        if (_freq.Name != name)
        {
            if (dbExists)
            {
                DBUtils.SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Move(dbPath, DBUtils.GetFreqDBPath(name));
            }

            _ = FreqUtils.FreqDicts.Remove(_freq.Name);
            _freq.Name = name;
            FreqUtils.FreqDicts.Add(name, _freq);
        }

        _freq.Options = options;

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
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string type = _freq.Type.GetDescription() ?? _freq.Type.ToString();
        _ = FreqTypeComboBox.Items.Add(type);
        FreqTypeComboBox.SelectedValue = type;
        PathTextBlock.Text = _freq.Path;
        NameTextBox.Text = _freq.Name;

        _freqOptionsControl.GenerateFreqOptionsElements(_freq);
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = FreqTypeComboBox.SelectionBoxItem.ToString()!;
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
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid frequency type: {selectedFreqType}");
                break;
        }
    }
}
