using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using JapaneseLookup.Anki;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for MiningSetupWindow.xaml
    /// </summary>
    public partial class MiningSetupWindow : Window
    {
        private static MiningSetupWindow _instance;

        public static MiningSetupWindow Instance
        {
            get { return _instance ??= new MiningSetupWindow(); }
        }

        // TODO: Make this window close too if the MainWindow is closed
        public MiningSetupWindow()
        {
            InitializeComponent();
            SetPreviousConfig();
            if (ComboBoxDeckNames.SelectedItem == null) PopulateDeckandModelNames();
        }

        private async void SetPreviousConfig()
        {
            try
            {
                var ankiConfig = await AnkiConfig.ReadAnkiConfig();
                if (ankiConfig == null) return;

                ComboBoxDeckNames.ItemsSource = new List<string> {ankiConfig.deckName};
                ComboBoxDeckNames.SelectedIndex = 0;
                ComboBoxModelNames.ItemsSource = new List<string> {ankiConfig.modelName};
                ComboBoxModelNames.SelectedIndex = 0;
                CreateFieldElements(ankiConfig.fields);
            }
            catch (Exception e)
            {
                // config probably doesn't exist; no need to alert the user
                Debug.WriteLine(e);
                throw;
            }
        }

        private async void PopulateDeckandModelNames()
        {
            try
            {
                var deckNamesList =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetDeckNames()).result.ToString()!);
                ComboBoxDeckNames.ItemsSource = deckNamesList;

                var modelNamesList =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelNames()).result.ToString()!);
                ComboBoxModelNames.ItemsSource = modelNamesList;
            }
            catch
            {
                Console.WriteLine("Error getting deck and model names");
                ComboBoxDeckNames.ItemsSource = "";
                ComboBoxModelNames.ItemsSource = "";
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateDeckandModelNames();
        }

        private async void ButtonGetFields_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modelName = ComboBoxModelNames.SelectionBoxItem.ToString();
                var fieldNames =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelFieldNames(modelName)).result
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
            StackPanelFields.Children.Clear();
            try
            {
                foreach (var (fieldName, jlField) in fields)
                {
                    var stackPanel = new StackPanel();
                    var textBlockFieldName = new TextBlock {Text = fieldName};
                    var comboBoxJLFields = new ComboBox
                        {ItemsSource = Enum.GetValues(typeof(JLField)), SelectedItem = jlField};

                    stackPanel.Children.Add(textBlockFieldName);
                    stackPanel.Children.Add(comboBoxJLFields);
                    StackPanelFields.Children.Add(stackPanel);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error creating field elements");
                Debug.WriteLine(exception);
            }
        }

        // TODO: tags
        // TODO: Make sure everything is filled before enabling the save button
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deckName = ComboBoxDeckNames.SelectionBoxItem.ToString();
                var modelName = ComboBoxModelNames.SelectionBoxItem.ToString();

                var dict = new Dictionary<string, JLField>();
                foreach (StackPanel stackPanel in StackPanelFields.Children)
                {
                    var textBlock = (TextBlock) stackPanel.Children[0];
                    var comboBox = (ComboBox) stackPanel.Children[1];

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
                var tags = new[] {"JL"};

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
    }
}