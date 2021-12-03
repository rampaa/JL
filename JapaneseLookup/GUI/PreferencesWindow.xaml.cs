using HandyControl.Controls;
using HandyControl.Tools;
using JapaneseLookup.Abstract;
using JapaneseLookup.Anki;
using JapaneseLookup.Dicts;
using JapaneseLookup.EDICT;
using JapaneseLookup.EDICT.JMdict;
using JapaneseLookup.EDICT.JMnedict;
using JapaneseLookup.KANJIDIC;
using JapaneseLookup.PoS;
using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

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
            picker.Confirmed += delegate { ColorSetter((Button)sender, picker.SelectedBrush, window); };

            window.ShowDialog(picker, false);
        }

        private static void ColorSetter(Button sender, SolidColorBrush selectedColor,
            HandyControl.Controls.PopupWindow window)
        {
            sender.Background = selectedColor;
            window.Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
            await ConfigManager.SavePreferences(this).ConfigureAwait(false);
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

        private async void UpdateJMdictButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Ready = false;

            await ResourceUpdater.UpdateResource(ConfigManager.Dicts[DictType.JMdict].Path,
                new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                DictType.JMdict.ToString(), true, false)
                .ConfigureAwait(false);

            ConfigManager.Dicts[DictType.JMdict].Contents.Clear();

            await Task.Run(async () => await JMdictLoader
                .Load(ConfigManager.Dicts[DictType.JMdict].Path).ConfigureAwait(false));

            await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);

            JmdictWcLoader.WcDict.Clear();

            await JmdictWcLoader.Load().ConfigureAwait(false);

            if (!ConfigManager.Dicts[DictType.JMdict].Active)
                ConfigManager.Dicts[DictType.JMdict].Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);

            ConfigManager.Ready = true;
        }

        private async void UpdateJMnedictButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Ready = false;

            await ResourceUpdater.UpdateResource(ConfigManager.Dicts[DictType.JMnedict].Path,
                new Uri("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz"),
                DictType.JMnedict.ToString(), true, false)
                .ConfigureAwait(false);

            ConfigManager.Dicts[DictType.JMnedict].Contents.Clear();

            await Task.Run(async () => await JMnedictLoader
                .Load(ConfigManager.Dicts[DictType.JMnedict].Path).ConfigureAwait(false));

            if (!ConfigManager.Dicts[DictType.JMnedict].Active)
                ConfigManager.Dicts[DictType.JMnedict].Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);

            ConfigManager.Ready = true;
        }

        private async void UpdateKanjidicButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Ready = false;

            await ResourceUpdater.UpdateResource(ConfigManager.Dicts[DictType.Kanjidic].Path,
                new Uri("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz"),
                DictType.Kanjidic.ToString(), true, false)
                .ConfigureAwait(false);

            ConfigManager.Dicts[DictType.Kanjidic].Contents.Clear();

            await Task.Run(async () => await KanjiInfoLoader
                .Load(ConfigManager.Dicts[DictType.Kanjidic].Path).ConfigureAwait(false));

            if (!ConfigManager.Dicts[DictType.Kanjidic].Active)
                ConfigManager.Dicts[DictType.Kanjidic].Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);

            ConfigManager.Ready = true;
        }

        private async void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemTab = (System.Windows.Controls.TabItem)TabControl.SelectedItem;
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
                Utils.logger.Information(e, "AnkiConfig does not exist, probably.");
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
                Utils.logger.Information("Error getting deck and model names");
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
                Utils.logger.Information(exception, "Error getting fields from AnkiConnect");
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
                Utils.logger.Information(exception, "Error creating field elements");
            }
        }

        private async void MiningSetupButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deckName = MiningSetupComboBoxDeckNames.SelectionBoxItem.ToString();
                var modelName = MiningSetupComboBoxModelNames.SelectionBoxItem.ToString();

                var dict = new Dictionary<string, JLField>();
                foreach (StackPanel stackPanel in MiningSetupStackPanelFields.Children)
                {
                    var textBlock = (TextBlock)stackPanel.Children[0];
                    var comboBox = (System.Windows.Controls.ComboBox)stackPanel.Children[1];

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
                    Utils.logger.Information("Incomplete config");
                    return;
                }

                var ankiConfig = new AnkiConfig(deckName, modelName, fields, tags);
                Console.WriteLine(await AnkiConfig.WriteAnkiConfig(ankiConfig).ConfigureAwait(false)
                    ? "Saved config"
                    : "Error saving config");
            }
            catch (Exception exception)
            {
                Utils.logger.Information(exception, "Error saving Anki config");
            }
        }

        #endregion

        #region Dictionaries

        // probably should be split into several methods
        private void UpdateDictionariesDisplay()
        {
            List<DockPanel> resultDockPanels = new();

            foreach ((DictType _, Dict dict) in ConfigManager.Dicts)
            {
                var dockPanel = new DockPanel();

                var checkBox = new CheckBox()
                {
                    Width = 20,
                    IsChecked = dict.Active,
                    Margin = new Thickness(10),
                };
                var buttonIncreasePriority = new Button()
                {
                    Width = 25,
                    Content = "↑",
                    Margin = new Thickness(1),
                };
                var buttonDecreasePriority = new Button()
                {
                    Width = 25,
                    Content = "↓",
                    Margin = new Thickness(1),
                };
                var priority = new TextBlock()
                {
                    Name = "priority",
                    // Width = 20,
                    Width = 0,
                    Text = dict.Priority.ToString(),
                    // Margin = new Thickness(10),
                };
                var dictTypeDisplay = new TextBlock()
                {
                    Width = 135,
                    Text = dict.Type.ToString(),
                    Margin = new Thickness(10),
                };
                var dictPathValidityDisplay = new TextBlock()
                {
                    Width = 12,
                    Text = (Directory.Exists(dict.Path) || File.Exists(dict.Path)) ? "" : "❌",
                    Margin = new Thickness(1),
                };
                var dictPathDisplay = new TextBlock()
                {
                    Width = 200,
                    Text = dict.Path,
                    Margin = new Thickness(10),
                    Cursor = Cursors.Hand
                };

                dictPathDisplay.PreviewMouseLeftButtonUp += PathTextbox_PreviewMouseLeftButtonUp;
                dictPathDisplay.MouseEnter += (_, _) => dictPathDisplay.TextDecorations = TextDecorations.Underline;
                dictPathDisplay.MouseLeave += (_, _) => dictPathDisplay.TextDecorations = null;

                var buttonRemove = new Button { Width = 0 };
                if (!ConfigManager.BuiltInDicts.Values.Select(t => t.Type).ToList().Contains(dict.Type))
                {
                    buttonRemove = new Button()
                    {
                        Width = 30,
                        Height = 30,
                        Content = "X",
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Red,
                        BorderThickness = new Thickness(1),
                    };
                }

                checkBox.Unchecked += (_, _) => dict.Active = false;
                checkBox.Checked += (_, _) => dict.Active = true;
                buttonIncreasePriority.Click += (_, _) =>
                {
                    PrioritizeDict(ConfigManager.Dicts, dict.Type);
                    UpdateDictionariesDisplay();
                };
                buttonDecreasePriority.Click += (_, _) =>
                {
                    UnPrioritizeDict(ConfigManager.Dicts, dict.Type);
                    UpdateDictionariesDisplay();
                };
                buttonRemove.Click += (_, _) =>
                {
                    if (System.Windows.MessageBox.Show("Really remove dictionary?", "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No,
                        MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                    {
                        ConfigManager.Dicts.Remove(dict.Type);
                        UpdateDictionariesDisplay();
                    }
                };

                dockPanel.Children.Add(checkBox);
                dockPanel.Children.Add(buttonIncreasePriority);
                dockPanel.Children.Add(buttonDecreasePriority);
                dockPanel.Children.Add(priority);
                dockPanel.Children.Add(dictTypeDisplay);
                dockPanel.Children.Add(dictPathValidityDisplay);
                dockPanel.Children.Add(dictPathDisplay);
                dockPanel.Children.Add(buttonRemove);

                resultDockPanels.Add(dockPanel);
            }

            // TODO: AddDictionaryWindow
            List<DictType> allDictTypes = Enum.GetValues(typeof(DictType)).Cast<DictType>().ToList();
            List<DictType> loadedDictTypes = ConfigManager.Dicts.Keys.ToList();
            ComboBoxAddDictionary.ItemsSource = allDictTypes.Except(loadedDictTypes);
            DictionariesDisplay.ItemsSource = resultDockPanels.OrderBy(dockPanel =>
                dockPanel.Children
                    .OfType<TextBlock>()
                    .Where(textBlock => textBlock.Name == "priority")
                    .Select(textBlockPriority => Convert.ToInt32(textBlockPriority.Text)).First());
        }

        private void PathTextbox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string path = ((TextBlock)sender).Text;

            if (File.Exists(path) || Directory.Exists(path))
            {
                if (File.Exists(path)) 
                    path = Path.GetDirectoryName(path);

                Process.Start("explorer.exe", path ?? throw new InvalidOperationException());
            }
        }

        private static void PrioritizeDict(Dictionary<DictType, Dict> dicts, DictType typeToBePrioritized)
        {
            if (ConfigManager.Dicts[typeToBePrioritized].Priority == 0) return;

            dicts.Single(dict => dict.Value.Priority == ConfigManager.Dicts[typeToBePrioritized].Priority - 1).Value
                .Priority += 1;
            ConfigManager.Dicts[typeToBePrioritized].Priority -= 1;
        }

        private static void UnPrioritizeDict(Dictionary<DictType, Dict> dicts, DictType typeToBeUnPrioritized)
        {
            // lowest priority means highest number
            int lowestPriority = ConfigManager.Dicts.Select(dict => dict.Value.Priority).Max();
            if (ConfigManager.Dicts[typeToBeUnPrioritized].Priority == lowestPriority) return;

            dicts.Single(dict => dict.Value.Priority == ConfigManager.Dicts[typeToBeUnPrioritized].Priority + 1).Value
                .Priority -= 1;
            ConfigManager.Dicts[typeToBeUnPrioritized].Priority += 1;
        }

        private void BrowseForDictionaryFile(DictType selectedDictType, string filter)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = ConfigManager.ApplicationPath,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // lowest priority means highest number
                int lowestPriority = ConfigManager.Dicts.Select(dict => dict.Value.Priority).Max();

                var relativePath = Path.GetRelativePath(ConfigManager.ApplicationPath, openFileDialog.FileName);
                ConfigManager.Dicts.Add(selectedDictType,
                    new Dict(selectedDictType, relativePath, true, lowestPriority + 1));
                ConfigManager.Dicts[selectedDictType].Contents = new Dictionary<string, List<IResult>>();
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
                // lowest priority means highest number
                int lowestPriority = ConfigManager.Dicts.Select(dict => dict.Value.Priority).Max();

                var relativePath = Path.GetRelativePath(ConfigManager.ApplicationPath, fbd.SelectedPath);
                ConfigManager.Dicts.Add(selectedDictType,
                    new Dict(selectedDictType, relativePath, true, lowestPriority + 1));
                ConfigManager.Dicts[selectedDictType].Contents = new Dictionary<string, List<IResult>>();
                UpdateDictionariesDisplay();
            }
        }

        private void ButtonAddDictionary_OnClick(object sender, RoutedEventArgs e)
        {
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
                case DictType.CustomWordDictionary:
                    BrowseForDictionaryFile(selectedDictType, "CustomWordDict file|custom_words.txt");
                    break;
                case DictType.CustomNameDictionary:
                    BrowseForDictionaryFile(selectedDictType, "CustomNameDict file|custom_names.txt");
                    break;
                case DictType.Kenkyuusha:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Daijirin:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Daijisen:
                    BrowseForDictionaryFolder(selectedDictType);
                    break;
                case DictType.Koujien:
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

        private void KeyGestureToText(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            if (key == Key.LeftShift || key == Key.RightShift
                                     || key == Key.LeftCtrl || key == Key.RightCtrl
                                     || key == Key.LeftAlt || key == Key.RightAlt
                                     || key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            StringBuilder hotkeyTextBuilder = new();

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                hotkeyTextBuilder.Append("Ctrl+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                hotkeyTextBuilder.Append("Shift+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                hotkeyTextBuilder.Append("Alt+");
            }

            hotkeyTextBuilder.Append(key.ToString());

            ((TextBox)sender).Text = hotkeyTextBuilder.ToString();
        }

        private void MiningModeKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            MiningModeKeyGestureTextBox.Text = "None";
        }

        private void KanjiModeKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            KanjiModeKeyGestureTextBox.Text = "None";
        }

        private void MousePassThroughModeKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            MousePassThroughModeKeyGestureTextBox.Text = "None";
        }

        private void PlayAudioKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            PlayAudioKeyGestureTextBox.Text = "None";
        }

        private void ShowPreferencesWindowKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPreferencesWindowKeyGestureTextBox.Text = "None";
        }

        private void ShowAddNameWindowKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAddNameWindowKeyGestureTextBox.Text = "None";
        }

        private void ShowAddWordWindowKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAddWordWindowKeyGestureTextBox.Text = "None";
        }

        private void SearchWithBrowserKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            SearchWithBrowserKeyGestureTextBox.Text = "None";
        }

        private void SteppedBacklogBackwardsKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            SteppedBacklogBackwardsKeyGestureTextBox.Text = "None";
        }

        private void SteppedBacklogForwardsKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            SteppedBacklogForwardsKeyGestureTextBox.Text = "None";
        }

        private void InactiveLookupModeKeyGestureTextBoxButton_Click(object sender, RoutedEventArgs e)
        {
            InactiveLookupModeKeyGestureTextBox.Text = "None";
        }
    }
}