using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using JL.Windows.GUI.Options;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace JL.Windows.GUI.Dictionary;

/// <summary>
/// Interaction logic for EditDictionaryWindow.xaml
/// </summary>
internal sealed partial class EditDictionaryWindow
{
    private readonly Dict _dict;

    private readonly DictOptionsControl _dictOptionsControl;

    public EditDictionaryWindow(Dict dict)
    {
        _dict = dict;
        _dictOptionsControl = new DictOptionsControl();
        InitializeComponent();
        _ = DictStackPanel.Children.Add(_dictOptionsControl);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ComboBoxDictType.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(CursorProperty);
        PathTextBlock.ClearValue(ToolTipProperty);
        NameTextBox.ClearValue(BorderBrushProperty);
        NameTextBox.ToolTip = "Dictionary name must be unique";

        string path = PathTextBlock.Text;
        string fullPath = Path.GetFullPath(path, AppInfo.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path) || (_dict.Path != path && !Path.Exists(fullPath)))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Invalid path!";
            return;
        }

        if (_dict.Path != path && DictUtils.Dicts.Values.Any(dict => dict.Path == path))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Dictionary path must be unique!";
            return;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > 128
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Invalid dictionary name!";
            return;
        }

        if (!_dict.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && DictUtils.Dicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Dictionary name must be unique!";
            return;
        }

        string dbPath = DBUtils.GetDictDBPath(_dict.Name);
        bool dbExists = File.Exists(dbPath);

        string? revision = (string?)NameTextBox.Tag;
        bool pathChanged = _dict.Path != path;
        if (pathChanged || _dict.Revision == revision)
        {
            if (pathChanged && DictUtils.YomichanDictTypes.Contains(_dict.Type))
            {
                bool validPath = Directory.EnumerateFiles(fullPath,
                    _dict.Type is DictType.NonspecificKanjiYomichan
                        ? "kanji_bank_*.json"
                        : _dict.Type is DictType.PitchAccentYomichan
                            ? "term*bank_*.json"
                            : "term_bank_*.json",
                    SearchOption.TopDirectoryOnly).Any();

                if (!validPath)
                {
                    PathTextBlock.BorderBrush = Brushes.Red;
                    PathTextBlock.Cursor = Cursors.Help;
                    PathTextBlock.ToolTip = "No valid file was found at the specified path!";
                    return;
                }
            }

            _dict.Path = path;
            _dict.Revision = revision;
            _dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            _dict.Ready = false;

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        _dict.AutoUpdatable = _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.IsVisible;
        if (!_dict.AutoUpdatable)
        {
            _dict.Url = null;
        }
        else
        {
            string? url = (string?)_dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Tag;
            if (url is not null)
            {
                _dict.Url = new Uri(url);
            }
        }

        DictOptions options = _dictOptionsControl.GetDictOptions(_dict.Type, _dict.AutoUpdatable);
        if (_dict.Type is DictType.PitchAccentYomichan)
        {
            Debug.Assert(_dict.Options.ShowPitchAccentWithDottedLines is not null);
            Debug.Assert(options.ShowPitchAccentWithDottedLines is not null);
            bool oldDottedLinesOption = _dict.Options.ShowPitchAccentWithDottedLines.Value;
            bool newDottedLinesOption = options.ShowPitchAccentWithDottedLines.Value;

            Debug.Assert(_dict.Options.PitchAccentMarkerColor is not null);
            Debug.Assert(options.PitchAccentMarkerColor is not null);
            string oldPitchAccentMarkerColor = _dict.Options.PitchAccentMarkerColor.Value;
            string newPitchAccentMarkerColor = options.PitchAccentMarkerColor.Value;

            if (oldDottedLinesOption != newDottedLinesOption || oldPitchAccentMarkerColor != newPitchAccentMarkerColor)
            {
                PopupWindowUtils.SetPitchAccentMarkerPen(newDottedLinesOption, WindowsUtils.FrozenBrushFromHex(newPitchAccentMarkerColor));
            }
        }

        if (_dict.Options.UseDB.Value != options.UseDB.Value)
        {
            _dict.Ready = false;
            //if (dbExists && !(options.UseDB?.Value ?? false))
            //{
            //    DBUtils.DeleteDB(dbPath);
            //    dbExists = false;
            //}
        }

        _dict.Options = options;

        if (_dict.Name != name)
        {
            if (dbExists)
            {
                DBUtils.SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Move(dbPath, DBUtils.GetDictDBPath(name));
            }

            _ = DictUtils.Dicts.Remove(_dict.Name);
            DictUtils.Dicts.Add(name, new Dict(_dict.Type, name, _dict.Path, _dict.Active, _dict.Priority, _dict.Size, _dict.Options, _dict.AutoUpdatable, _dict.Url, _dict.Revision));
        }

        Close();
    }

    private void BrowseForDictionaryFile(string filter)
    {
        string? initialDirectory = Path.GetDirectoryName(_dict.Path);
        if (!Directory.Exists(initialDirectory))
        {
            initialDirectory = AppInfo.ApplicationPath;
        }

        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = initialDirectory,
            Filter = filter
        };

        if (openFileDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = PathUtils.GetPortablePath(openFileDialog.FileName);
        }
    }

    private void BrowseForDictionaryFolder()
    {
        string initialDirectory = _dict.Path;
        if (!Directory.Exists(initialDirectory))
        {
            initialDirectory = AppInfo.ApplicationPath;
        }

        OpenFolderDialog openFolderDialog = new()
        {
            InitialDirectory = initialDirectory
        };

        if (openFolderDialog.ShowDialog() is true)
        {
            PathTextBlock.Text = PathUtils.GetPortablePath(openFolderDialog.FolderName);
            string indexJsonPath = Path.Join(openFolderDialog.FolderName, "index.json");
            if (File.Exists(indexJsonPath))
            {
                JsonElement jsonElement;
                using (FileStream fileStream = new(indexJsonPath, FileStreamOptionsPresets.SyncReadFso))
                {
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(fileStream, JsonOptions.DefaultJso);
                }

                NameTextBox.Tag = jsonElement.GetProperty("revision").GetString();

                bool isUpdatable = jsonElement.TryGetProperty("isUpdatable", out JsonElement isUpdatableJsonElement) && isUpdatableJsonElement.GetBoolean();
                if (isUpdatable)
                {
                    _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Visible;

                    string? indexUrl = jsonElement.GetProperty("indexUrl").GetString();
                    Debug.Assert(indexUrl is not null);
                    _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Tag = new Uri(indexUrl);
                }
                else
                {
                    _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string type = _dict.Type.GetDescription();
        _ = ComboBoxDictType.Items.Add(type);
        ComboBoxDictType.SelectedValue = type;
        PathTextBlock.Text = _dict.Path;

        bool isNotCustomDict = _dict.Type is not DictType.ProfileCustomNameDictionary
            and not DictType.ProfileCustomWordDictionary
            and not DictType.CustomNameDictionary
            and not DictType.CustomWordDictionary;

        PathTextBlock.IsEnabled = isNotCustomDict;
        FolderBrowseButton.IsEnabled = isNotCustomDict;

        NameTextBox.Text = _dict.Name;
        _dictOptionsControl.GenerateDictOptionsElements(_dict.Type, _dict.Options);
        _dictOptionsControl.AutoUpdateAfterNDaysDockPanel.Visibility = _dict.AutoUpdatable
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? typeString = ComboBoxDictType.SelectionBoxItem.ToString();
        Debug.Assert(typeString is not null);
        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // Not providing a description for the filter causes the filename returned to be empty
            case DictType.JMdict:
                BrowseForDictionaryFile("JMdict file|JMdict.xml");
                break;

            case DictType.JMnedict:
                BrowseForDictionaryFile("JMnedict file|JMnedict.xml");
                break;

            case DictType.Kanjidic:
                BrowseForDictionaryFile("kanjidic2 file|kanjidic2.xml");
                break;

            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
            case DictType.NonspecificNazeka:
                BrowseForDictionaryFile("Nazeka file|*.json");
                break;

            case DictType.NonspecificWordYomichan:
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
            case DictType.PitchAccentYomichan:
                BrowseForDictionaryFolder();
                break;

            case DictType.CustomWordDictionary:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomWordDictionary:
            case DictType.ProfileCustomNameDictionary:
                break;

            default:
                LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(EditDictionaryWindow), nameof(BrowsePathButton_OnClick), selectedDictType);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid dictionary type: {selectedDictType}");
                break;
        }
    }
}
