﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using JapaneseLookup.EDICT;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using HandyControl.Tools;
using HandyControl.Controls;
using HandyControl.Properties;
using JapaneseLookup.Anki;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PreferenceWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : System.Windows.Window
    {
        private static PreferencesWindow _instance;
        private bool _setAnkiConfig;

        public static PreferencesWindow Instance
        {
            get { return _instance ??= new PreferencesWindow(); }
        }

        public PreferencesWindow()
        {
            InitializeComponent();
        }

        private void ShowColorPicker(object sender, RoutedEventArgs e)
        {
            var picker = SingleOpenHelper.CreateControl<ColorPicker>();
            var window = new HandyControl.Controls.PopupWindow
            {
                PopupElement = picker,
            };
            picker.Canceled += delegate { window.Close(); };
            picker.Confirmed += delegate
            {
                ColorSetter((System.Windows.Controls.Button) sender, picker.SelectedBrush, window);
            };

            window.ShowDialog(picker, false);
        }

        private static void ColorSetter(System.Windows.Controls.Button sender, SolidColorBrush selectedColor,
            HandyControl.Controls.PopupWindow window)
        {
            sender.Background = selectedColor;
            window.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.SavePreferences(this);
            Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Collapsed;
        }

        private void UpdateJMdictButton_Click(object sender, RoutedEventArgs e)
        {
            ResourceUpdater.UpdateJMdict();
        }

        private void UpdateJMnedictButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(ResourceUpdater.UpdateJMnedict);
        }

        private void UpdateKanjidicButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(ResourceUpdater.UpdateKanjidic);
        }

        private async void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemTab = (System.Windows.Controls.TabItem) TabControl.SelectedItem;
            if (itemTab == null) return;

            switch (itemTab.Header)
            {
                case "Anki":
                    if (!_setAnkiConfig)
                    {
                        await SetPreviousMiningConfig();
                        if (MiningSetupComboBoxDeckNames.SelectedItem == null) await PopulateDeckAndModelNames();
                        _setAnkiConfig = true;
                    }

                    break;
                case "Dictionaries":
                    UpdateDictionariesDisplay();
                    break;
            }
        }

        #region MiningSetup

        private async Task SetPreviousMiningConfig()
        {
            try
            {
                var ankiConfig = await AnkiConfig.ReadAnkiConfig();
                if (ankiConfig == null) return;

                MiningSetupComboBoxDeckNames.ItemsSource = new List<string> { ankiConfig.DeckName };
                MiningSetupComboBoxDeckNames.SelectedIndex = 0;
                MiningSetupComboBoxModelNames.ItemsSource = new List<string> { ankiConfig.ModelName };
                MiningSetupComboBoxModelNames.SelectedIndex = 0;
                CreateFieldElements(ankiConfig.Fields);
            }
            catch (Exception e)
            {
                // config probably doesn't exist; no need to alert the user
                Debug.WriteLine(e);
                throw;
            }
        }

        private async Task PopulateDeckAndModelNames()
        {
            try
            {
                MiningSetupComboBoxDeckNames.ItemsSource = null;
                var deckNamesList =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetDeckNames()).Result.ToString()!);
                MiningSetupComboBoxDeckNames.ItemsSource = deckNamesList;

                MiningSetupComboBoxModelNames.ItemsSource = null;
                var modelNamesList =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelNames()).Result.ToString()!);
                MiningSetupComboBoxModelNames.ItemsSource = modelNamesList;
            }
            catch
            {
                Console.WriteLine("Error getting deck and model names");
                MiningSetupComboBoxDeckNames.ItemsSource = "";
                MiningSetupComboBoxModelNames.ItemsSource = "";
            }
        }

        private async void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            await PopulateDeckAndModelNames();
        }

        private async void MiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modelName = MiningSetupComboBoxModelNames.SelectionBoxItem.ToString();
                var fieldNames =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelFieldNames(modelName)).Result
                        .ToString()!);

                var fields =
                    fieldNames!.ToDictionary(fieldName => fieldName, _ => JLField.Nothing);

                CreateFieldElements(fields);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error getting fields from AnkiConnect");
                Debug.WriteLine(exception);
            }
        }

        private void CreateFieldElements(Dictionary<string, JLField> fields)
        {
            MiningSetupStackPanelFields.Children.Clear();
            try
            {
                foreach (var (fieldName, jlField) in fields)
                {
                    var stackPanel = new StackPanel();
                    var textBlockFieldName = new TextBlock { Text = fieldName };
                    var comboBoxJLFields = new System.Windows.Controls.ComboBox
                        { ItemsSource = Enum.GetValues(typeof(JLField)), SelectedItem = jlField };

                    stackPanel.Children.Add(textBlockFieldName);
                    stackPanel.Children.Add(comboBoxJLFields);
                    MiningSetupStackPanelFields.Children.Add(stackPanel);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error creating field elements");
                Debug.WriteLine(exception);
            }
        }

        private void MiningSetupButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deckName = MiningSetupComboBoxDeckNames.SelectionBoxItem.ToString();
                var modelName = MiningSetupComboBoxModelNames.SelectionBoxItem.ToString();

                var dict = new Dictionary<string, JLField>();
                foreach (StackPanel stackPanel in MiningSetupStackPanelFields.Children)
                {
                    var textBlock = (TextBlock) stackPanel.Children[0];
                    var comboBox = (System.Windows.Controls.ComboBox) stackPanel.Children[1];

                    if (Enum.TryParse<JLField>(comboBox.SelectionBoxItem.ToString(), out var result))
                    {
                        dict.Add(textBlock.Text, result);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                var fields = dict;
                var tags = new[] { "JapaneseLookup" };

                if (MiningSetupComboBoxDeckNames.SelectedItem == null ||
                    MiningSetupComboBoxModelNames.SelectedItem == null)
                {
                    Console.WriteLine("Incomplete config");
                    return;
                }

                var ankiConfig = new AnkiConfig(deckName, modelName, fields, tags);
                Console.WriteLine(AnkiConfig.WriteAnkiConfig(ankiConfig).Result == "ok"
                    ? "Saved config"
                    : "Error saving config");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error saving config");
                Debug.WriteLine(exception);
            }
        }

        #endregion

        #region Dictionaries

        // probably should be split into several methods
        private void UpdateDictionariesDisplay()
        {
            List<DockPanel> resultDockPanels = new();

            foreach ((DictType _, Dict dict) in Dicts.dicts)
            {
                var dockPanel = new DockPanel();

                var checkBox = new CheckBox()
                {
                    Width = 20,
                    IsChecked = dict.Active,
                    Margin = new Thickness(10),
                };
                var dictTypeDisplay = new TextBlock()
                {
                    Width = 100,
                    Text = dict.Type.ToString(),
                    Margin = new Thickness(10),
                };
                var dictPathDisplay = new TextBlock()
                {
                    Width = 210,
                    Text = dict.Path,
                    Margin = new Thickness(10),
                };

                // should be a red cross ideally
                var buttonRemove = new Button()
                {
                    Width = 70,
                    Content = "Remove",
                    Background = Brushes.Red,
                    Margin = new Thickness(10),
                };

                // yeah, dunno about this
                checkBox.Unchecked += (sender, args) => dict.Active = false;
                checkBox.Checked += (sender, args) => dict.Active = true;
                buttonRemove.Click += (sender, args) =>
                {
                    if (System.Windows.MessageBox.Show("Really remove dictionary?", "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No,
                        MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                    {
                        Dicts.dicts.Remove(dict.Type);
                        UpdateDictionariesDisplay();
                    }
                };

                dockPanel.Children.Add(checkBox);
                dockPanel.Children.Add(dictTypeDisplay);
                dockPanel.Children.Add(dictPathDisplay);
                dockPanel.Children.Add(buttonRemove);

                resultDockPanels.Add(dockPanel);
            }

            // TODO: AddDictionaryWindow
            List<DictType> allDictTypes = Enum.GetValues(typeof(DictType)).Cast<DictType>().ToList();
            List<DictType> loadedDictTypes = Dicts.dicts.Keys.ToList();
            ComboBoxAddDictionary.ItemsSource = allDictTypes.Except(loadedDictTypes);
            DictionariesDisplay.ItemsSource = resultDockPanels;
        }

        private void BrowseForDictionaryFile(DictType selectedDictType, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                InitialDirectory = ConfigManager.ApplicationPath,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var relativePath = Path.GetRelativePath(ConfigManager.ApplicationPath, openFileDialog.FileName);
                Dicts.dicts.Add(selectedDictType, new Dict(selectedDictType, relativePath, true));
                Dicts.dicts[selectedDictType].Contents = new Dictionary<string, List<IResult>>();
                UpdateDictionariesDisplay();
            }
        }

        // could get rid of this and make users select the index.json file for EPWING dictionaries
        private void BrowseForDictionaryFolder(DictType selectedDictType)
        {
            using var fbd = new FolderBrowserDialog()
            {
                SelectedPath = ConfigManager.ApplicationPath
            };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
                !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                var relativePath = Path.GetRelativePath(ConfigManager.ApplicationPath, fbd.SelectedPath);
                Dicts.dicts.Add(selectedDictType, new Dict(selectedDictType, relativePath, true));
                Dicts.dicts[selectedDictType].Contents = new Dictionary<string, List<IResult>>();
                UpdateDictionariesDisplay();
            }
        }

        private void ButtonAddDictionary_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Shouldn't need this if done properly w/ a dedicated window
            if (ComboBoxAddDictionary.SelectionBoxItem.ToString() == "") return;

            var selectedDictType =
                Enum.Parse<DictType>(ComboBoxAddDictionary.SelectionBoxItem.ToString() ??
                                     throw new InvalidOperationException());

            switch (selectedDictType)
            {
                case DictType.JMdict:
                    // not providing a description for the filter causes the filename returned to be empty, lmfao microsoft
                    // BrowseForDictionaryFile(selectedDictType, "|JMdict.xml");
                    BrowseForDictionaryFile(selectedDictType, "JMdict file|JMdict.xml");
                    break;
                case DictType.JMnedict:
                    BrowseForDictionaryFile(selectedDictType, "JMnedict file|JMnedict.xml");
                    break;
                case DictType.Kanjidic:
                    BrowseForDictionaryFile(selectedDictType, "Kanjidic2 file|Kanjidic2.xml");
                    break;
                case DictType.UnknownEpwing:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Daijirin:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Daijisen:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Kojien:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Meikyou:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}