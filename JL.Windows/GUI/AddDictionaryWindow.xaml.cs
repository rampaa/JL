using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using JL.Windows.Utilities;
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
        PathTextBlock.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(CursorProperty);
        PathTextBlock.ClearValue(ToolTipProperty);
        NameTextBox.ClearValue(BorderBrushProperty);
        NameTextBox.ToolTip = "Dictionary name must be unique";

        string? typeString = ComboBoxDictType.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            ComboBoxDictType.BorderBrush = Brushes.Red;
            return;
        }

        string path = PathTextBlock.Text;
        string fullPath = Path.GetFullPath(path, Utils.ApplicationPath);

        if (string.IsNullOrWhiteSpace(path)
            || !Path.Exists(fullPath))
        {
            PathTextBlock.BorderBrush = Brushes.Red;
            PathTextBlock.Cursor = Cursors.Help;
            PathTextBlock.ToolTip = "Invalid path!";
            return;
        }

        if (DictUtils.Dicts.Values.Any(dict => dict.Path == path))
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

        if (DictUtils.Dicts.ContainsKey(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            NameTextBox.ToolTip = "Dictionary name must be unique!";
            return;
        }

        DictType type = typeString.GetEnum<DictType>();
        if (DictUtils.YomichanDictTypes.Contains(type))
        {
            bool validPath = Directory.EnumerateFiles(fullPath,
                type is DictType.NonspecificKanjiYomichan
                    ? "kanji_bank_*.json"
                    : type is DictType.PitchAccentYomichan
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

        DictOptions options = _dictOptionsControl.GetDictOptions(type);
        Dict dict = new(type, name, path, true, DictUtils.Dicts.Count + 1, 0, options);
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
            PathTextBlock.Text = Utils.GetPath(openFileDialog.FileName);
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
            PathTextBlock.Text = Utils.GetPath(openFolderDialog.FolderName);
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

        ComboBoxDictType.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(BorderBrushProperty);
        PathTextBlock.ClearValue(CursorProperty);
        PathTextBlock.ClearValue(ToolTipProperty);
        PathTextBlock.Clear();
        ComboBoxDictType.ItemsSource = validTypes.Select(static d => d.GetDescription());
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? typeString = ComboBoxDictType.SelectionBoxItem.ToString();
        Debug.Assert(typeString is not null);

        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case DictType.NonspecificWordYomichan:
            case DictType.NonspecificKanjiYomichan:
            case DictType.NonspecificKanjiWithWordSchemaYomichan:
            case DictType.NonspecificNameYomichan:
            case DictType.NonspecificYomichan:
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
                Utils.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(DictType), nameof(AddDictionaryWindow), nameof(BrowsePathButton_OnClick), selectedDictType);
                WindowsUtils.Alert(AlertLevel.Error, $"Invalid dictionary type: {selectedDictType}");
                break;
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
            _dictOptionsControl.GenerateDictOptionsElements(type, null);
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
