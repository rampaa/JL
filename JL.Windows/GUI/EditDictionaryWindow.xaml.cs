using System.Collections.Frozen;
using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditDictionaryWindow.xaml
/// </summary>
internal sealed partial class EditDictionaryWindow : Window
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
        TextBlockPath.ClearValue(BorderBrushProperty);
        NameTextBox.ClearValue(BorderBrushProperty);

        string path = TextBlockPath.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path)
            || (_dict.Path != path
                && (!Path.Exists(fullPath) || DictUtils.Dicts.Values.Any(dict => dict.Path == path))))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            return;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > 128
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || (!_dict.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && DictUtils.Dicts.ContainsKey(name)))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            return;
        }

        string dbPath = DBUtils.GetDictDBPath(_dict.Name);
        bool dbExists = File.Exists(dbPath);

        if (_dict.Path != path)
        {
            if (DictUtils.YomichanDictTypes.Contains(_dict.Type))
            {
                if (_dict.Type is DictType.NonspecificKanjiYomichan)
                {
                    bool validPath = Directory.EnumerateFiles(fullPath, "kanji_bank_*.json", SearchOption.TopDirectoryOnly).Any();
                    if (!validPath)
                    {
                        TextBlockPath.BorderBrush = Brushes.Red;
                        return;
                    }
                }
                else
                {
                    bool validPath = Directory.EnumerateFiles(fullPath, "*_bank_*.json", SearchOption.TopDirectoryOnly)
                        .Any(static s => s.Contains("term", StringComparison.Ordinal) || s.Contains("kanji", StringComparison.Ordinal));

                    if (!validPath)
                    {
                        TextBlockPath.BorderBrush = Brushes.Red;
                        return;
                    }
                }
            }

            _dict.Path = path;
            _dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            _dict.Ready = false;

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        DictOptions options = _dictOptionsControl.GetDictOptions(_dict.Type);
        if (_dict.Type is DictType.PitchAccentYomichan)
        {
            bool oldDottedLinesOption = _dict.Options.ShowPitchAccentWithDottedLines!.Value;
            bool newDottedLinesOption = options.ShowPitchAccentWithDottedLines!.Value;

            if (oldDottedLinesOption != newDottedLinesOption)
            {
                PopupWindowUtils.SetStrokeDashArray(newDottedLinesOption);
            }
        }

        if (_dict.Options.Examples?.Value != options.Examples?.Value)
        {
            _dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
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

        if (_dict.Name != name)
        {
            if (dbExists)
            {
                DBUtils.SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Move(dbPath, DBUtils.GetDictDBPath(name));
            }

            _ = DictUtils.Dicts.Remove(_dict.Name);
            _dict.Name = name;
            DictUtils.Dicts.Add(name, _dict);
        }

        _dict.Options = options;

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
            TextBlockPath.Text = Utils.GetPath(openFileDialog.FileName);
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
            TextBlockPath.Text = Utils.GetPath(openFolderDialog.FolderName);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string type = _dict.Type.GetDescription() ?? _dict.Type.ToString();
        _ = ComboBoxDictType.Items.Add(type);
        ComboBoxDictType.SelectedValue = type;
        TextBlockPath.Text = _dict.Path;

        bool isNotCustomDict = _dict.Type is not DictType.ProfileCustomNameDictionary
            and not DictType.ProfileCustomWordDictionary
            and not DictType.CustomNameDictionary
            and not DictType.CustomWordDictionary;

        TextBlockPath.IsEnabled = isNotCustomDict;
        FolderBrowseButton.IsEnabled = isNotCustomDict;

        NameTextBox.Text = _dict.Name;
        _dictOptionsControl.GenerateDictOptionsElements(_dict);
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = ComboBoxDictType.SelectionBoxItem.ToString()!;
        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case DictType.JMdict:
                BrowseForDictionaryFile("JMdict file|JMdict.xml");
                break;
            case DictType.JMnedict:
                BrowseForDictionaryFile("JMnedict file|JMnedict.xml");
                break;
            case DictType.Kanjidic:
                BrowseForDictionaryFile("kanjidic2 file|kanjidic2.xml");
                break;

            case DictType.DaijirinNazeka:
                BrowseForDictionaryFile("Daijirin file|*.json");
                break;
            case DictType.KenkyuushaNazeka:
                BrowseForDictionaryFile("Kenkyuusha file|*.json");
                break;
            case DictType.ShinmeikaiNazeka:
                BrowseForDictionaryFile("Shinmeikai file|*.json");
                break;
            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
            case DictType.NonspecificNazeka:
                BrowseForDictionaryFile("Nazeka file|*.json");
                break;

            case DictType.CustomWordDictionary:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomWordDictionary:
            case DictType.ProfileCustomNameDictionary:
                break;

            case DictType.Kenkyuusha:
            case DictType.Daijirin:
            case DictType.Daijisen:
            case DictType.Koujien:
            case DictType.Meikyou:
            case DictType.Gakken:
            case DictType.Kotowaza:
            case DictType.IwanamiYomichan:
            case DictType.JitsuyouYomichan:
            case DictType.ShinmeikaiYomichan:
            case DictType.NikkokuYomichan:
            case DictType.ShinjirinYomichan:
            case DictType.OubunshaYomichan:
            case DictType.ZokugoYomichan:
            case DictType.WeblioKogoYomichan:
            case DictType.GakkenYojijukugoYomichan:
            case DictType.ShinmeikaiYojijukugoYomichan:
            case DictType.KanjigenYomichan:
            case DictType.KireiCakeYomichan:
            case DictType.NonspecificWordYomichan:
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
            case DictType.PitchAccentYomichan:
                BrowseForDictionaryFolder();
                break;

            default:
                throw new ArgumentOutOfRangeException(null, selectedDictType, "Invalid DictType (Edit)");
        }
    }
}
