using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Mining;
using JL.Core.Mining.Anki;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PreferenceWindow.xaml
/// </summary>
internal sealed partial class PreferencesWindow
{
    private static PreferencesWindow? s_instance;
    public static PreferencesWindow Instance => s_instance ??= new PreferencesWindow();
    public bool SetAnkiConfig { get; private set; } // = false;
    private string _profileName;
    private readonly Dict _profileNamesDict;
    private readonly Dict _profileWordsDict;

    private PreferencesWindow()
    {
        InitializeComponent();
        _profileName = ProfileUtils.CurrentProfileName;
        _profileNamesDict = DictUtils.SingleDictTypeDicts[DictType.ProfileCustomNameDictionary];
        _profileWordsDict = DictUtils.SingleDictTypeDicts[DictType.ProfileCustomWordDictionary];
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private static readonly string s_wordJLFieldsInfo = $"""
                                            • {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the word, e.g., if you look up "わかりました", its primary spelling will be "分かる".
                                            • {JLField.PrimarySpellingWithOrthographyInfo.GetDescription()}: It's the spelling you click to mine the word with its orthography Info, e.g., if you look up "珈琲", its "Primary Spelling with Orthography Info" will be "珈琲 (ateji)".
                                            • {JLField.Readings.GetDescription()}: Readings of the mined word, e.g., if you look up "従妹", its "Readings" will be "じゅうまい、いとこ".
                                            • {JLField.FirstReading.GetDescription()}: First reading of the mined word, e.g., if you look up "従妹", its "First Reading" will be "じゅうまい".
                                            • {JLField.ReadingsWithOrthographyInfo.GetDescription()}: Readings of the mined word with their orthography info, e.g. if you look up "従妹", its "Readings with Orthography Info" will be "じゅうまい、いとこ (gikun)".
                                            • {JLField.AlternativeSpellings.GetDescription()}: Alternative spellings of the mined word, e.g., if you look up "わかりました", its alternative spellings will be "解る、判る、分る".
                                            • {JLField.AlternativeSpellingsWithOrthographyInfo.GetDescription()}: Alternative spellings of the mined word with their orthography info, e.g., if you look up "嫁" its "Alternative Spellings with Orthography Info" will be "娵 (rK)、婦 (rK)、媳 (rK)".
                                            • {JLField.Definitions.GetDescription()}: Definitions of the mined word. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.
                                            • {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.
                                            • {JLField.PrimarySpellingAndReadings.GetDescription()}: Primary spelling and its readings in the format "Primary Spelling[Reading 1、Reading 2、...、Reading N]" format, e.g., 俺[おれ、オレ、おらあ、おり].
                                            • {JLField.PrimarySpellingAndFirstReading.GetDescription()}: Primary spelling and its first reading in the format "Primary Spelling[Reading 1]" format, e.g., 俺[おれ].
                                            • {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., JMDict.
                                            • {JLField.Audio.GetDescription()}: Audio for the first reading of the mined word.
                                            • {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.
                                            • {JLField.SourceText.GetDescription()}: Whole text in which the mined word appears in.
                                            • {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the matched text.
                                            • {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the matched text.
                                            • {JLField.Sentence.GetDescription()}: Sentence in which the mined word appears in.
                                            • {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the matched text. e.g., if the mined word is "大好き" while the sentence is "妹が大好きです", "Leading Sentence Part" will be "妹が".
                                            • {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the matched text. e.g., if the mined word is "大好き" while the sentence is "妹が大好きです", "Trailing Sentence Part" will be "です".
                                            • {JLField.MatchedText.GetDescription()}: Text the mined word found as, e.g., "わかりました".
                                            • {JLField.DeconjugatedMatchedText.GetDescription()}: Matched Text's deconjugated form, e.g., if the "Matched Text" is "わかりました", "Deconjugated Matched Text" will be "わかる".
                                            • {JLField.DeconjugationProcess.GetDescription()}: Deconjugation path from the "Matched Text" to "Deconjugated Matched Text".
                                            • {JLField.Frequencies.GetDescription()}: Frequency info for the mined word, e.g., "VN: 77, jpdb: 666".
                                            • {JLField.RawFrequencies.GetDescription()}: Raw frequency info for the mined word, e.g., "77, 666".
                                            • {JLField.PreferredFrequency.GetDescription()}: Frequency info for the mined word from the frequency dictionary with the highest priority, e.g., "666".
                                            • {JLField.FrequencyHarmonicMean.GetDescription()}: Harmonic mean of the raw frequencies, e.g., "666".
                                            • {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.
                                            • {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: ３, おみな: 0, おうな: 1".
                                            • {JLField.PitchAccentForFirstReading.GetDescription()}: Pitch accent for the first reading of the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.
                                            • {JLField.NumericPitchAccentForFirstReading.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: 3".
                                            • {JLField.EdictId.GetDescription()}: JMDict entry ID.
                                            • {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone.
                                            """;

    private static readonly string s_kanjiJLFieldsInfo = $"""
                                             • {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the kanji, e.g., "妹".
                                             • {JLField.Readings.GetDescription()}: Kun+On+Nanori readings of the kanji.
                                             • {JLField.KunReadings.GetDescription()}: Kun readings of the mined kanji.
                                             • {JLField.OnReadings.GetDescription()}: On readings of the mined kanji.
                                             • {JLField.NanoriReadings.GetDescription()}: Nanori readings of the mined kanji.
                                             • {JLField.RadicalNames.GetDescription()}: Radical names of the kanji.
                                             • {JLField.StrokeCount.GetDescription()}: Stroke count of the kanji.
                                             • {JLField.KanjiGrade.GetDescription()}: The kanji grade level.
                                             • {JLField.KanjiComposition.GetDescription()}: Kanji composition info, e.g., "⿰女未" for "妹".
                                             • {JLField.KanjiStats.GetDescription()}: Kanji stats.
                                             • {JLField.Definitions.GetDescription()}: Definitions of the mined kanji. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.
                                             • {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.
                                             • {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., "Kanjidic".
                                             • {JLField.Audio.GetDescription()}: Audio for the first reading of the mined word.
                                             • {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.
                                             • {JLField.SourceText.GetDescription()}: Whole text in which the mined kanji appears in.
                                             • {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the mined kanji.
                                             • {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the mined kanji.
                                             • {JLField.Sentence.GetDescription()}: Sentence in which the mined kanji appears in.
                                             • {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the mined kanji. e.g., if the mined kanji is "大" while the sentence is "妹が大好きです", "Leading Sentence Part" will be "妹が".
                                             • {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the mined kanji. e.g., if the mined kanji is "大" while the sentence is "妹が大好きです", "Trailing Sentence Part" will be "好きです".
                                             • {JLField.Frequencies.GetDescription()}: Frequency info for the kanji, e.g., "KANJIDIC2: 77, jpdb: 666".
                                             • {JLField.RawFrequencies.GetDescription()}: Raw frequency info for the mined word, e.g., "77, 666".
                                             • {JLField.PreferredFrequency.GetDescription()}: Frequency info for the mined word from the frequency dictionary with the highest priority, e.g., "666".
                                             • {JLField.FrequencyHarmonicMean.GetDescription()}: Harmonic mean of the raw frequencies, e.g., "666".
                                             • {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.
                                             • {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: 3, おみな: 0, おうな: 1".
                                             • {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone.
                                             """;

    private static readonly string s_nameJLFieldsInfo = $"""
                                            • {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the name.
                                            • {JLField.Readings.GetDescription()}: Readings of the name.
                                            • {JLField.FirstReading.GetDescription()}: First reading of the name.
                                            • {JLField.AlternativeSpellings.GetDescription()}: Alternative spellings of the mined name.
                                            • {JLField.Definitions.GetDescription()}: Translations of the name. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.
                                            • {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.
                                            • {JLField.PrimarySpellingAndReadings.GetDescription()}: Primary spelling and its readings in the format "Primary Spelling[Reading 1、Reading 2、...、Reading N]" format, e.g., 俺[おれ、オレ、おらあ、おり].
                                            • {JLField.PrimarySpellingAndFirstReading.GetDescription()}: Primary spelling and its first reading in the format "Primary Spelling[Reading 1]" format, e.g., 俺[おれ].
                                            • {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., "JMnedict".
                                            • {JLField.Audio.GetDescription()}: Audio for the first reading of the mined word.
                                            • {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.
                                            • {JLField.SourceText.GetDescription()}: Whole text in which the mined name appears in.
                                            • {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the mined name.
                                            • {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the mined name.
                                            • {JLField.Sentence.GetDescription()}: Sentence in which the mined name appears in.
                                            • {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the mined name. e.g., if the mined name is "エスト" while the sentence is "俺はエストのことが大好き", "Leading Sentence Part" will be "俺は".
                                            • {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the mined name. e.g., if the mined name is "エスト" while the sentence is "俺はエストのことが大好き", "Trailing Sentence Part" will be "のことが大好き".
                                            • {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.
                                            • {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: 3, おみな: 0, おうな: 1".
                                            • {JLField.PitchAccentForFirstReading.GetDescription()}: Pitch accent for the first reading of the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.
                                            • {JLField.NumericPitchAccentForFirstReading.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: 3".
                                            • {JLField.EdictId.GetDescription()}: JMnedict entry ID.
                                            • {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone.
                                            """;

    #region EventHandlers

    private void ShowColorPicker(object sender, RoutedEventArgs e)
    {
        WindowsUtils.ShowColorPicker((Button)sender);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ConfigManager.Instance.SavePreferences(this).ConfigureAwait(true);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;

        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();

        if (_profileName == ProfileUtils.CurrentProfileName)
        {
            return;
        }

        _profileName = ProfileUtils.CurrentProfileName;
        _profileNamesDict.Path = Utils.GetPath(ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfileName));
        _profileWordsDict.Path = Utils.GetPath(ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfileName));

        if (!_profileNamesDict.Active && !_profileWordsDict.Active)
        {
            return;
        }

        if (_profileNamesDict.Active)
        {
            if (DictUtils.ProfileCustomNamesCancellationTokenSource is not null)
            {
                await DictUtils.ProfileCustomNamesCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _profileNamesDict.Contents = new Dictionary<string, IList<IDictRecord>>(256, StringComparer.Ordinal);
        }

        if (_profileWordsDict.Active)
        {
            if (DictUtils.ProfileCustomWordsCancellationTokenSource is not null)
            {
                await DictUtils.ProfileCustomWordsCancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }

            _profileWordsDict.Contents = new Dictionary<string, IList<IDictRecord>>(256, StringComparer.Ordinal);
        }

        await DictUtils.LoadDictionaries().ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void AnkiTabItem_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!SetAnkiConfig)
        {
            if (CoreConfigManager.Instance.AnkiIntegration)
            {
                await SetPreviousMiningConfig().ConfigureAwait(true);
                await PopulateDeckAndModelNames().ConfigureAwait(true);
            }

            SetAnkiConfig = true;
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void CheckForJLUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckForJLUpdatesButton.IsEnabled = false;
        await Networking.CheckForJLUpdates(false).ConfigureAwait(true);
        CheckForJLUpdatesButton.IsEnabled = true;
    }

    private void ResetPreferencesButton_Click(object sender, RoutedEventArgs e)
    {
        ResetPreferencesButton.IsEnabled = false;
        if (WindowsUtils.ShowYesNoDialog("Are you really sure that you want to reset all your preferences to their default values for the current profile?", "Reset preferences for the current profile?"))
        {
            IsEnabled = false;
            ConfigManager.ResetConfigs();
            MainWindow.Instance.UpdateLayout();
            Close();
        }
        else
        {
            ResetPreferencesButton.IsEnabled = true;
        }
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
            CreateFieldElements(otherAnkiConfig.Fields, Enum.GetValues<JLField>(), OtherMiningSetupStackPanelFields);
        }
    }

    private static void SetPreviousMiningConfig(Selector deckNamesSelector, Selector modelNamesComboBox, TextBox tagTextBox, AnkiConfig ankiConfig)
    {
        deckNamesSelector.ItemsSource = new[]
        {
            ankiConfig.DeckName
        };
        deckNamesSelector.SelectedItem = ankiConfig.DeckName;
        modelNamesComboBox.ItemsSource = new[]
        {
            ankiConfig.ModelName
        };
        modelNamesComboBox.SelectedItem = ankiConfig.ModelName;
        tagTextBox.Text = ankiConfig.Tags is not null
            ? string.Join(", ", ankiConfig.Tags)
            : "";
    }

    private async Task PopulateDeckAndModelNames()
    {
        List<string>? deckNames = await AnkiUtils.GetDeckNames().ConfigureAwait(true);

        if (deckNames is not null)
        {
            List<string>? modelNames = await AnkiUtils.GetModelNames().ConfigureAwait(true);

            if (modelNames is not null)
            {
                WordMiningSetupComboBoxDeckNames.ItemsSource = deckNames;
                KanjiMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToArray();
                NameMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToArray();
                OtherMiningSetupComboBoxDeckNames.ItemsSource = deckNames.ToArray();

                WordMiningSetupComboBoxModelNames.ItemsSource = modelNames;
                KanjiMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToArray();
                NameMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToArray();
                OtherMiningSetupComboBoxModelNames.ItemsSource = modelNames.ToArray();
            }

            else
            {
                WindowsUtils.Alert(AlertLevel.Error, "Error getting model names from Anki");
                Utils.Logger.Error("Error getting model names from Anki");
            }
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error getting deck names from Anki");
            Utils.Logger.Error("Error getting deck names from Anki");
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
    {
        await PopulateDeckAndModelNames().ConfigureAwait(false);
    }

    private static async Task GetFields(ComboBox modelNamesComboBox, Panel miningPanel, JLField[] fieldList)
    {
        string modelName = modelNamesComboBox.SelectionBoxItem.ToString()!;

        List<string>? fieldNames = await AnkiUtils.GetFieldNames(modelName).ConfigureAwait(true);

        if (fieldNames is not null)
        {
            Dictionary<string, JLField> fields =
                fieldNames.ToDictionary(static fieldName => fieldName, static _ => JLField.Nothing, StringComparer.Ordinal);

            CreateFieldElements(fields, fieldList, miningPanel);
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Error getting fields from AnkiConnect");
            Utils.Logger.Error("Error getting fields from AnkiConnect");
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void WordMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForWordDicts).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void KanjiMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForKanjiDicts).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void NameMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForNameDicts).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OtherMiningSetupButtonGetFields_Click(object sender, RoutedEventArgs e)
    {
        await GetFields(OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, JLFieldUtils.JLFieldsForWordDicts).ConfigureAwait(false);
    }

    private static void CreateFieldElements(Dictionary<string, JLField> fields, JLField[] fieldList, Panel fieldPanel)
    {
        fieldPanel.Children.Clear();

        string[] descriptions = fieldList
            .Select(static jlFieldName => jlFieldName.GetDescription() ?? jlFieldName.ToString()).ToArray();

        foreach ((string fieldName, JLField jlField) in fields)
        {
            StackPanel stackPanel = new();
            TextBlock textBlockFieldName = new()
            {
                Text = fieldName
            };
            ComboBox comboBoxJLFields = new()
            {
                ItemsSource = descriptions,
                SelectedItem = jlField.GetDescription() ?? jlField.ToString(),
                Margin = new Thickness(0, 10, 0, 15)
            };

            _ = stackPanel.Children.Add(textBlockFieldName);
            _ = stackPanel.Children.Add(comboBoxJLFields);
            _ = fieldPanel.Children.Add(stackPanel);
        }
    }

    private static AnkiConfig? GetAnkiConfigFromPreferences(Selector deckNamesSelector, Selector modelNamesSelector, Panel miningPanel, TextBox tagsTextBox, JLField[] jlFieldList, MineType mineType)
    {
        if (deckNamesSelector.SelectedItem is null ||
            modelNamesSelector.SelectedItem is null)
        {
            WindowsUtils.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Save failed: Incomplete Anki config for {mineType} dictionaries"));
            Utils.Logger.Error("Save failed: Incomplete Anki config for {MineType} dictionaries", mineType);
            return null;
        }

        string deckName = deckNamesSelector.SelectedItem.ToString()!;
        string modelName = modelNamesSelector.SelectedItem.ToString()!;

        Dictionary<string, JLField> dict = new(miningPanel.Children.Count, StringComparer.Ordinal);
        foreach (StackPanel stackPanel in miningPanel.Children.Cast<StackPanel>())
        {
            TextBlock textBlock = (TextBlock)stackPanel.Children[0];
            ComboBox comboBox = (ComboBox)stackPanel.Children[1];

            string selectedDescription = comboBox.SelectionBoxItem.ToString()!;
            JLField result = jlFieldList.FirstOrDefault(jlFieldName =>
                (jlFieldName.GetDescription() ?? jlFieldName.ToString()) == selectedDescription, JLField.Nothing);

            dict.Add(textBlock.Text, result);
        }

        string rawTags = tagsTextBox.Text;
        string[]? tags = string.IsNullOrWhiteSpace(rawTags)
            ? null
            : rawTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new AnkiConfig(deckName, modelName, dict, tags);
    }

    public Task SaveMiningSetup()
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (!coreConfigManager.AnkiIntegration)
        {
            return Task.CompletedTask;
        }

        Dictionary<MineType, AnkiConfig> ankiConfigDict = [];

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
            return AnkiConfig.WriteAnkiConfig(ankiConfigDict);
        }

        WindowsUtils.Alert(AlertLevel.Error, "Error saving AnkiConfig");
        Utils.Logger.Error("Error saving AnkiConfig");
        coreConfigManager.AnkiIntegration = false;
        return Task.CompletedTask;
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
            CoreConfigManager.Instance.AnkiConnectUri = new Uri(normalizedUrl);
            AnkiUriTextBox.Text = normalizedUrl;
        }

        else
        {
            WindowsUtils.Alert(AlertLevel.Error, "Couldn't save AnkiConnect server address, invalid URL");
        }
    }

    private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string title = "JL Fields for ";
        string text;

        if (WordInfoButton == sender)
        {
            title += "Words";
            text = s_wordJLFieldsInfo;
        }

        else if (KanjiInfoButton == sender)
        {
            title += "Kanjis";
            text = s_kanjiJLFieldsInfo;
        }

        else if (NameInfoButton == sender)
        {
            title += "Names";
            text = s_nameJLFieldsInfo;
        }

        else // if (OtherInfoButton == sender)
        {
            title += "Others";
            text = s_wordJLFieldsInfo;
        }

        InfoWindow infoWindow = new()
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = title,
            InfoTextBox =
            {
                Text = text
            }
        };

        _ = infoWindow.ShowDialog();
    }

    private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string selectedProfileName = (string)((ComboBox)sender).SelectedItem;
        if (selectedProfileName == ProfileUtils.CurrentProfileName)
        {
            return;
        }

        using (SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection())
        {
            StatsDBUtils.UpdateProfileLifetimeStats(connection);
            ProfileUtils.CurrentProfileName = selectedProfileName;
            ProfileUtils.CurrentProfileId = ProfileDBUtils.GetProfileId(connection, selectedProfileName);
            ProfileDBUtils.UpdateCurrentProfile(connection);
            Stats.ProfileLifetimeStats = StatsDBUtils.GetStatsFromDB(connection, ProfileUtils.CurrentProfileId)!;
            StatsDBUtils.UpdateProfileLifetimeStats(connection);
        }

        ConfigManager configManager = ConfigManager.Instance;
        Application.Current.Dispatcher.Invoke(() =>
        {
            configManager.ApplyPreferences();
            configManager.LoadPreferenceWindow(this);
        });

        RegexReplacerUtils.PopulateRegexReplacements();
    }

    private void ProfileConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _ = new ManageProfilesWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }
}
