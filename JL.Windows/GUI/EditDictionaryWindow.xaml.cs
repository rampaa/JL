using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Utilities;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for EditDictionaryWindow.xaml
    /// </summary>
    public partial class EditDictionaryWindow : Window
    {
        private readonly Dict _oldDict;

        public EditDictionaryWindow(Dict oldDict)
        {
            _oldDict = oldDict;
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;

            string typeString = ComboBoxDictType.SelectionBoxItem.ToString();

            string path = TextBlockPath.Text;
            if (string.IsNullOrEmpty(path) || (!Directory.Exists(path) && !File.Exists(path)))
            {
                TextBlockPath.BorderBrush = Brushes.Red;
                isValid = false;
            }
            else if (TextBlockPath.BorderBrush == Brushes.Red)
            {
                TextBlockPath.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF3F3F46");
            }

            if (isValid)
            {
                //todo this will break on DictTypes without descriptions
                DictType type = typeString.GetEnum<DictType>();

                if (Storage.Dicts[type].Type != type || Storage.Dicts[type].Path != path)
                {
                    Storage.Dicts[type].Type = type;
                    Storage.Dicts[type].Path = path;
                    Storage.Dicts[type].Contents = new Dictionary<string, List<IResult>>();
                }

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
            string type = _oldDict.Type.GetDescription() ?? _oldDict.Type.ToString();
            ComboBoxDictType.Items.Add(type);
            ComboBoxDictType.SelectedValue = type;
            TextBlockPath.Text = _oldDict.Path;
        }

        private void BrowsePathButton_OnClick(object sender, RoutedEventArgs e)
        {
            string typeString = ComboBoxDictType.SelectionBoxItem.ToString();
            //todo this will break on DictTypes without descriptions
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
}
