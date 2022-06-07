using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Utilities;
using JL.Windows.GUI.UserControls;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for EditDictionaryWindow.xaml
/// </summary>
public partial class EditDictionaryWindow : Window
{
    private readonly Dict _dict;

    private readonly DictOptionsControl _dictOptionsControl;

    public EditDictionaryWindow(Dict dict)
    {
        _dict = dict;
        _dictOptionsControl = new DictOptionsControl();
        InitializeComponent();
        StackPanel.Children.Add(_dictOptionsControl);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool isValid = true;

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

        string name = NameTextBox.Text;
        if (string.IsNullOrEmpty(name) || Storage.Dicts.Values.Count(dict => dict.Name == name) > 1)
        {
            NameTextBox.BorderBrush = Brushes.Red;
            isValid = false;
        }
        else if (NameTextBox.BorderBrush == Brushes.Red)
        {
            NameTextBox.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46")!;
        }

        if (isValid)
        {
            if (_dict.Path != path)
            {
                _dict.Path = path;
                _dict.Contents.Clear();
            }

            _dict.Name = name;

            Core.Dicts.Options.DictOptions options = _dictOptionsControl.GetDictOptions(_dict.Type);

            if (_dict.Options?.Examples?.Value != options?.Examples?.Value)
            {
                _dict.Contents.Clear();
            }

            _dict.Options = options;
            Storage.Frontend.InvalidateDisplayCache();

            Close();
        }
    }

    private void BrowseForDictionaryFile(string filter)
    {
        OpenFileDialog openFileDialog = new() { InitialDirectory = File.Exists(TextBlockPath.Text) ? new FileInfo(TextBlockPath.Text).Directory!.FullName : Storage.ApplicationPath, Filter = filter };

        if (openFileDialog.ShowDialog() == true)
        {
            string relativePath = Path.GetRelativePath(Storage.ApplicationPath, openFileDialog.FileName);
            TextBlockPath.Text = relativePath;
        }
    }

    private void BrowseForDictionaryFolder()
    {
        using var fbd = new FolderBrowserDialog { SelectedPath = Directory.Exists(TextBlockPath.Text) ? TextBlockPath.Text : Storage.ApplicationPath };

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
                BrowseForDictionaryFile("CustomWordDict file|custom_words.txt");
                break;
            case DictType.CustomNameDictionary:
                BrowseForDictionaryFile("CustomNameDict file|custom_names.txt");
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
            case DictType.NonspecificYomichan:
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
            case DictType.NonspecificNazeka:
                BrowseForDictionaryFile("Nazeka file|*.json");
                break;

            default:
                throw new ArgumentOutOfRangeException(null, "Invalid DictType (Edit)");
        }
    }
}
