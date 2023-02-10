using System.IO;
using System.Windows;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddDictionaryWindow.xaml
/// </summary>
internal sealed partial class AddDictionaryWindow : Window
{
    public AddDictionaryWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        string? typeString = ComboBoxDictType.SelectionBoxItem.ToString();
        if (string.IsNullOrEmpty(typeString))
        {
            ComboBoxDictType.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (ComboBoxDictType.BorderBrush == Brushes.Red)
        {
            ComboBoxDictType.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        string path = TextBlockPath.Text;
        if (string.IsNullOrEmpty(path)
            || (!Directory.Exists(path) && !File.Exists(path))
            || Storage.Dicts.Values.Select(static dict => dict.Path).Contains(path))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        string name = NameTextBox.Text;
        if (string.IsNullOrEmpty(name) || Storage.Dicts.Values.Select(static dict => dict.Name).Contains(name))
        {
            NameTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (NameTextBox.BorderBrush == Brushes.Red)
        {
            NameTextBox.BorderBrush = WindowsUtils.FrozenBrushFromHex("#FF3F3F46")!;
        }

        if (isValid)
        {
            DictType type = typeString!.GetEnum<DictType>();

            NewlineBetweenDefinitionsOption? newlineOption = null;
            if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(type))
            {
                bool isEpwing = Storage.YomichanDictTypes.Concat(Storage.NazekaDictTypes).Contains(type);
                newlineOption = new NewlineBetweenDefinitionsOption { Value = isEpwing };
            }

            ExamplesOption? examplesOption = null;
            // if (ExamplesOption.ValidDictTypes.Contains(type)) //todo
            // {
            //     Enum.TryParse<ExamplesOptionValue>(ComboBoxExamples.SelectedValue?.ToString(), out var eov);
            //     examplesOption = new ExamplesOption { Value = eov };
            // }

            // TODO?
            var options =
                new DictOptions(
                    newlineOption,
                    examplesOption);

            Storage.Dicts.Add(name,
                new Dict(type, name, path, true, Storage.Dicts.Count + 1, 0, options));

            Close();
        }
    }

    private void BrowseForDictionaryFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Storage.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() is true)
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void BrowseForDictionaryFolder()
    {
        using var fbd = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Storage.ApplicationPath };

        if (fbd.ShowDialog() is System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, fbd.SelectedPath);
            TextBlockPath.Text = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RadioButtonYomichanImport.IsChecked = true;
        FillDictTypesCombobox(Storage.YomichanDictTypes);
    }

    private void RadioButtonYomichanImport_OnClick(object sender, RoutedEventArgs e)
    {
        FillDictTypesCombobox(Storage.YomichanDictTypes);
    }

    private void RadioButtonNazekaEpwingConverter_OnClick(object sender, RoutedEventArgs e)
    {
        FillDictTypesCombobox(Storage.NazekaDictTypes);
    }

    private void FillDictTypesCombobox(IEnumerable<DictType> types)
    {
        IEnumerable<DictType> loadedDictTypes = Storage.Dicts.Values.Select(static dict => dict.Type);
        IEnumerable<DictType> validTypes = types.Except(loadedDictTypes.Except(Storage.NonspecificDictTypes));

        ComboBoxDictType.ItemsSource = validTypes.Select(static d => d.GetDescription() ?? d.ToString());
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = ComboBoxDictType.SelectionBoxItem.ToString()!;
        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // not providing a description for the filter causes the filename returned to be empty
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

            case DictType.JMdict:
            case DictType.JMnedict:
            case DictType.Kanjidic:
            case DictType.CustomWordDictionary:
            case DictType.CustomNameDictionary:
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid DictType (Add)");
        }
    }
}
