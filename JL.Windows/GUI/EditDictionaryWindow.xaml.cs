using System.IO;
using System.Windows;
using System.Windows.Forms;
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
                newlineOption = new NewlineBetweenDefinitionsOption { Value = NewlineCheckBox.IsChecked!.Value };
            }

            ExamplesOption? examplesOption = null;
            if (ExamplesOption.ValidDictTypes.Contains(type))
            {
                if (Enum.TryParse(ExamplesComboBox.SelectedValue?.ToString(), out ExamplesOptionValue eov))
                    examplesOption = new ExamplesOption { Value = eov };
            }

            RequireKanjiModeOption? kanjiOption = null;
            if (RequireKanjiModeOption.ValidDictTypes.Contains(type))
            {
                kanjiOption = new RequireKanjiModeOption { Value = RequireKanjiModeCheckBox.IsChecked!.Value };
            }

            WordClassInfoOption? wordClassOption = null;
            if (WordClassInfoOption.ValidDictTypes.Contains(type))
            {
                wordClassOption = new WordClassInfoOption { Value = WordClassInfoCheckBox.IsChecked!.Value };
            }

            DialectInfoOption? dialectOption = null;
            if (DialectInfoOption.ValidDictTypes.Contains(type))
            {
                dialectOption = new DialectInfoOption { Value = DialectInfoCheckBox.IsChecked!.Value };
            }

            POrthographyInfoOption? pOrthographyInfoOption = null;
            if (POrthographyInfoOption.ValidDictTypes.Contains(type))
            {
                pOrthographyInfoOption = new POrthographyInfoOption { Value = POrthographyInfoCheckBox.IsChecked!.Value };
            }

            POrthographyInfoColorOption? pOrthographyInfoColorOption = null;
            if (POrthographyInfoColorOption.ValidDictTypes.Contains(type))
            {
                pOrthographyInfoColorOption = new POrthographyInfoColorOption { Value = POrthographyInfoColorButton.Background.ToString() };
            }

            POrthographyInfoFontSizeOption? pOrthographyInfoFontSize = null;
            if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(type))
            {
                pOrthographyInfoFontSize = new POrthographyInfoFontSizeOption { Value = POrthographyInfoFontSizeNumericUpDown.Value };
            }

            AOrthographyInfoOption? aOrthographyInfoOption = null;
            if (AOrthographyInfoOption.ValidDictTypes.Contains(type))
            {
                aOrthographyInfoOption = new AOrthographyInfoOption { Value = AOrthographyInfoCheckBox.IsChecked!.Value };
            }

            ROrthographyInfoOption? rOrthographyInfoOption = null;
            if (ROrthographyInfoOption.ValidDictTypes.Contains(type))
            {
                rOrthographyInfoOption = new ROrthographyInfoOption { Value = ROrthographyInfoCheckBox.IsChecked!.Value };
            }

            WordTypeInfoOption? wordTypeOption = null;
            if (WordTypeInfoOption.ValidDictTypes.Contains(type))
            {
                wordTypeOption = new WordTypeInfoOption { Value = WordTypeInfoCheckBox.IsChecked!.Value };
            }

            SpellingRestrictionInfoOption? spellingRestrictionInfo = null;
            if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(type))
            {
                spellingRestrictionInfo = new SpellingRestrictionInfoOption { Value = SpellingRestrictionInfoCheckBox.IsChecked!.Value };
            }

            ExtraDefinitionInfoOption? extraDefinitionInfo = null;
            if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(type))
            {
                extraDefinitionInfo = new ExtraDefinitionInfoOption { Value = ExtraDefinitionInfoCheckBox.IsChecked!.Value };
            }

            MiscInfoOption? miscInfoOption = null;
            if (MiscInfoOption.ValidDictTypes.Contains(type))
            {
                miscInfoOption = new MiscInfoOption { Value = MiscInfoCheckBox.IsChecked!.Value };
            }

            var options =
                new DictOptions(
                    newlineOption,
                    examplesOption,
                    kanjiOption,
                    wordClassOption,
                    dialectOption,
                    pOrthographyInfoOption,
                    pOrthographyInfoColorOption,
                    pOrthographyInfoFontSize,
                    aOrthographyInfoOption,
                    rOrthographyInfoOption,
                    wordTypeOption,
                    spellingRestrictionInfo,
                    extraDefinitionInfo,
                    miscInfoOption
                    );

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
            NewlineCheckBox.IsChecked = dict.Options?.NewlineBetweenDefinitions?.Value ?? isEpwing;
            NewlineCheckBox.Visibility = Visibility.Visible;
        }

        if (ExamplesOption.ValidDictTypes.Contains(dict.Type))
        {
            ExamplesComboBox.ItemsSource = Enum.GetValues<ExamplesOptionValue>().ToArray();
            ExamplesComboBox.SelectedValue = dict.Options?.Examples?.Value ?? ExamplesOptionValue.All;

            ExamplesComboBox.Visibility = Visibility.Visible;
        }

        if (RequireKanjiModeOption.ValidDictTypes.Contains(dict.Type))
        {
            RequireKanjiModeCheckBox.IsChecked = dict.Options?.RequireKanjiMode?.Value ?? false;
            RequireKanjiModeCheckBox.Visibility = Visibility.Visible;
        }

        if (WordClassInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            WordClassInfoCheckBox.IsChecked = dict.Options?.WordClassInfo?.Value ?? true;
            WordClassInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (DialectInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            DialectInfoCheckBox.IsChecked = dict.Options?.DialectInfo?.Value ?? true;
            DialectInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoCheckBox.IsChecked = dict.Options?.POrthographyInfo?.Value ?? true;
            POrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoColorOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoColorButton.Background = (SolidColorBrush)new BrushConverter()
                   .ConvertFrom(dict.Options?.POrthographyInfoColor?.Value ?? ConfigManager.PrimarySpellingColor.ToString())!;

            POrthographyInfoColorDockPanel.Visibility = Visibility.Visible;
        }

        if (POrthographyInfoFontSizeOption.ValidDictTypes.Contains(dict.Type))
        {
            POrthographyInfoFontSizeNumericUpDown.Value = dict.Options?.POrthographyInfoFontSize?.Value ?? 15;
            POrthographyInfoFontSizeDockPanel.Visibility = Visibility.Visible;
        }

        if (AOrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            AOrthographyInfoCheckBox.IsChecked = dict.Options?.AOrthographyInfo?.Value ?? true;
            AOrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (ROrthographyInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            ROrthographyInfoCheckBox.IsChecked = dict.Options?.ROrthographyInfo?.Value ?? true;
            ROrthographyInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (WordTypeInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            WordTypeInfoCheckBox.IsChecked = dict.Options?.WordTypeInfo?.Value ?? true;
            WordTypeInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (SpellingRestrictionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            SpellingRestrictionInfoCheckBox.IsChecked = dict.Options?.SpellingRestrictionInfo?.Value ?? true;
            SpellingRestrictionInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (ExtraDefinitionInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            ExtraDefinitionInfoCheckBox.IsChecked = dict.Options?.ExtraDefinitionInfo?.Value ?? true;
            ExtraDefinitionInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (MiscInfoOption.ValidDictTypes.Contains(dict.Type))
        {
            MiscInfoCheckBox.IsChecked = dict.Options?.MiscInfo?.Value ?? true;
            MiscInfoCheckBox.Visibility = Visibility.Visible;
        }

        if (NewlineCheckBox.Visibility == Visibility.Visible
            || ExamplesComboBox.Visibility == Visibility.Visible
            || WordClassInfoCheckBox.Visibility == Visibility.Visible
            //|| DialectInfoCheckBox.Visibility == Visibility.Visible
            //|| POrthographyInfoCheckBox.Visibility == Visibility.Visible
            //|| POrthographyInfoColorDockPanel.Visibility == Visibility.Visible
            //|| POrthographyInfoFontSizeNumericUpDown.Visibility == Visibility.Visible
            //|| AOrthographyInfoCheckBox.Visibility == Visibility.Visible
            //|| ROrthographyInfoCheckBox.Visibility == Visibility.Visible
            //|| WordTypeInfoCheckBox.Visibility == Visibility.Visible
            //|| SpellingRestrictionInfoCheckBox.Visibility == Visibility.Visible
            //|| ExtraDefinitionInfoCheckBox.Visibility == Visibility.Visible
            //|| MiscInfoCheckBox.Visibility == Visibility.Visible
            )
        {
            OptionsTextBlock.Visibility = Visibility.Visible;
            OptionsStackPanel.Visibility = Visibility.Visible;
        }
    }

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker(sender, e);
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
