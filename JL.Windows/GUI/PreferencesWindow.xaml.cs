using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PreferenceWindow.xaml
/// </summary>
public partial class PreferencesWindow : Window
{
    private static PreferencesWindow? s_instance;
    public bool SetAnkiConfig { get; private set; } = false;

    public static PreferencesWindow Instance
    {
        get { return s_instance ??= new(); }
    }

    public PreferencesWindow()
    {
        InitializeComponent();
    }

    #region EventHandlers

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker(sender, e);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        WindowsUtils.HideWindow(this);
        Storage.Frontend.InvalidateDisplayCache();
        await ConfigManager.Instance.SavePreferences(this).ConfigureAwait(false);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        WindowsUtils.HideWindow(this);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        WindowsUtils.HideWindow(this);
    }

    private async void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source.Equals(AnkiTabItem))
        {
            if (ConfigManager.AnkiIntegration && !SetAnkiConfig)
            {
                await SetPreviousMiningConfig();
                await PopulateDeckAndModelNames();

                SetAnkiConfig = true;
            }
        }
    }

    private async void CheckForJLUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckForJLUpdatesButton.IsEnabled = false;
        await Networking.CheckForJLUpdates(false);
        CheckForJLUpdatesButton.IsEnabled = true;
    }

    #endregion

    #region MiningSetup

    private async Task SetPreviousMiningConfig()
    {
        try
        {
            Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig();

            if (ankiConfigDict == null)
                return;

            AnkiConfig? wordAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Word);
            AnkiConfig? kanjiAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Kanji);
            AnkiConfig? nameAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Name);
            AnkiConfig? otherAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Other);


            if (wordAnkiConfig != null)
            {
                SetPreviousMiningConfig(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordTagsTextBox, wordAnkiConfig);
                CreateFieldElements(wordAnkiConfig.Fields, Storage.JLFieldsForWordDicts, WordMiningSetupStackPanelFields);
            }

            if (kanjiAnkiConfig != null)
            {
                SetPreviousMiningConfig(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiTagsTextBox, kanjiAnkiConfig);
                CreateFieldElements(kanjiAnkiConfig.Fields, Storage.JLFieldsForKanjiDicts, KanjiMiningSetupStackPanelFields);
            }

            if (nameAnkiConfig != null)
            {
                SetPreviousMiningConfig(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameTagsTextBox, nameAnkiConfig);
                CreateFieldElements(nameAnkiConfig.Fields, Storage.JLFieldsForNameDicts, NameMiningSetupStackPanelFields);
            }

            if (otherAnkiConfig != null)
            {
                SetPreviousMiningConfig(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherTagsTextBox, otherAnkiConfig);
                CreateFieldElements(otherAnkiConfig.Fields, Enum.GetValues<JLField>().ToList(), OtherMiningSetupStackPanelFields);
            }
        }
        catch (Exception e)
        {
            // config probably doesn't exist; no need to alert the user
            Utils.Logger.Warning(e, "Error setting previous mining config");
        }
    }

    private static void SetPreviousMiningConfig(ComboBox deckNamesComboBox, ComboBox modelNamesComboBox, TextBox tagTextBox, AnkiConfig ankiConfig)
    {
        deckNamesComboBox.ItemsSource = new List<string> { ankiConfig.DeckName };
        deckNamesComboBox.SelectedItem = ankiConfig.DeckName;
        modelNamesComboBox.ItemsSource = new List<string> { ankiConfig.ModelName };
        modelNamesComboBox.SelectedItem = ankiConfig.ModelName;
        tagTextBox.Text = string.Join(",", ankiConfig.Tags);
    }

    private async Task PopulateDeckAndModelNames()
    {
        List<string>? deckNames = await AnkiUtils.GetDeckNames();
        List<string>? modelNames = await AnkiUtils.GetModelNames();

        if (deckNames != null && modelNames != null)
        {
            WordMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToList();
            KanjiMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToList();
            NameMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToList();
            OtherMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToList();

            WordMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToList();
            KanjiMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToList();
            NameMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToList();
            OtherMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToList();
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error getting deck and model names form Anki");
            Utils.Logger.Error("Error getting deck and model names from Anki");

            //WordMiningSetupComboBoxDeckNames.ItemsSource = "";
            //KanjiMiningSetupComboBoxDeckNames.ItemsSource = "";
            //NameMiningSetupComboBoxDeckNames.ItemsSource = "";
            //OtherMiningSetupComboBoxDeckNames.ItemsSource = "";

            //WordMiningSetupComboBoxModelNames.ItemsSource = "";
            //KanjiMiningSetupComboBoxModelNames.ItemsSource = "";
            //NameMiningSetupComboBoxModelNames.ItemsSource = "";
            //OtherMiningSetupComboBoxModelNames.ItemsSource = "";
        }
    }

    private async void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
    {
        await PopulateDeckAndModelNames().ConfigureAwait(false);
    }

    private static async Task GetFields(ComboBox modelNamesComboBox, StackPanel miningStackPanel, List<JLField> fieldList)
    {
        string modelName = modelNamesComboBox.SelectionBoxItem.ToString()!;

        List<string>? fieldNames = await AnkiUtils.GetFieldNames(modelName);

        if (fieldNames != null)
        {
            Dictionary<string, JLField> fields =
                fieldNames.ToDictionary(fieldName => fieldName, _ => JLField.Nothing);

            CreateFieldElements(fields, fieldList, miningStackPanel);
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error getting fields from AnkiConnect");
            Utils.Logger.Error("Error getting fields from AnkiConnect");
        }
    }

    private async void WordMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, Storage.JLFieldsForWordDicts);
    }

    private async void KanjiMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, Storage.JLFieldsForKanjiDicts);
    }

    private async void NameMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, Storage.JLFieldsForNameDicts);
    }

    private async void OtherMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, Storage.AllJLFields);
    }

    private static void CreateFieldElements(Dictionary<string, JLField> fields, List<JLField> fieldList, StackPanel fieldStackPanel)
    {
        fieldStackPanel.Children.Clear();

        string[] descriptions = fieldList
            .Select(jlFieldName => jlFieldName.GetDescription() ?? jlFieldName.ToString()).ToArray();

        try
        {
            foreach ((string fieldName, JLField jlField) in fields)
            {
                StackPanel stackPanel = new();
                var textBlockFieldName = new TextBlock { Text = fieldName };
                var comboBoxJLFields = new ComboBox
                {
                    ItemsSource = descriptions,
                    SelectedItem = jlField.GetDescription() ?? jlField.ToString()
                };

                stackPanel.Children.Add(textBlockFieldName);
                stackPanel.Children.Add(comboBoxJLFields);
                fieldStackPanel.Children.Add(stackPanel);
            }
        }
        catch (Exception exception)
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error creating field elements for Anki setup");
            Utils.Logger.Error(exception, "Error creating field elements for Anki setup");
        }
    }

    private static AnkiConfig? GetAnkiConfigFromPreferences(ComboBox deckNamesComboBox, ComboBox modelNamesComboBox, StackPanel miningStackPanel, TextBox tagsTextBox, List<JLField> jlFieldList)
    {
        try
        {
            string deckName = deckNamesComboBox.SelectionBoxItem.ToString()!;
            string modelName = modelNamesComboBox.SelectionBoxItem.ToString()!;

            Dictionary<string, JLField> dict = new();
            foreach (StackPanel stackPanel in miningStackPanel.Children)
            {
                var textBlock = (TextBlock)stackPanel.Children[0];
                var comboBox = (ComboBox)stackPanel.Children[1];

                string selectedDescription = comboBox.SelectionBoxItem.ToString()!;
                JLField result = jlFieldList.FirstOrDefault(jlFieldName =>
                        (jlFieldName.GetDescription() ?? jlFieldName.ToString()) == selectedDescription, JLField.Nothing);

                dict.Add(textBlock.Text, result);
            }

            Dictionary<string, JLField> fields = dict;

            string rawTags = tagsTextBox.Text;
            string[] tags = string.IsNullOrEmpty(rawTags)
                ? Array.Empty<string>()
                : rawTags.Split(',').Select(s => s.Trim()).ToArray();

            if (deckNamesComboBox.SelectedItem == null ||
                modelNamesComboBox.SelectedItem == null)
            {
                WindowsUtils.Alert(AlertLevel.Error, "Save failed: Incomplete Anki config");
                Utils.Logger.Error("Save failed: Incomplete Anki config");
                return null;
            }

            return new AnkiConfig(deckName, modelName, fields, tags);
        }
        catch (Exception exception)
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error saving AnkiConfig");
            Utils.Logger.Error(exception, "Error saving AnkiConfig");
            return null;
        }
    }

    public async Task SaveMiningSetup()
    {
        if (!ConfigManager.AnkiIntegration)
            return;

        Dictionary<MineType, AnkiConfig> ankiConfigDict = new();

        AnkiConfig? ankiConfig = GetAnkiConfigFromPreferences(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, WordTagsTextBox, Storage.JLFieldsForWordDicts);
        if (ankiConfig != null)
            ankiConfigDict.Add(MineType.Word, ankiConfig);

        ankiConfig = GetAnkiConfigFromPreferences(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, KanjiTagsTextBox, Storage.JLFieldsForKanjiDicts);
        if (ankiConfig != null)
            ankiConfigDict.Add(MineType.Kanji, ankiConfig);

        ankiConfig = GetAnkiConfigFromPreferences(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, NameTagsTextBox, Storage.JLFieldsForNameDicts);
        if (ankiConfig != null)
            ankiConfigDict.Add(MineType.Name, ankiConfig);

        ankiConfig = GetAnkiConfigFromPreferences(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, OtherTagsTextBox, Storage.AllJLFields);
        if (ankiConfig != null)
            ankiConfigDict.Add(MineType.Other, ankiConfig);

        if (ankiConfigDict.Count > 0)
        {
            await AnkiConfig.WriteAnkiConfig(ankiConfigDict).ConfigureAwait(false);
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error saving AnkiConfig");
            Utils.Logger.Error("Error saving AnkiConfig");
        }
    }

    #endregion

    #region Keys

    private void KeyGestureToText(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

        if (key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        StringBuilder hotkeyTextBuilder = new();

        if (key == Key.LeftShift || key == Key.RightShift
            || key == Key.LeftCtrl || key == Key.RightCtrl
            || key == Key.LeftAlt || key == Key.RightAlt)
        {
            hotkeyTextBuilder.Append(key.ToString());
        }

        else
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                hotkeyTextBuilder.Append("Ctrl+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                hotkeyTextBuilder.Append("Alt+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 && hotkeyTextBuilder.Length > 0)
            {
                hotkeyTextBuilder.Append("Shift+");
            }

            hotkeyTextBuilder.Append(key.ToString());
        }

        ((TextBox)sender).Text = hotkeyTextBuilder.ToString();
    }

    private void ClearKeyGestureButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var dockPanel = (DockPanel)button.Parent;
        TextBox textBox = dockPanel.Children.OfType<TextBox>().ToArray()[0];
        textBox.Text = "None";
    }

    #endregion
}
