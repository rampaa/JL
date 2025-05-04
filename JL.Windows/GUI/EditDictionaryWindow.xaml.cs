using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.Options;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditDictionaryWindow.xaml
/// </summary>
internal sealed partial class EditDictionaryWindow
{
    private readonly DictBase _dict;

    private readonly DictOptionsControl _dictOptionsControl;

    public EditDictionaryWindow(DictBase dict)
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
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

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

        if (_dict.Path != path)
        {
            if (DictUtils.YomichanDictTypes.Contains(_dict.Type))
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
            _dict.Ready = false;

            switch (_dict)
            {
                case Dict<CustomWordRecord> customWordDict:
                {
                    customWordDict.Contents = FrozenDictionary<string, IList<CustomWordRecord>>.Empty;
                    break;
                }

                case Dict<CustomNameRecord> customNameDict:
                {
                    customNameDict.Contents = FrozenDictionary<string, IList<CustomNameRecord>>.Empty;
                    break;
                }

                case Dict<JmdictRecord> jmdict:
                {
                    jmdict.Contents = FrozenDictionary<string, IList<JmdictRecord>>.Empty;
                    break;
                }

                case Dict<JmnedictRecord> jmnedict:
                {
                    jmnedict.Contents = FrozenDictionary<string, IList<JmnedictRecord>>.Empty;
                    break;
                }

                case Dict<KanjidicRecord> kanjidic:
                {
                    kanjidic.Contents = FrozenDictionary<string, IList<KanjidicRecord>>.Empty;
                    break;
                }

                case Dict<PitchAccentRecord> pitchAccent:
                {
                    pitchAccent.Contents = FrozenDictionary<string, IList<PitchAccentRecord>>.Empty;
                    break;
                }

                case Dict<EpwingYomichanRecord> yomichanRecord:
                {
                    yomichanRecord.Contents = FrozenDictionary<string, IList<EpwingYomichanRecord>>.Empty;
                    break;
                }

                case Dict<YomichanKanjiRecord> yomichanKanji:
                {
                    yomichanKanji.Contents = FrozenDictionary<string, IList<YomichanKanjiRecord>>.Empty;
                    break;
                }

                case Dict<EpwingNazekaRecord> nazeka:
                {
                    nazeka.Contents = FrozenDictionary<string, IList<EpwingNazekaRecord>>.Empty;
                    break;
                }

                default:
                {
                    Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(EditDictionaryWindow), nameof(SaveButton_Click), _dict.Type);
                    Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {_dict.Type}");
                    break;
                }
            }

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        DictOptions options = _dictOptionsControl.GetDictOptions(_dict.Type);
        if (_dict.Type is DictType.PitchAccentYomichan)
        {
            Debug.Assert(_dict.Options.ShowPitchAccentWithDottedLines is not null);
            Debug.Assert(options.ShowPitchAccentWithDottedLines is not null);
            bool oldDottedLinesOption = _dict.Options.ShowPitchAccentWithDottedLines.Value;
            bool newDottedLinesOption = options.ShowPitchAccentWithDottedLines.Value;

            if (oldDottedLinesOption != newDottedLinesOption)
            {
                PopupWindowUtils.SetStrokeDashArray(newDottedLinesOption);
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


            DictBase? dict = null;
            switch (_dict.Type)
            {
                case DictType.NonspecificKanjiWithWordSchemaYomichan:
                case DictType.NonspecificNameYomichan:
                case DictType.NonspecificYomichan:
                {
                    dict = new Dict<EpwingYomichanRecord>(_dict.Type, name, path, true, DictUtils.Dicts.Count + 1, 0, options);
                    break;
                }

                case DictType.NonspecificKanjiYomichan:
                {
                    dict = new Dict<YomichanKanjiRecord>(_dict.Type, name, path, true, DictUtils.Dicts.Count + 1, 0, options);
                    break;
                }

                case DictType.PitchAccentYomichan:
                {
                    dict = new Dict<PitchAccentRecord>(_dict.Type, name, path, true, DictUtils.Dicts.Count + 1, 0, options);
                    break;
                }

                case DictType.NonspecificWordNazeka:
                case DictType.NonspecificKanjiNazeka:
                case DictType.NonspecificNameNazeka:
                case DictType.NonspecificNazeka:
                {
                    dict = new Dict<EpwingNazekaRecord>(_dict.Type, name, path, true, DictUtils.Dicts.Count + 1, 0, options);
                    break;
                }

                case DictType.JMdict:
                case DictType.JMnedict:
                case DictType.Kanjidic:
                case DictType.CustomWordDictionary:
                case DictType.CustomNameDictionary:
                case DictType.ProfileCustomWordDictionary:
                case DictType.ProfileCustomNameDictionary:
                case DictType.NonspecificWordYomichan:
                    break;

                default:
                {
                    Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(AddDictionaryWindow), nameof(SaveButton_Click), _dict.Type);
                    Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {_dict.Type}");
                    break;
                }
            }

            if (dict is not null)
            {
                DictUtils.Dicts.Add(name, dict);
            }
        }

        Close();
    }

    private void BrowseForDictionaryFile(string filter)
    {
        string? initialDirectory = Path.GetDirectoryName(_dict.Path);
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

    private void BrowseForDictionaryFolder()
    {
        string initialDirectory = _dict.Path;
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
        _dictOptionsControl.GenerateDictOptionsElements(_dict);
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
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(EditDictionaryWindow), nameof(BrowsePathButton_OnClick), selectedDictType);
                Utils.Frontend.Alert(AlertLevel.Error, $"Invalid dictionary type: {selectedDictType}");
                break;
        }
    }
}
