using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Utilities;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AddDictionaryWindow.xaml
/// </summary>
public partial class AddDictionaryWindow : Window
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
            ComboBoxDictType.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        string path = TextBlockPath.Text;
        if (string.IsNullOrEmpty(path) || (!Directory.Exists(path) && !File.Exists(path)))
        {
            TextBlockPath.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (TextBlockPath.BorderBrush == Brushes.Red)
        {
            TextBlockPath.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        if (isValid)
        {
            DictType type = typeString!.GetEnum<DictType>();

            // lowest priority means highest number
            int lowestPriority = Storage.Dicts.Select(dict => dict.Value.Priority).Max();

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

            Storage.Dicts.Add(type,
                new Dict(type, path, true, lowestPriority + 1, options));
            Storage.Dicts[type].Contents = new Dictionary<string, List<IResult>>();

            Close();
        }
    }

    private void BrowseForDictionaryFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = Storage.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() == true)
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath;
        }
    }

    private void BrowseForDictionaryFolder()
    {
        using var fbd = new FolderBrowserDialog { SelectedPath = Storage.ApplicationPath };

        if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
            !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, fbd.SelectedPath);
            TextBlockPath.Text = relativePath;
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
        List<DictType> loadedDictTypes = Storage.Dicts.Keys.ToList();
        IEnumerable<DictType> validTypes = types.Except(loadedDictTypes);

        ComboBoxDictType.ItemsSource = validTypes.Select(d => d.GetDescription() ?? d.ToString());
    }

    private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
    {
        string typeString = ComboBoxDictType.SelectionBoxItem.ToString()!;
        DictType selectedDictType = typeString.GetEnum<DictType>();

        switch (selectedDictType)
        {
            // not providing a description for the filter causes the filename returned to be empty
            case DictType.Kenkyuusha:
                BrowseForDictionaryFolder();
                break;
            case DictType.Daijirin:
                BrowseForDictionaryFolder();
                break;
            case DictType.Daijisen:
                BrowseForDictionaryFolder();
                break;
            case DictType.Koujien:
                BrowseForDictionaryFolder();
                break;
            case DictType.Meikyou:
                BrowseForDictionaryFolder();
                break;
            case DictType.Gakken:
                BrowseForDictionaryFolder();
                break;
            case DictType.Kotowaza:
                BrowseForDictionaryFolder();
                break;
            case DictType.Kanjium:
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
            default:
                throw new ArgumentOutOfRangeException(null, "Invalid DictType (Add)");
        }
    }
}
