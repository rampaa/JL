using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using Microsoft.Win32;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddDictionaryWindow.xaml
/// </summary>
internal sealed partial class AddDictionaryWindow
{
    private readonly DictOptionsControl _dictOptionsControl;

    public AddDictionaryWindow()
    {
        InitializeComponent();
        _dictOptionsControl = new DictOptionsControl();
        _ = DictStackPanel.Children.Add(_dictOptionsControl);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ComboBoxDictType.ClearValue(BorderBrushProperty);
        TextBlockPath.ClearValue(BorderBrushProperty);
        NameTextBox.ClearValue(BorderBrushProperty);

        string? typeString = ComboBoxDictType.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            ComboBoxDictType.BorderBrush = Brushes.Red;
            return;
        }

        string path = TextBlockPath.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path)
            || !Path.Exists(fullPath)
            || DictUtils.Dicts.Values.Any(dict => dict.Path == path))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            return;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > 128
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || DictUtils.Dicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            return;
        }

        DictType type = typeString.GetEnum<DictType>();
        if (DictUtils.YomichanDictTypes.Contains(type))
        {
            if (type is DictType.NonspecificKanjiYomichan)
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

        DictOptions options = _dictOptionsControl.GetDictOptions(type);
        Dict dict = new(type, name, path, true, DictUtils.Dicts.Count + 1, 0, false, options);
        DictUtils.Dicts.Add(name, dict);

        if (dict.Type is DictType.PitchAccentYomichan)
        {
            DictUtils.SingleDictTypeDicts[DictType.PitchAccentYomichan] = dict;
        }

        Close();
    }

    private void BrowseForDictionaryFile(string filter)
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

    private void BrowseForDictionaryFolder()
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
        RadioButtonYomichanImport.IsChecked = true;
        FillDictTypesCombobox(DictUtils.YomichanDictTypes);
    }

    private void RadioButtonYomichanImport_OnClick(object sender, RoutedEventArgs e)
    {
        FillDictTypesCombobox(DictUtils.YomichanDictTypes);
        HideDictOptions();
    }

    private void RadioButtonNazekaEpwingConverter_OnClick(object sender, RoutedEventArgs e)
    {
        FillDictTypesCombobox(DictUtils.NazekaDictTypes);
        HideDictOptions();
    }

    private void FillDictTypesCombobox(DictType[] types)
    {
        IEnumerable<DictType> loadedDictTypes = DictUtils.Dicts.Values.Select(static dict => dict.Type);
        IEnumerable<DictType> validTypes = types.Except(loadedDictTypes.Except(DictUtils.NonspecificDictTypes));

        ComboBoxDictType.ItemsSource = validTypes.Select(static d => d.GetDescription() ?? d.ToString());
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = ComboBoxDictType.SelectionBoxItem.ToString()!;
        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // not providing a description for the filter causes the filename returned to be empty
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

            case DictType.NonspecificWordNazeka:
            case DictType.NonspecificKanjiNazeka:
            case DictType.NonspecificNameNazeka:
            case DictType.NonspecificNazeka:
                BrowseForDictionaryFile("Nazeka file|*.json");
                break;

            case DictType.JMdict:
            case DictType.JMnedict:
            case DictType.Kanjidic:
            case DictType.CustomWordDictionary:
            case DictType.CustomNameDictionary:
            case DictType.ProfileCustomWordDictionary:
            case DictType.ProfileCustomNameDictionary:
                break;

            default:
                throw new ArgumentOutOfRangeException(null, selectedDictType, "Invalid DictType (Add)");
        }
    }

    private void ComboBoxDictType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GenerateDictOptions();
    }

    private void GenerateDictOptions()
    {
        string? typeString = ComboBoxDictType.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(typeString))
        {
            DictType type = typeString.GetEnum<DictType>();
            _dictOptionsControl.GenerateDictOptionsElements(type);
        }

        else
        {
            HideDictOptions();
        }
    }

    private void HideDictOptions()
    {
        _dictOptionsControl.OptionsStackPanel.Visibility = Visibility.Collapsed;
        _dictOptionsControl.OptionsTextBlock.Visibility = Visibility.Collapsed;
    }
}
