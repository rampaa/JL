using System.Configuration;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Dicts;
using JL.Core.Network;
using JL.Core.Profile;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PreferenceWindow.xaml
/// </summary>
internal sealed partial class PreferencesWindow : Window
{
    private static PreferencesWindow? s_instance;
    public static PreferencesWindow Instance => s_instance ??= new PreferencesWindow();
    public bool SetAnkiConfig { get; private set; } = false;
    private string _profileName;
    private readonly Dict _profileNamesDict;
    private readonly Dict _profileWordsDict;

    public PreferencesWindow()
    {
        InitializeComponent();
        _profileName = ProfileUtils.CurrentProfile;
        _profileNamesDict = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.ProfileCustomNameDictionary);
        _profileWordsDict = DictUtils.Dicts.Values.First(static dict => dict.Type is DictType.ProfileCustomWordDictionary);
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private const string WordJLFieldsInfo = """
                                            • Primary Spelling: It's the spelling you click to mine the word, e.g., if you look up "わかりました", its primary spelling will be "分かる".
                                            • Readings: Readings of the mined word, e.g., if you look up "描く", its "Readings" will be "えがく, かく".
                                            • Alternative Spellings: Alternative spellings of the mined word, e.g., if you look up "わかりました", its alternative spellings will be "解る, 判る, 分る".
                                            • Definitions: Definitions of the mined word.
                                            • Dictionary Name: Name of the dictionary, e.g., JMDict.
                                            • Audio: Audio for the first reading of the mined word.
                                            • Source Text: Whole text in which the mined word appears in.
                                            • Sentence: Sentence in which the mined word appears in.
                                            • Matched Text: Text the mined word found as, e.g., "わかりました".
                                            • Deconjugated Matched Text: Matched Text's deconjugated form, e.g., if the "Matched Text" is "わかりました", "Deconjugated Matched Text" will be "わかる".
                                            • Deconjugation Process: Deconjugation path from the "Matched Text" to "Deconjugated Matched Text".
                                            • Frequencies: Frequency info for the mined word, e.g., "VN: #77, JPDB: #666".
                                            • EDICT ID: JMDict entry ID.
                                            • Local Time: Mining date and time expressed in local timezone.
                                            """;

    private const string KanjiJLFieldsInfo = """
                                             • Primary Spelling: It's the spelling you click to mine the kanji, e.g., "妹".
                                             • Readings: Kun+On+Nanori readings of the kanji.
                                             • Kun Readings: Kun readings of the mined kanji.
                                             • On Readings: On readings of the mined kanji.
                                             • Nanori Readings: Nanori readings of the mined kanji.
                                             • Stroke Count: Stroke count of the kanji.
                                             • Kanji Grade: The kanji grade level.
                                             • Kanji Composition: Kanji composition info, e.g., "⿰女未" for "妹".
                                             • Definitions: Definitions of the mined kanji.
                                             • Dictionary Name: Name of the dictionary, e.g., "Kanjidic".
                                             • Source Text: Whole text in which the mined kanji appears in.
                                             • Sentence: Sentence in which the mined kanji appears in.
                                             • Frequencies: Frequency info for the kanji.
                                             • EDICT ID: KANJIDIC2 entry ID.
                                             • Local Time: Mining date and time expressed in local timezone.
                                             """;

    private const string NameJLFieldsInfo = """
                                            • Primary Spelling: It's the spelling you click to mine the name.
                                            • Readings: Readings of the name.
                                            • Alternative Spellings: Alternative spellings of the mined name.
                                            • Definitions: Translations of the name.
                                            • Dictionary Name: Name of the dictionary, e.g., "JMnedict".
                                            • Source Text: Whole text in which the mined name appears in.
                                            • Sentence: Sentence in which the mined name appears in.
                                            • EDICT ID: JMnedict entry ID.
                                            • Local Time: Mining date and time expressed in local timezone.
                                            """;

    #region EventHandlers

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker((Button)sender);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Utils.Frontend.InvalidateDisplayCache();
        await ConfigManager.SavePreferences(this).ConfigureAwait(true);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;

        if (_profileName != ProfileUtils.CurrentProfile)
        {
            _profileName = ProfileUtils.CurrentProfile;
            _profileNamesDict.Path = Utils.GetPath(ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfile));
            _profileWordsDict.Path = Utils.GetPath(ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfile));

            if (_profileNamesDict.Active || _profileWordsDict.Active)
            {
                if (_profileNamesDict.Active)
                {
                    DictUtils.ProfileCustomNamesCancellationTokenSource?.Cancel();
                    _profileNamesDict.Contents = new Dictionary<string, IList<IDictRecord>>();
                }

                if (_profileWordsDict.Active)
                {
                    DictUtils.ProfileCustomWordsCancellationTokenSource?.Cancel();
                    _profileWordsDict.Contents = new Dictionary<string, IList<IDictRecord>>();
                }

                await DictUtils.LoadDictionaries().ConfigureAwait(false);
            }

            await ProfileUtils.SerializeProfiles().ConfigureAwait(false);
        }
    }

    private async void AnkiTabItem_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (ConfigManager.AnkiIntegration && !SetAnkiConfig)
        {
            await SetPreviousMiningConfig().ConfigureAwait(true);
            await PopulateDeckAndModelNames().ConfigureAwait(true);

            SetAnkiConfig = true;
        }
    }

    private async void CheckForJLUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckForJLUpdatesButton.IsEnabled = false;
        await Networking.CheckForJLUpdates(false).ConfigureAwait(true);
        CheckForJLUpdatesButton.IsEnabled = true;
    }

    #endregion

    #region MiningSetup

    private async Task SetPreviousMiningConfig()
    {
        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfig.ReadAnkiConfig().ConfigureAwait(true);

        if (ankiConfigDict is null)
        {
            return;
        }

        AnkiConfig? wordAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Word);
        AnkiConfig? kanjiAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Kanji);
        AnkiConfig? nameAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Name);
        AnkiConfig? otherAnkiConfig = ankiConfigDict.GetValueOrDefault(MineType.Other);


        if (wordAnkiConfig is not null)
        {
            SetPreviousMiningConfig(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordTagsTextBox, wordAnkiConfig);
            CreateFieldElements(wordAnkiConfig.Fields, JLFieldUtils.JLFieldsForWordDicts, WordMiningSetupStackPanelFields);
        }

        if (kanjiAnkiConfig is not null)
        {
            SetPreviousMiningConfig(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiTagsTextBox, kanjiAnkiConfig);
            CreateFieldElements(kanjiAnkiConfig.Fields, JLFieldUtils.JLFieldsForKanjiDicts, KanjiMiningSetupStackPanelFields);
        }

        if (nameAnkiConfig is not null)
        {
            SetPreviousMiningConfig(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameTagsTextBox, nameAnkiConfig);
            CreateFieldElements(nameAnkiConfig.Fields, JLFieldUtils.JLFieldsForNameDicts, NameMiningSetupStackPanelFields);
        }

        if (otherAnkiConfig is not null)
        {
            SetPreviousMiningConfig(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherTagsTextBox, otherAnkiConfig);
            CreateFieldElements(otherAnkiConfig.Fields, Enum.GetValues<JLField>().ToList(), OtherMiningSetupStackPanelFields);
        }
    }

    private static void SetPreviousMiningConfig(Selector deckNamesSelector, Selector modelNamesComboBox, TextBox tagTextBox, AnkiConfig ankiConfig)
    {
        deckNamesSelector.ItemsSource = new List<string> { ankiConfig.DeckName };
        deckNamesSelector.SelectedItem = ankiConfig.DeckName;
        modelNamesComboBox.ItemsSource = new List<string> { ankiConfig.ModelName };
        modelNamesComboBox.SelectedItem = ankiConfig.ModelName;
        tagTextBox.Text = string.Join(", ", ankiConfig.Tags);
    }

    private async Task PopulateDeckAndModelNames()
    {
        List<string>? deckNames = await AnkiUtils.GetDeckNames().ConfigureAwait(true);

        if (deckNames is not null)
        {
            List<string>? modelNames = await AnkiUtils.GetModelNames().ConfigureAwait(true);

            if (modelNames is not null)
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
                Utils.Frontend.Alert(AlertLevel.Error, "Error getting model names from Anki");
                Utils.Logger.Error("Error getting model names from Anki");
            }
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Error getting deck names from Anki");
            Utils.Logger.Error("Error getting deck names from Anki");
        }
    }

    private async void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
    {
        await PopulateDeckAndModelNames().ConfigureAwait(false);
    }

    private static async Task GetFields(ComboBox modelNamesComboBox, Panel miningPanel, IEnumerable<JLField> fieldList)
    {
        string modelName = modelNamesComboBox.SelectionBoxItem.ToString()!;

        List<string>? fieldNames = await AnkiUtils.GetFieldNames(modelName).ConfigureAwait(true);

        if (fieldNames is not null)
        {
            Dictionary<string, JLField> fields =
                fieldNames.ToDictionary(static fieldName => fieldName, static _ => JLField.Nothing);

            CreateFieldElements(fields, fieldList, miningPanel);
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Error getting fields from AnkiConnect");
            Utils.Logger.Error("Error getting fields from AnkiConnect");
        }
    }

    private async void WordMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForWordDicts).ConfigureAwait(false);
    }

    private async void KanjiMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForKanjiDicts).ConfigureAwait(false);
    }

    private async void NameMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForNameDicts).ConfigureAwait(false);
    }

    private async void OtherMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForWordDicts).ConfigureAwait(false);
    }

    private static void CreateFieldElements(Dictionary<string, JLField> fields, IEnumerable<JLField> fieldList, Panel fieldPanel)
    {
        fieldPanel.Children.Clear();

        string[] descriptions = fieldList
            .Select(static jlFieldName => jlFieldName.GetDescription() ?? jlFieldName.ToString()).ToArray();

        foreach ((string fieldName, JLField jlField) in fields)
        {
            StackPanel stackPanel = new();
            TextBlock textBlockFieldName = new() { Text = fieldName };
            ComboBox comboBoxJLFields = new()
            {
                ItemsSource = descriptions,
                SelectedItem = jlField.GetDescription() ?? jlField.ToString()
            };

            _ = stackPanel.Children.Add(textBlockFieldName);
            _ = stackPanel.Children.Add(comboBoxJLFields);
            _ = fieldPanel.Children.Add(stackPanel);
        }
    }

    private static AnkiConfig? GetAnkiConfigFromPreferences(Selector deckNamesSelector, Selector modelNamesSelector, Panel miningPanel, TextBox tagsTextBox, IReadOnlyCollection<JLField> jlFieldList, MineType mineType)
    {
        if (deckNamesSelector.SelectedItem is null ||
            modelNamesSelector.SelectedItem is null)
        {
            string mineTypeStr = mineType.ToString();
            Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Save failed: Incomplete Anki config for {mineTypeStr} dictionaries"));
            Utils.Logger.Error("Save failed: Incomplete Anki config for {MineType} dictionaries", mineTypeStr);
            return null;
        }

        string deckName = deckNamesSelector.SelectedItem.ToString()!;
        string modelName = modelNamesSelector.SelectedItem.ToString()!;

        Dictionary<string, JLField> dict = new();
        foreach (StackPanel stackPanel in miningPanel.Children)
        {
            TextBlock textBlock = (TextBlock)stackPanel.Children[0];
            ComboBox comboBox = (ComboBox)stackPanel.Children[1];

            string selectedDescription = comboBox.SelectionBoxItem.ToString()!;
            JLField result = jlFieldList.FirstOrDefault(jlFieldName =>
                (jlFieldName.GetDescription() ?? jlFieldName.ToString()) == selectedDescription, JLField.Nothing);

            dict.Add(textBlock.Text, result);
        }

        string rawTags = tagsTextBox.Text;
        string[] tags = string.IsNullOrEmpty(rawTags)
            ? Array.Empty<string>()
            : rawTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();

        return new AnkiConfig(deckName, modelName, dict, tags);
    }

    public async Task SaveMiningSetup()
    {
        if (!ConfigManager.AnkiIntegration)
        {
            return;
        }

        Dictionary<MineType, AnkiConfig> ankiConfigDict = new();

        AnkiConfig? ankiConfig = GetAnkiConfigFromPreferences(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, WordTagsTextBox, JLFieldUtils.JLFieldsForWordDicts, MineType.Word);
        if (ankiConfig is not null)
        {
            ankiConfigDict.Add(MineType.Word, ankiConfig);
        }

        ankiConfig = GetAnkiConfigFromPreferences(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, KanjiTagsTextBox, JLFieldUtils.JLFieldsForKanjiDicts, MineType.Kanji);
        if (ankiConfig is not null)
        {
            ankiConfigDict.Add(MineType.Kanji, ankiConfig);
        }

        ankiConfig = GetAnkiConfigFromPreferences(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, NameTagsTextBox, JLFieldUtils.JLFieldsForNameDicts, MineType.Name);
        if (ankiConfig is not null)
        {
            ankiConfigDict.Add(MineType.Name, ankiConfig);
        }

        ankiConfig = GetAnkiConfigFromPreferences(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, OtherTagsTextBox, JLFieldUtils.JLFieldsForWordDicts, MineType.Other);
        if (ankiConfig is not null)
        {
            ankiConfigDict.Add(MineType.Other, ankiConfig);
        }

        if (ankiConfigDict.Count > 0)
        {
            _ = await AnkiConfig.WriteAnkiConfig(ankiConfigDict).ConfigureAwait(false);
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Error saving AnkiConfig");
            Utils.Logger.Error("Error saving AnkiConfig");
            ConfigManager.AnkiIntegration = false;
        }
    }

    #endregion

    #region Keys

    private void KeyGestureToText(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        Key key = e.Key is Key.System
            ? e.SystemKey
            : e.Key;

        if (key is Key.LWin or Key.RWin)
        {
            return;
        }

        string hotKeyText;
        if (key is Key.LeftShift or Key.RightShift
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt)
        {
            hotKeyText = key.ToString();
        }
        else
        {
            StringBuilder sb = new();

            if ((Keyboard.Modifiers & ModifierKeys.Control) is not 0)
            {
                _ = sb.Append("Ctrl+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) is not 0)
            {
                _ = sb.Append("Alt+");
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) is not 0 && sb.Length > 0)
            {
                _ = sb.Append("Shift+");
            }

            hotKeyText = sb.Append(key.ToString()).ToString();
        }

        TextBox currentTextBox = (TextBox)sender;

        if (LookupKeyKeyGestureTextBox != currentTextBox
            && LookupKeyKeyGestureTextBox.Text == hotKeyText)
        {
            return;
        }

        currentTextBox.Text = hotKeyText;

        foreach (DockPanel dockPanel in HotKeysStackPanel.Children.OfType<DockPanel>())
        {
            TextBox textBox = dockPanel.Children.OfType<TextBox>().First();
            if (textBox.Text == hotKeyText && textBox != currentTextBox)
            {
                textBox.Text = "None";
            }
        }
    }

    private void ClearKeyGestureButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = (Button)sender;
        DockPanel dockPanel = (DockPanel)button.Parent;
        TextBox textBox = dockPanel.Children.OfType<TextBox>().First();
        textBox.Text = "None";
    }

    #endregion

    private void ApplyAnkiConnectUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (Uri.IsWellFormedUriString(AnkiUriTextBox.Text, UriKind.Absolute))
        {
            string normalizedUrl = AnkiUriTextBox.Text
                .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                .Replace("://localhost:", "://127.0.0.1:", StringComparison.Ordinal);
            CoreConfig.AnkiConnectUri = new Uri(normalizedUrl);
            AnkiUriTextBox.Text = normalizedUrl;
        }

        else
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save AnkiConnect server address, invalid URL");
        }
    }

    private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string title = "JL Fields for ";
        string text;

        if (WordInfoButton == sender)
        {
            title += "Words";
            text = WordJLFieldsInfo;
        }

        else if (KanjiInfoButton == sender)
        {
            title += "Kanjis";
            text = KanjiJLFieldsInfo;
        }

        else if (NameInfoButton == sender)
        {
            title += "Names";
            text = NameJLFieldsInfo;
        }

        else // if (OtherInfoButton == sender)
        {
            title += "Others";
            text = WordJLFieldsInfo;
        }

        InfoWindow infoWindow = new()
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = title,
            InfoTextBox = { Text = text }
        };

        _ = infoWindow.ShowDialog();
    }

    private async void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string selectedProfile = (string)((ComboBox)sender).SelectedItem;
        if (selectedProfile != ProfileUtils.CurrentProfile)
        {
            await Stats.SerializeProfileLifetimeStats().ConfigureAwait(false);
            ProfileUtils.CurrentProfile = selectedProfile;

            ConfigManager.MappedExeConfiguration = new ExeConfigurationFileMap
            {
                ExeConfigFilename = ProfileUtils.GetProfilePath(ProfileUtils.CurrentProfile)
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                ConfigManager.ApplyPreferences();
                ConfigManager.LoadPreferences(this);
            });

            await StatsUtils.DeserializeProfileLifetimeStats().ConfigureAwait(false);
        }
    }

    private void ProfileConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _ = new ManageProfilesWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
    }
}
