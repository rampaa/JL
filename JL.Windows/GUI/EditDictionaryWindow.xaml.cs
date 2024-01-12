using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
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
        bool isValid = true;

        string path = TextBlockPath.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path)
            || (!Directory.Exists(fullPath) && !File.Exists(fullPath))
            || (_dict.Path != path && DictUtils.Dicts.Values.Any(dict => dict.Path == path)))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.ClearValue(BorderBrushProperty);
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrEmpty(name)
            || (_dict.Name != name && DictUtils.Dicts.ContainsKey(name)))
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
            string dbPath = DictUtils.GetDBPath(_dict.Name);
            bool dbExists = File.Exists(dbPath);

            if (_dict.Path != path)
            {
                _dict.Path = path;
                _dict.Contents.Clear();
                _dict.Ready = false;

                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                    dbExists = false;
                }
            }

            DictOptions options = _dictOptionsControl.GetDictOptions(_dict.Type);

            if (_dict.Type is DictType.PitchAccentYomichan)
            {
                bool oldDottedLinesOption = _dict.Options?.ShowPitchAccentWithDottedLines?.Value ?? true;
                bool newDottedLinesOption = options.ShowPitchAccentWithDottedLines?.Value ?? true;

                if (oldDottedLinesOption != newDottedLinesOption)
                {
                    PopupWindowUtils.StrokeDashArray = newDottedLinesOption
                        ? new DoubleCollection() { 1, 1 }
                        : new DoubleCollection() { 1, 0 };
                }
            }

            if (_dict.Options?.Examples?.Value != options.Examples?.Value)
            {
                _dict.Contents.Clear();

                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                    dbExists = false;
                }
            }

            if (_dict.Options?.UseDB?.Value != options.UseDB?.Value)
            {
                _dict.Ready = false;
                //if (dbExists && !(options.UseDB?.Value ?? false))
                //{
                //    SqliteConnection.ClearAllPools();
                //    File.Delete(dbPath);
                //    dbExists = false;
                //}
            }

            if (_dict.Name != name)
            {
                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Move(dbPath, DictUtils.GetDBPath(name));
                }

                _dict.Name = name;
            }

            _dict.Options = options;
            Utils.Frontend.InvalidateDisplayCache();

            Close();
        }
    }

    private void BrowseForDictionaryFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Utils.ApplicationPath, Filter = filter };
        if (openFileDialog.ShowDialog() is true)
        {
            TextBlockPath.Text = Utils.GetPath(openFileDialog.FileName);
        }
    }

    private void BrowseForDictionaryFolder()
    {
        using System.Windows.Forms.FolderBrowserDialog fbd = new();
        fbd.SelectedPath = Utils.ApplicationPath;

        if (fbd.ShowDialog() is System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            TextBlockPath.Text = Utils.GetPath(fbd.SelectedPath);
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
                BrowseForDictionaryFile("Kanjidic2 file|Kanjidic2.xml");
                break;
            case DictType.CustomWordDictionary:
                BrowseForDictionaryFile("Custom Word Dictionary file|*.txt");
                break;
            case DictType.CustomNameDictionary:
                BrowseForDictionaryFile("Custom Name Dictionary file|*.txt");
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
                BrowseForDictionaryFolder();
                break;

            case DictType.PitchAccentYomichan:
                BrowseForDictionaryFolder();
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

            case DictType.ProfileCustomWordDictionary:
            case DictType.ProfileCustomNameDictionary:
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid DictType (Edit)");
        }
    }
}
