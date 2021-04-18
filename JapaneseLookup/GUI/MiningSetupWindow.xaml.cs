using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // TODO: Make this window close too if the MainWindow is closed
        // TODO: Scrolling (test with like 100 fields)
        // maybe convert all the procedural GUI generation to xaml-based GUI generation
        public MiningSetupWindow()
        {
            InitializeComponent();
            PopulateDeckandModelNames();
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
                ComboBoxDeckNames.ItemsSource = string.Empty;
                ComboBoxModelNames.ItemsSource = string.Empty;
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateDeckandModelNames();
        }

        private void ButtonGetFields_Click(object sender, RoutedEventArgs e)
        {
            CreateFieldElements(ComboBoxModelNames.SelectionBoxItem.ToString());
        }

        private async void CreateFieldElements(string modelName)
        {
            StackPanelFields.Children.Clear();
            try
            {
                var fields =
                    JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelFieldNames(modelName)).result
                        .ToString()!);

                foreach (var field in fields!)
                {
                    var stackPanel = new StackPanel();
                    var textBlockField = new TextBlock {Text = field};
                    var comboBoxJLFields = new ComboBox {ItemsSource = Enum.GetValues(typeof(JLField))};

                    stackPanel.Children.Add(textBlockField);
                    stackPanel.Children.Add(comboBoxJLFields);
                    StackPanelFields.Children.Add(stackPanel);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting fields");
                Debug.WriteLine(e);
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
                    // there must be a better way of doing this
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
                AnkiConfig.WriteConfig(ankiConfig);
                Console.WriteLine("Saved config");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error saving config");
                Debug.WriteLine(exception);
            }
        }
    }
}