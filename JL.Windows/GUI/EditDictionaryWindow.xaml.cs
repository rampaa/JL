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
/// Interaction logic for EditDictionaryWindow.xaml
/// </summary>
public partial class EditDictionaryWindow : Window
{
    private readonly Dict _dict;

    public EditDictionaryWindow(Dict dict)
    {
        _dict = dict;
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

        string typeString = ComboBoxDictType.SelectionBoxItem.ToString()!;

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
            DictType type = typeString.GetEnum<DictType>();

            if (Storage.Dicts[type].Path != path)
            {
                Storage.Dicts[type].Path = path;
                Storage.Dicts[type].Contents = new Dictionary<string, List<IResult>>();
            }

            NewlineBetweenDefinitionsOption? newlineOption = null;
            if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(type))
            {
                newlineOption = new NewlineBetweenDefinitionsOption { Value = CheckBoxNewline.IsChecked!.Value };
            }

            ExamplesOption? examplesOption = null;
            if (ExamplesOption.ValidDictTypes.Contains(type))
            {
                if (Enum.TryParse(ComboBoxExamples.SelectedValue?.ToString(), out ExamplesOptionValue eov))
                    examplesOption = new ExamplesOption { Value = eov };
            }

            RequireKanjiModeOption? kanjiOption = null;
            if (RequireKanjiModeOption.ValidDictTypes.Contains(type))
            {
                kanjiOption = new RequireKanjiModeOption { Value = CheckBoxKanji.IsChecked!.Value };
            }

            var options =
                new DictOptions(
                    newlineOption,
                    examplesOption,
                    kanjiOption);

            Storage.Dicts[type].Options = options;

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
        string type = _dict.Type.GetDescription() ?? _dict.Type.ToString();
        ComboBoxDictType.Items.Add(type);
        ComboBoxDictType.SelectedValue = type;
        TextBlockPath.Text = _dict.Path;
        GenerateDictOptionsElements(_dict);
    }

    private void GenerateDictOptionsElements(Dict dict)
    {
        if (NewlineBetweenDefinitionsOption.ValidDictTypes.Contains(dict.Type))
        {
            bool isEpwing = !Storage.BuiltInDicts.ContainsKey(dict.Type.ToString());
            CheckBoxNewline.IsChecked = dict.Options?.NewlineBetweenDefinitions?.Value ?? isEpwing;
            DockPanelNewline.Visibility = Visibility.Visible;
        }

        if (ExamplesOption.ValidDictTypes.Contains(dict.Type))
        {
            ComboBoxExamples.ItemsSource = Enum.GetValues<ExamplesOptionValue>().ToArray();
            ComboBoxExamples.SelectedValue = dict.Options?.Examples?.Value ?? ExamplesOptionValue.All;

            DockPanelExamples.Visibility = Visibility.Visible;
        }

        if (RequireKanjiModeOption.ValidDictTypes.Contains(dict.Type))
        {
            CheckBoxKanji.IsChecked = dict.Options?.RequireKanjiMode?.Value ?? false;
            DockPanelKanji.Visibility = Visibility.Visible;
        }

        if (DockPanelNewline.Visibility == Visibility.Visible ||
            DockPanelExamples.Visibility == Visibility.Visible ||
            DockPanelKanji.Visibility == Visibility.Visible)
        {
            StackPanelOptions.Visibility = Visibility.Visible;
        }
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
                BrowseForDictionaryFile("CustomWordDict file|custom_words.txt");
                break;
            case DictType.CustomNameDictionary:
                BrowseForDictionaryFile("CustomNameDict file|custom_names.txt");
                break;
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
                throw new ArgumentOutOfRangeException(null, "Invalid DictType (Edit)");
        }
    }
}
