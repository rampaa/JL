using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using JapaneseLookup.EDICT;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using HandyControl.Tools;
using HandyControl.Controls;
using HandyControl.Properties;
using JapaneseLookup.Anki;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PreferenceWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : System.Windows.Window
    {
        private static PreferencesWindow _instance;

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
            EdictUpdater.UpdateJMdict();
        }

        private void UpdateJMnedictButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(EdictUpdater.UpdateJMnedict);
        }

        #region MiningSetup

        private void TabItemAnki_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetPreviousMiningConfig();
            if (MiningSetupComboBoxDeckNames.SelectedItem == null) PopulateDeckAndModelNames();
        }

        private async void SetPreviousMiningConfig()
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

        private async void PopulateDeckAndModelNames()
        {
            try
            {
                var deckNamesList =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetDeckNames()).Result.ToString()!);
                MiningSetupComboBoxDeckNames.ItemsSource = deckNamesList;

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

        private void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateDeckAndModelNames();
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
    }
}