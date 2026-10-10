using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;
using JL.Core.External.AnkiConnect;
using JL.Core.Frontend;
using JL.Core.Mining;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;
using JL.Windows.Backlog;
using JL.Windows.Config;
using JL.Windows.GUI.Info;
using JL.Windows.GUI.Notification;
using JL.Windows.GUI.Profile;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for PreferenceWindow.xaml
/// </summary>
internal sealed partial class PreferencesWindow
{
    private static PreferencesWindow? s_instance;
    public static PreferencesWindow Instance
    {
        get
        {
            if (s_instance is null || !s_instance.IsLoaded)
            {
                s_instance = new PreferencesWindow();
            }

            return s_instance;
        }
    }

    public bool SetAnkiConfig { get; private set; } // = false;
    private Task? _ankiConfigTask;
    private bool _ankiNamesLoaded; // = false;
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

    private static readonly string[] s_wordJLFieldsInfo =
        [
            $"• {JLField.SelectedSpelling.GetDescription()}: The primary spelling or the selected reading you click to mine the word.",
            $"""• {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the word, e.g., if you look up "わかりました", its primary spelling will be "分かる".""",
            $"""• {JLField.PrimarySpellingWithOrthographyInfo.GetDescription()}: It's the spelling you click to mine the word with its orthography Info, e.g., if you look up "珈琲", its "Primary Spelling with Orthography Info" will be "珈琲 (ateji)".""",
            $"""• {JLField.Readings.GetDescription()}: Readings of the mined word, e.g., if you look up "従妹", its "Readings" will be "じゅうまい、いとこ".""",
            $"• {JLField.FirstReading.GetDescription()}: If you selected a reading to mine the word, it will be that reading, otherwise it will be the first reading of the mined word",
            $"""• {JLField.ReadingsWithOrthographyInfo.GetDescription()}: Readings of the mined word with their orthography info, e.g. if you look up "従妹", its "Readings with Orthography Info" will be "じゅうまい、いとこ (gikun)".""",
            $"""• {JLField.AlternativeSpellings.GetDescription()}: Alternative spellings of the mined word, e.g., if you look up "わかりました", its alternative spellings will be "解る、判る、分る".""",
            $"""• {JLField.AlternativeSpellingsWithOrthographyInfo.GetDescription()}: Alternative spellings of the mined word with their orthography info, e.g., if you look up "嫁" its "Alternative Spellings with Orthography Info" will be "娵 (rK)、婦 (rK)、媳 (rK)".""",
            $"• {JLField.Definitions.GetDescription()}: Definitions of the mined word. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.",
            $"• {JLField.DefinitionsFromMultipleDictionaries.GetDescription()}: Definitions for the mined word from word dictionaries.",
            $"""• {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.""",
            $"""• {JLField.PrimarySpellingAndReadings.GetDescription()}: Primary spelling and its readings in the format "Primary Spelling[Reading 1]、Spelling[Reading 2]、...、Spelling[Reading N]" format, e.g., 俺[おれ]、俺[オレ]、俺[おらあ]、俺[おり].""",
            $"""• {JLField.PrimarySpellingAndFirstReading.GetDescription()}: Primary spelling and its first reading in the format "Primary Spelling[Selected Reading]" format, e.g., 俺[おれ].""",
            $"• {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., JMDict.",
            $"• {JLField.Audio.GetDescription()}: Audio for the 'Selected Reading' of the mined word.",
            $"• {JLField.SentenceAudio.GetDescription()}: Audio for the sentence in which the mined word appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.SourceTextAudio.GetDescription()}: Audio for the whole text in which the mined word appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.MonitorScreenshot.GetDescription()}: Screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.",
            $"• {JLField.ImageClipboardOverMonitorScreenshot.GetDescription()}: Image from the clipboard at the time of mining. If the clipboard does not contain an image, screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.DefinitionsImages.GetDescription()}: Images included in definitions.",
            $"• {JLField.SourceText.GetDescription()}: Whole text in which the mined word appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SourceTextNoBolding.GetDescription()}: Whole text in which the mined word appears in. Unlike the Source Text field, it does not bold the Matched Text.",
            $"• {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the matched text.",
            $"• {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the matched text.",
            $"• {JLField.Sentence.GetDescription()}: Sentence in which the mined word appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SentenceNoBolding.GetDescription()}: Sentence in which the mined word appears in. Unlike the Sentence field, it does not bold the Matched Text.",
            $"""• {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the matched text. e.g., if the mined word is "大好き" while the sentence is "妹が大好きです", "Leading Sentence Part" will be "妹が".""",
            $"""• {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the matched text. e.g., if the mined word is "大好き" while the sentence is "妹が大好きです", "Trailing Sentence Part" will be "です".""",
            $"""• {JLField.MatchedText.GetDescription()}: Text the mined word found as, e.g., "わかりました".""",
            $"""• {JLField.DeconjugatedMatchedText.GetDescription()}: Matched Text's deconjugated form, e.g., if the "Matched Text" is "わかりました", "Deconjugated Matched Text" will be "わかる".""",
            $"""• {JLField.DeconjugationProcess.GetDescription()}: Deconjugation path from the "Matched Text" to "Deconjugated Matched Text".""",
            $"""• {JLField.WordClasses.GetDescription()}: Part-of-speech info for the mined word, e.g., if the mined word is 電話, "Word Classes" will be "n, vs, vt, vi".""",
            $"""• {JLField.Frequencies.GetDescription()}: Frequency info for the mined word, e.g., "VN: 77, jpdb: 666".""",
            $"""• {JLField.RawFrequencies.GetDescription()}: Raw frequency info for the mined word, e.g., "77, 666".""",
            $"""• {JLField.PreferredFrequency.GetDescription()}: Frequency info for the mined word from the frequency dictionary with the highest priority, e.g., "666".""",
            $"""• {JLField.FrequencyHarmonicMean.GetDescription()}: Harmonic mean of the raw frequencies, e.g., "666".""",
            $"• {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.",
            $"""• {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: ３, おみな: 0, おうな: 1".""",
            $"• {JLField.PitchAccentForFirstReading.GetDescription()}: Pitch accent for the 'Selected Reading' of the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.",
            $"""• {JLField.NumericPitchAccentForFirstReading.GetDescription()}: Pitch accent for the 'Selected Reading' of the mined word in numeric form, e.g., "おんな: 3".""",
            $"""• {JLField.PitchAccentCategories.GetDescription()}: Pitch accent categories for the mined word, e.g., "にほんご: Heiban, にっぽんご: Heiban". There are currently four pitch accent categories: Heiban, Atamadaka, Odaka and Nakadaka.""",
            $"""• {JLField.PitchAccentCategoryForFirstReading.GetDescription()}: Pitch accent category for the 'Selected Reading' of the mined word, e.g., "にほんご: Heiban". There are currently four pitch accent categories: Heiban, Atamadaka, Odaka and Nakadaka.""",
            $"• {JLField.EdictId.GetDescription()}: JMDict entry ID.",
            $"• {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone."
        ];

    private static readonly string[] s_kanjiJLFieldsInfo =
        [
            $"""• {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the kanji, e.g., "妹".""",
            $"• {JLField.Readings.GetDescription()}: Kun+On+Nanori readings of the kanji.",
            $"• {JLField.KunReadings.GetDescription()}: Kun readings of the mined kanji.",
            $"• {JLField.OnReadings.GetDescription()}: On readings of the mined kanji.",
            $"• {JLField.NanoriReadings.GetDescription()}: Nanori readings of the mined kanji.",
            $"• {JLField.RadicalNames.GetDescription()}: Radical names of the kanji.",
            $"• {JLField.StrokeCount.GetDescription()}: Stroke count of the kanji.",
            $"• {JLField.KanjiGrade.GetDescription()}: The kanji grade level.",
            $"""• {JLField.KanjiComposition.GetDescription()}: Kanji composition info, e.g., "⿰女未" for "妹".""",
            $"• {JLField.KanjiStats.GetDescription()}: Kanji stats.",
            $"• {JLField.Definitions.GetDescription()}: Definitions of the mined kanji. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.",
            $"• {JLField.DefinitionsFromMultipleDictionaries.GetDescription()}: Definitions for the mined kanji from kanji dictionaries.",
            $"""• {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.""",
            $"""• {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., "Kanjidic".""",
            $"• {JLField.SentenceAudio.GetDescription()}: Audio for the sentence in which the mined kanji appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.SourceTextAudio.GetDescription()}: Audio for the whole text in which the mined kanji appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.MonitorScreenshot.GetDescription()}: Screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.",
            $"• {JLField.ImageClipboardOverMonitorScreenshot.GetDescription()}: Image from the clipboard at the time of mining. If the clipboard does not contain an image, screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.DefinitionsImages.GetDescription()}: Images included in definitions.",
            $"• {JLField.SourceText.GetDescription()}: Whole text in which the mined kanji appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SourceTextNoBolding.GetDescription()}: Whole text in which the mined kanji appears in. Unlike the Source Text field, it does not bold the Matched Text.",
            $"• {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the mined kanji.",
            $"• {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the mined kanji.",
            $"• {JLField.Sentence.GetDescription()}: Sentence in which the mined kanji appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SentenceNoBolding.GetDescription()}: Sentence in which the mined kanji appears in. Unlike the Sentence field, it does not bold the Matched Text.",
            $"""• {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the mined kanji. e.g., if the mined kanji is "大" while the sentence is "妹が大好きです", "Leading Sentence Part" will be "妹が".""",
            $"""• {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the mined kanji. e.g., if the mined kanji is "大" while the sentence is "妹が大好きです", "Trailing Sentence Part" will be "好きです".""",
            $"""• {JLField.Frequencies.GetDescription()}: Frequency info for the kanji, e.g., "KANJIDIC2: 77, jpdb: 666".""",
            $"""• {JLField.RawFrequencies.GetDescription()}: Raw frequency info for the mined kanji, e.g., "77, 666".""",
            $"""• {JLField.PreferredFrequency.GetDescription()}: Frequency info for the mined kanji from the frequency dictionary with the highest priority, e.g., "666".""",
            $"""• {JLField.FrequencyHarmonicMean.GetDescription()}: Harmonic mean of the raw frequencies, e.g., "666".""",
            $"• {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined kanji, displayed in a similar fashion to how pitch accents are shown in a JL popup.",
            $"""• {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined kanji in numeric form, e.g., "おんな: 3, おみな: 0, おうな: 1".""",
            $"""• {JLField.PitchAccentCategories.GetDescription()}: Pitch accent categories for the mined word, e.g., "にほんご: Heiban, にっぽんご: Heiban". There are currently four pitch accent categories: Heiban, Atamadaka, Odaka and Nakadaka.""",
            $"• {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone."
        ];

    private static readonly string[] s_nameJLFieldsInfo =
        [
            $"• {JLField.SelectedSpelling.GetDescription()}: The primary spelling or the selected reading you click to mine the name.",
            $"• {JLField.PrimarySpelling.GetDescription()}: It's the spelling you click to mine the name.",
            $"• {JLField.Readings.GetDescription()}: Readings of the name.",
            $"• {JLField.FirstReading.GetDescription()}: If you selected a reading to mine the name, it will be that reading, otherwise it will be the first reading of the mined name",
            $"• {JLField.AlternativeSpellings.GetDescription()}: Alternative spellings of the mined name.",
            $"• {JLField.Definitions.GetDescription()}: Translations of the name. You can edit the definitions in the popup window by pressing Insert key and clicking on the definitions text box with the left mouse button.",
            $"• {JLField.DefinitionsFromMultipleDictionaries.GetDescription()}: Definitions for the mined name from name dictionaries.",
            $"""• {JLField.SelectedDefinitions.GetDescription()}: The selected text on definition text box. If no text is selected, it will have the same value as "Definitions" field.""",
            $"""• {JLField.PrimarySpellingAndReadings.GetDescription()}: Primary spelling and its readings in the format "Primary Spelling[Reading 1、Reading 2、...、Reading N]" format, e.g., 俺[おれ、オレ、おらあ、おり].""",
            $"""• {JLField.PrimarySpellingAndFirstReading.GetDescription()}: Primary spelling and its 'Selected Reading' in the format "Primary Spelling[Selected Reading]" format, e.g., 俺[おれ].""",
            $"""• {JLField.DictionaryName.GetDescription()}: Name of the dictionary, e.g., "JMnedict".""",
            $"• {JLField.Audio.GetDescription()}: Audio for the 'Selected Reading' of the mined name.",
            $"• {JLField.SentenceAudio.GetDescription()}: Audio for the sentence in which the mined name appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.SourceTextAudio.GetDescription()}: Audio for the whole text in which the mined name appears in. The audio is generated using the first active TTS voice listed in the Manage Audio Sources window.",
            $"• {JLField.MonitorScreenshot.GetDescription()}: Screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.Image.GetDescription()}: Image found in clipboard at the time of mining.",
            $"• {JLField.ImageClipboardOverMonitorScreenshot.GetDescription()}: Image from the clipboard at the time of mining. If the clipboard does not contain an image, screenshot of the entire monitor where the main window is located at the time of mining.",
            $"• {JLField.DefinitionsImages.GetDescription()}: Images included in definitions.",
            $"• {JLField.SourceText.GetDescription()}: Whole text in which the mined name appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SourceTextNoBolding.GetDescription()}: Whole text in which the mined name appears in. Unlike the Source Text field, it does not bold the Matched Text.",
            $"• {JLField.LeadingSourceTextPart.GetDescription()}: Part of the source text that appears before the mined name.",
            $"• {JLField.TrailingSourceTextPart.GetDescription()}: Part of the source that appears after the mined name.",
            $"• {JLField.Sentence.GetDescription()}: Sentence in which the mined name appears in. Bolds the Matched Text by wrapping it in <b> tags.",
            $"• {JLField.SentenceNoBolding.GetDescription()}: Sentence in which the mined name appears in. Unlike the Sentence field, it does not bold the Matched Text.",
            $"""• {JLField.LeadingSentencePart.GetDescription()}: Part of the sentence that appears before the mined name. e.g., if the mined name is "エスト" while the sentence is "俺はエストのことが大好き", "Leading Sentence Part" will be "俺は".""",
            $"""• {JLField.TrailingSentencePart.GetDescription()}: Part of the sentence that appears after the mined name. e.g., if the mined name is "エスト" while the sentence is "俺はエストのことが大好き", "Trailing Sentence Part" will be "のことが大好き".""",
            $"• {JLField.PitchAccents.GetDescription()}: Pitch accents for the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.",
            $"""• {JLField.NumericPitchAccents.GetDescription()}: Pitch accents for the mined word in numeric form, e.g., "おんな: 3, おみな: 0, おうな: 1".""",
            $"• {JLField.PitchAccentForFirstReading.GetDescription()}: Pitch accent for the 'Selected Reading' of the mined word, displayed in a similar fashion to how pitch accents are shown in a JL popup.",
            $"""• {JLField.NumericPitchAccentForFirstReading.GetDescription()}: Pitch accent for the 'Selected Reading' of the mined word in numeric form, e.g., "おんな: 3".""",
            $"""• {JLField.PitchAccentCategories.GetDescription()}: Pitch accent categories for the mined word, e.g., "にほんご: Heiban, にっぽんご: Heiban". There are currently four pitch accent categories: Heiban, Atamadaka, Odaka and Nakadaka.""",
            $"""• {JLField.PitchAccentCategoryForFirstReading.GetDescription()}: Pitch accent category for the 'Selected Reading' of the mined word, e.g., "にほんご: Heiban". There are currently four pitch accent categories: Heiban, Atamadaka, Odaka and Nakadaka.""",
            $"• {JLField.EdictId.GetDescription()}: JMnedict entry ID.",
            $"• {JLField.LocalTime.GetDescription()}: Mining date and time expressed in local timezone."
        ];

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
        _profileNamesDict.Path = PathUtils.GetPortablePath(ProfileUtils.GetProfileCustomNameDictPath(ProfileUtils.CurrentProfileName));
        _profileWordsDict.Path = PathUtils.GetPortablePath(ProfileUtils.GetProfileCustomWordDictPath(ProfileUtils.CurrentProfileName));

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

        await Task.Run(DictUtils.LoadDictionaries).ConfigureAwait(false);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void PreferencesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized && ReferenceEquals(e.OriginalSource, sender))
        {
            await InitializeAnkiConfig(false).ConfigureAwait(false);
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void AnkiIntegrationCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (IsInitialized)
        {
            await InitializeAnkiConfig(false).ConfigureAwait(false);
        }
    }

    private async Task InitializeAnkiConfig(bool refresh)
    {
        if (!IsVisible || !AnkiTabItem.IsSelected || AnkiIntegrationCheckBox.IsChecked is not true
            || (!refresh && ApplyAnkiConnectSettingsButton.IsEnabled))
        {
            return;
        }

        if (_ankiConfigTask is not null)
        {
            await _ankiConfigTask.ConfigureAwait(false);
            return;
        }

        if (!refresh && SetAnkiConfig && _ankiNamesLoaded)
        {
            return;
        }

        if (refresh)
        {
            _ankiNamesLoaded = false;
        }

        _ankiConfigTask = LoadAnkiConfig();
        try
        {
            await _ankiConfigTask.ConfigureAwait(false);
        }
        finally
        {
            _ankiConfigTask = null;
        }
    }

    private async Task LoadAnkiConfig()
    {
        if (!SetAnkiConfig)
        {
            await SetPreviousMiningConfig().ConfigureAwait(true);
            if (!IsVisible)
            {
                return;
            }

            SetAnkiConfig = true;
        }

        do
        {
            CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
            Uri uri = coreConfigManager.AnkiConnectUri;
            string apiKey = coreConfigManager.AnkiConnectApiKey;
            _ankiNamesLoaded = await PopulateDeckAndModelNames().ConfigureAwait(true);
            if (!IsVisible || (uri == coreConfigManager.AnkiConnectUri && apiKey == coreConfigManager.AnkiConnectApiKey))
            {
                break;
            }
        }
        while (!_ankiNamesLoaded);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void CheckForJLUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        CheckForJLUpdatesButton.IsEnabled = false;
        await NetworkUtils.CheckForJLUpdates(false).ConfigureAwait(true);
        CheckForJLUpdatesButton.IsEnabled = true;
    }

    private void ResetPreferencesButton_Click(object sender, RoutedEventArgs e)
    {
        ResetPreferencesButton.IsEnabled = false;
        if (WindowsUtils.ShowYesNoDialog("Are you really sure that you want to reset all your preferences to their default values for the current profile?", "Reset preferences for the current profile?", this))
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
        bool wordTagsChanged = false;
        bool kanjiTagsChanged = false;
        bool nameTagsChanged = false;
        bool otherTagsChanged = false;

        void TagsTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == WordTagsTextBox)
            {
                wordTagsChanged = true;
            }

            else if (sender == KanjiTagsTextBox)
            {
                kanjiTagsChanged = true;
            }

            else if (sender == NameTagsTextBox)
            {
                nameTagsChanged = true;
            }

            else
            {
                otherTagsChanged = true;
            }
        }

        TextChangedEventHandler tagsTextChanged = TagsTextChanged;
        WordTagsTextBox.TextChanged += tagsTextChanged;
        KanjiTagsTextBox.TextChanged += tagsTextChanged;
        NameTagsTextBox.TextChanged += tagsTextChanged;
        OtherTagsTextBox.TextChanged += tagsTextChanged;
        Dictionary<MineType, AnkiConfig>? ankiConfigDict;
        try
        {
            ankiConfigDict = await AnkiConfigUtils.ReadAnkiConfig(CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            WordTagsTextBox.TextChanged -= tagsTextChanged;
            KanjiTagsTextBox.TextChanged -= tagsTextChanged;
            NameTagsTextBox.TextChanged -= tagsTextChanged;
            OtherTagsTextBox.TextChanged -= tagsTextChanged;
        }

        if (ankiConfigDict is null || !IsVisible)
        {
            return;
        }

        if (ankiConfigDict.TryGetValue(MineType.Word, out AnkiConfig? wordAnkiConfig))
        {
            SetPreviousMiningConfig(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordTagsTextBox, wordAnkiConfig, wordTagsChanged);
            CreateFieldElements(wordAnkiConfig.Fields, JLFieldUtils.JLFieldsForWordDicts, WordMiningSetupStackPanelFields);
        }

        if (ankiConfigDict.TryGetValue(MineType.Kanji, out AnkiConfig? kanjiAnkiConfig))
        {
            SetPreviousMiningConfig(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiTagsTextBox, kanjiAnkiConfig, kanjiTagsChanged);
            CreateFieldElements(kanjiAnkiConfig.Fields, JLFieldUtils.JLFieldsForKanjiDicts, KanjiMiningSetupStackPanelFields);
        }

        if (ankiConfigDict.TryGetValue(MineType.Name, out AnkiConfig? nameAnkiConfig))
        {
            SetPreviousMiningConfig(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameTagsTextBox, nameAnkiConfig, nameTagsChanged);
            CreateFieldElements(nameAnkiConfig.Fields, JLFieldUtils.JLFieldsForNameDicts, NameMiningSetupStackPanelFields);
        }

        if (ankiConfigDict.TryGetValue(MineType.Other, out AnkiConfig? otherAnkiConfig))
        {
            SetPreviousMiningConfig(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherTagsTextBox, otherAnkiConfig, otherTagsChanged);
            CreateFieldElements(otherAnkiConfig.Fields, JLFieldUtils.JLFieldsForWordDicts, OtherMiningSetupStackPanelFields);
        }
    }

    private static void SetPreviousMiningConfig(Selector deckNamesSelector, Selector modelNamesSelector, TextBox tagTextBox, AnkiConfig ankiConfig, bool tagsChanged)
    {
        deckNamesSelector.ItemsSource = new[]
        {
            ankiConfig.DeckName
        };
        deckNamesSelector.SelectedItem = ankiConfig.DeckName;
        modelNamesSelector.ItemsSource = new[]
        {
            ankiConfig.ModelName
        };
        modelNamesSelector.SelectedItem = ankiConfig.ModelName;

        if (!tagsChanged)
        {
            tagTextBox.Text = ankiConfig.Tags is not null
                ? string.Join(", ", ankiConfig.Tags)
                : "";
        }
    }

    private async Task<bool> PopulateDeckAndModelNames()
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        Uri uri = coreConfigManager.AnkiConnectUri;
        string apiKey = coreConfigManager.AnkiConnectApiKey;
        string[]? deckNames = await AnkiConnectUtils.GetDeckNames().ConfigureAwait(true);
        if (!IsVisible || uri != coreConfigManager.AnkiConnectUri || apiKey != coreConfigManager.AnkiConnectApiKey)
        {
            return false;
        }

        string[]? modelNames = deckNames is not null
            ? await AnkiConnectUtils.GetModelNames().ConfigureAwait(true)
            : null;

        if (!IsVisible || uri != coreConfigManager.AnkiConnectUri || apiKey != coreConfigManager.AnkiConnectApiKey)
        {
            return false;
        }

        if (deckNames is null)
        {
            LoggerManager.Logger.Error("Error getting deck names from Anki");
            NotificationManager.Notify(NotificationLevel.Error, "Error getting deck names from Anki");
            return false;
        }

        if (modelNames is null)
        {
            LoggerManager.Logger.Error("Error getting model names from Anki");
            NotificationManager.Notify(NotificationLevel.Error, "Error getting model names from Anki");
            return false;
        }

        // Separate item sources keep each category's selection independent.
        SetMiningSetupNames(WordMiningSetupComboBoxDeckNames, deckNames, copyNames: false);
        SetMiningSetupNames(KanjiMiningSetupComboBoxDeckNames, deckNames, copyNames: true);
        SetMiningSetupNames(NameMiningSetupComboBoxDeckNames, deckNames, copyNames: true);
        SetMiningSetupNames(OtherMiningSetupComboBoxDeckNames, deckNames, copyNames: true);
        SetMiningSetupNames(WordMiningSetupComboBoxModelNames, modelNames, copyNames: false);
        SetMiningSetupNames(KanjiMiningSetupComboBoxModelNames, modelNames, copyNames: true);
        SetMiningSetupNames(NameMiningSetupComboBoxModelNames, modelNames, copyNames: true);
        SetMiningSetupNames(OtherMiningSetupComboBoxModelNames, modelNames, copyNames: true);
        return true;
    }

    private static void SetMiningSetupNames(Selector selector, string[] names, bool copyNames)
    {
        string? selectedName = selector.SelectedItem as string;
        bool addSelectedName = selectedName is not null && !names.AsSpan().Contains(selectedName);
        if (copyNames || addSelectedName)
        {
            string[] newNames = new string[names.Length + (addSelectedName ? 1 : 0)];
            names.CopyTo(newNames, 0);
            if (addSelectedName)
            {
                newNames[^1] = selectedName!;
            }

            names = newNames;
        }

        selector.ItemsSource = names;
        selector.SelectedItem = selectedName;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MiningSetupButtonRefresh_Click(object sender, RoutedEventArgs e)
    {
        await InitializeAnkiConfig(true).ConfigureAwait(false);
    }

    private async Task GetFields(ComboBox modelNamesComboBox, Panel miningPanel, JLField[] fieldList)
    {
        string? modelName = (string?)modelNamesComboBox.SelectedItem;
        if (string.IsNullOrEmpty(modelName))
        {
            NotificationManager.Notify(NotificationLevel.Error, "Please select a note type first");
            return;
        }

        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        Uri uri = coreConfigManager.AnkiConnectUri;
        string apiKey = coreConfigManager.AnkiConnectApiKey;
        string[]? fieldNames = await AnkiConnectUtils.GetFieldNames(modelName, CancellationToken.None).ConfigureAwait(true);
        if (!IsVisible || (string?)modelNamesComboBox.SelectedItem != modelName
            || uri != coreConfigManager.AnkiConnectUri || apiKey != coreConfigManager.AnkiConnectApiKey)
        {
            return;
        }

        if (fieldNames is not null)
        {
            OrderedDictionary<string, JLField> fields = new(fieldNames.Length, StringComparer.Ordinal);
            foreach (string fieldName in fieldNames)
            {
                fields.Add(fieldName, JLField.Nothing);
            }

            CreateFieldElements(fields, fieldList, miningPanel);
        }
        else
        {
            NotificationManager.Notify(NotificationLevel.Error, "Error getting fields from AnkiConnect");
            LoggerManager.Logger.Error("Error getting fields from AnkiConnect");
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

    private void CreateFieldElements(OrderedDictionary<string, JLField> fields, JLField[] fieldList, Panel fieldPanel)
    {
        fieldPanel.Children.Clear();

        string[] descriptions = new string[fieldList.Length];
        for (int i = 0; i < fieldList.Length; i++)
        {
            JLField jLField = fieldList[i];
            descriptions[i] = jLField.GetDescription();
        }

        int fieldsCount = fields.Count;
        for (int i = 0; i < fieldsCount; i++)
        {
            (string fieldName, JLField jlField) = fields.GetAt(i);
            StackPanel stackPanel = new();
            TextBlock textBlockFieldName = new()
            {
                Text = fieldName
            };
            ComboBox comboBoxJLFields = new()
            {
                ItemsSource = descriptions,
                SelectedItem = jlField.GetDescription(),
                Margin = new Thickness(0, 10, 0, 15)
            };

            _ = stackPanel.Children.Add(textBlockFieldName);
            _ = stackPanel.Children.Add(comboBoxJLFields);
            _ = fieldPanel.Children.Add(stackPanel);
        }

        UpdateMiningSetupStatus(fieldPanel);
    }

    private void WordMiningSetup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized)
        {
            UpdateMiningSetupStatus(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, WordMiningSetupStatusTextBlock);
        }
    }

    private void KanjiMiningSetup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized)
        {
            UpdateMiningSetupStatus(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, KanjiMiningSetupStatusTextBlock);
        }
    }

    private void NameMiningSetup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized)
        {
            UpdateMiningSetupStatus(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, NameMiningSetupStatusTextBlock);
        }
    }

    private void OtherMiningSetup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized)
        {
            UpdateMiningSetupStatus(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, OtherMiningSetupStatusTextBlock);
        }
    }

    private void UpdateMiningSetupStatus(Panel fieldPanel)
    {
        if (fieldPanel == WordMiningSetupStackPanelFields)
        {
            UpdateMiningSetupStatus(WordMiningSetupComboBoxDeckNames, WordMiningSetupComboBoxModelNames, WordMiningSetupStackPanelFields, WordMiningSetupStatusTextBlock);
        }

        else if (fieldPanel == KanjiMiningSetupStackPanelFields)
        {
            UpdateMiningSetupStatus(KanjiMiningSetupComboBoxDeckNames, KanjiMiningSetupComboBoxModelNames, KanjiMiningSetupStackPanelFields, KanjiMiningSetupStatusTextBlock);
        }

        else if (fieldPanel == NameMiningSetupStackPanelFields)
        {
            UpdateMiningSetupStatus(NameMiningSetupComboBoxDeckNames, NameMiningSetupComboBoxModelNames, NameMiningSetupStackPanelFields, NameMiningSetupStatusTextBlock);
        }

        else
        {
            UpdateMiningSetupStatus(OtherMiningSetupComboBoxDeckNames, OtherMiningSetupComboBoxModelNames, OtherMiningSetupStackPanelFields, OtherMiningSetupStatusTextBlock);
        }
    }

    private static void UpdateMiningSetupStatus(Selector deckNamesSelector, Selector modelNamesSelector, Panel fieldPanel, UIElement status)
    {
        if (deckNamesSelector.SelectedItem is not null && modelNamesSelector.SelectedItem is not null)
        {
            // Nothing is the first option in each field list.
            foreach (object child in fieldPanel.Children)
            {
                StackPanel field = (StackPanel)child;
                if (((ComboBox)field.Children[1]).SelectedIndex > 0)
                {
                    status.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }

        status.Visibility = Visibility.Visible;
    }

    private static AnkiConfig? GetAnkiConfigFromPreferences(Selector deckNamesSelector, Selector modelNamesSelector, Panel miningPanel, TextBox tagsTextBox, JLField[] jlFieldList, MineType mineType)
    {
        if (deckNamesSelector.SelectedItem is null || modelNamesSelector.SelectedItem is null)
        {
            LoggerManager.Logger.Error("Save failed: Incomplete Anki config for {MineType} dictionaries", mineType);
            NotificationManager.Notify(NotificationLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Save failed for {mineType} Anki config: Please complete the Anki configuration for {mineType} dictionaries first."));
            return null;
        }

        string? deckName = deckNamesSelector.SelectedItem.ToString();
        Debug.Assert(deckName is not null);

        string? modelName = modelNamesSelector.SelectedItem.ToString();
        Debug.Assert(modelName is not null);

        OrderedDictionary<string, JLField> dict = new(miningPanel.Children.Count, StringComparer.Ordinal);
        foreach (object child in miningPanel.Children)
        {
            StackPanel stackPanel = (StackPanel)child;
            ComboBox comboBox = (ComboBox)stackPanel.Children[1];
            TextBlock textBlock = (TextBlock)stackPanel.Children[0];
            int selectedIndex = comboBox.SelectedIndex;
            JLField jlField = selectedIndex < 0
                ? JLField.Nothing
                : jlFieldList[selectedIndex];
            dict.Add(textBlock.Text, jlField);
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

        Dictionary<MineType, AnkiConfig> ankiConfigDict = new(4);

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
            return AnkiConfigUtils.WriteAnkiConfig(ankiConfigDict);
        }

        LoggerManager.Logger.Error("Error saving AnkiConfig");
        NotificationManager.Notify(NotificationLevel.Error, "Error saving AnkiConfig");
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
            StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

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

            ObjectPoolManager.StringBuilderPool.Return(sb);
        }

        TextBox currentTextBox = (TextBox)sender;

        if (LookupKeyKeyGestureTextBox != currentTextBox
            && LookupKeyKeyGestureTextBox.Text == hotKeyText)
        {
            return;
        }

        currentTextBox.Text = hotKeyText;

        // Logical children include bindings hidden by the search filter.
        foreach (object item in LogicalTreeHelper.GetChildren(HotkeysPreferencesListBox))
        {
            ListBoxItem listBoxItem = (ListBoxItem)item;
            DockPanel dockPanel = (DockPanel)listBoxItem.Content;
            TextBox textBox = (TextBox)dockPanel.Children[1];
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

    private void AnkiConnectSettings_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        ApplyAnkiConnectSettingsButton.IsEnabled = AnkiUriTextBox.Text != coreConfigManager.AnkiConnectUri.OriginalString
            || AnkiConnectApiKeyTextBox.Text != coreConfigManager.AnkiConnectApiKey;
        AnkiConnectSettingsStatusTextBlock.Text = "";
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ApplyAnkiConnectSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        string normalizedUrl = AnkiUriTextBox.Text
            .Replace(NetworkUtils.AllIpAddressToReplace, NetworkUtils.NormalizedLocalhostString, StringComparison.Ordinal)
            .Replace(NetworkUtils.LocalhostStringToReplace, NetworkUtils.NormalizedLocalhostString, StringComparison.OrdinalIgnoreCase);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uri) || !uri.IsWellFormedOriginalString())
        {
            AnkiConnectSettingsStatusTextBlock.Text = "Invalid server address.";
            return;
        }

        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.AnkiConnectUri = uri;
        coreConfigManager.AnkiConnectApiKey = AnkiConnectApiKeyTextBox.Text;
        AnkiUriTextBox.Text = normalizedUrl;
        ApplyAnkiConnectSettingsButton.IsEnabled = false;
        AnkiConnectSettingsStatusTextBlock.Text = "Settings applied.";
        _ankiNamesLoaded = false;
        await InitializeAnkiConfig(false).ConfigureAwait(false);
    }

    private void Button_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Button button = (Button)sender;

        string title = "JL Fields for ";
        string[] itemArray;

        if (WordInfoButton == button)
        {
            title += "Words";
            itemArray = s_wordJLFieldsInfo;
        }

        else if (KanjiInfoButton == button)
        {
            title += "Kanjis";
            itemArray = s_kanjiJLFieldsInfo;
        }

        else if (NameInfoButton == button)
        {
            title += "Names";
            itemArray = s_nameJLFieldsInfo;
        }

        else // if (OtherInfoButton == button)
        {
            title += "Others";
            itemArray = s_wordJLFieldsInfo;
        }

        InfoWindow infoWindow = new(itemArray)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Title = title
        };

        _ = infoWindow.ShowDialog();
    }

    private async void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string selectedProfileName = (string)((ComboBox)sender).SelectedItem;
        if (selectedProfileName == ProfileUtils.CurrentProfileName)
        {
            return;
        }

        // ReSharper disable once UseAwaitUsing
        using (SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection())
        {
            StatsUtils.UpdateTimeStats(restartStopWatch: false);
            StatsDBUtils.UpdateProfileLifetimeStats(connection);

            BacklogUtils.StopBacklogTimer();
            await BacklogUtils.WriteBacklog().ConfigureAwait(true);
            BacklogUtils.ClearBacklog();

            int selectedProfileId = ProfileDBUtils.GetProfileId(connection, selectedProfileName);
            Stats selectedProfileStats = StatsDBUtils.GetStatsFromDB(connection, selectedProfileId);
            lock (StatsUtils.TimeStatsLock)
            {
                lock (StatsUtils.TermLookupCountsLock)
                {
                    ProfileUtils.CurrentProfileName = selectedProfileName;
                    ProfileUtils.CurrentProfileId = selectedProfileId;
                    ProfileUtils.CurrentProfileSessionStartTime = DateTime.Now;
                    StatsUtils.ProfileLifetimeStats = selectedProfileStats;
                }
            }

            ProfileDBUtils.UpdateCurrentProfile(connection);
        }

        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        Uri previousAnkiUri = coreConfigManager.AnkiConnectUri;
        string previousAnkiApiKey = coreConfigManager.AnkiConnectApiKey;
        ConfigManager configManager = ConfigManager.Instance;
        // ReSharper disable once UseAwaitUsing
        using (SqliteConnection preferencesConnection = ConfigDBManager.CreateReadWriteDBConnection())
        {
            configManager.ApplyPreferences(preferencesConnection);
        }
        if (previousAnkiUri != coreConfigManager.AnkiConnectUri || previousAnkiApiKey != coreConfigManager.AnkiConnectApiKey)
        {
            _ankiNamesLoaded = false;
        }

        configManager.LoadPreferenceWindow(this);
        RegexReplacerUtils.PopulateRegexReplacements();
        await InitializeAnkiConfig(false).ConfigureAwait(false);
    }

    private void ProfileConfigButton_Click(object sender, RoutedEventArgs e)
    {
        _ = new ManageProfilesWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();
    }

    private void ListBoxItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        ListBoxItem listBoxItem = (ListBoxItem)sender;
        DockPanel dockPanel = (DockPanel)listBoxItem.Content;
        if (dockPanel.Children.Count is 2)
        {
            UIElement inputItem = dockPanel.Children[1];
            if (inputItem is TextBox)
            {
                _ = inputItem.Focus();
            }
        }
    }

    private void GeneralPreferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        GeneralPreferencesListBox.Items.Filter = GeneralPreferencesFilter;
    }

    private void MainWindowPreferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        MainWindowPreferencesListBox.Items.Filter = MainWindowPreferencesFilter;
    }

    private void PopupPreferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PopupPreferencesListBox.Items.Filter = PopupPreferencesFilter;
    }

    private void HotkeysPreferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        HotkeysPreferencesListBox.Items.Filter = HotkeysPreferencesFilter;
    }

    private void AdvancedPreferencesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AdvancedPreferencesListBox.Items.Filter = AdvancedPreferencesFilter;
    }

    private bool GeneralPreferencesFilter(object item)
    {
        ListBoxItem listBoxItem = (ListBoxItem)item;
        string preferenceName = ((TextBlock)((DockPanel)listBoxItem.Content).Children[0]).Text;
        return preferenceName.AsSpan().Contains(GeneralPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) || (listBoxItem.ToolTip?.ToString()?.AsSpan().Contains(GeneralPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool MainWindowPreferencesFilter(object item)
    {
        ListBoxItem listBoxItem = (ListBoxItem)item;
        string preferenceName = ((TextBlock)((DockPanel)listBoxItem.Content).Children[0]).Text;
        return preferenceName.AsSpan().Contains(MainWindowPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) || (listBoxItem.ToolTip?.ToString()?.AsSpan().Contains(MainWindowPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool PopupPreferencesFilter(object item)
    {
        ListBoxItem listBoxItem = (ListBoxItem)item;
        string preferenceName = ((TextBlock)((DockPanel)listBoxItem.Content).Children[0]).Text;
        return preferenceName.AsSpan().Contains(PopupPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) || (listBoxItem.ToolTip?.ToString()?.AsSpan().Contains(PopupPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool HotkeysPreferencesFilter(object item)
    {
        ListBoxItem listBoxItem = (ListBoxItem)item;
        string preferenceName = ((TextBlock)((DockPanel)listBoxItem.Content).Children[2]).Text;
        return preferenceName.AsSpan().Contains(HotkeysPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) || (listBoxItem.ToolTip?.ToString()?.AsSpan().Contains(HotkeysPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool AdvancedPreferencesFilter(object item)
    {
        ListBoxItem listBoxItem = (ListBoxItem)item;
        string preferenceName = ((TextBlock)((DockPanel)listBoxItem.Content).Children[0]).Text;
        return preferenceName.AsSpan().Contains(AdvancedPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) || (listBoxItem.ToolTip?.ToString()?.AsSpan().Contains(AdvancedPreferencesSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void MainWindowFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? selectedFont = (string?)((ComboBox)sender).SelectedValue;
        if (selectedFont is null)
        {
            return;
        }

        ComboBoxItem[] fontWeightNames = WindowsUtils.GetFontWeightNames(selectedFont);
        if (fontWeightNames.Length is 0)
        {
            LoggerManager.Logger.Error("No font weights found for font {FontFamily}", selectedFont);
            fontWeightNames = [new ComboBoxItem
            {
                Content = "Normal",
                FontFamily = new System.Windows.Media.FontFamily(selectedFont),
                FontWeight = FontWeights.Normal
            }];
        }

        ConfigManager.MainWindowFontWeights = fontWeightNames;
        MainWindowFontWeightComboBox.ItemsSource = fontWeightNames;

        string? selectedFontWeight = (string?)MainWindowFontWeightComboBox.SelectedValue;
        int index = Array.FindIndex(fontWeightNames, fw => (string)fw.Content == selectedFontWeight);
        if (index < 0)
        {
            ReadOnlySpan<string> fallbackOrder = [ "Normal", "Regular", "Medium", "DemiBold", "SemiBold", "Bold", "ExtraBold", "UltraBold",
                "Light", "Black", "Heavy", "ExtraLight", "UltraLight", "Thin", "ExtraBlack", "UltraBlack"];

            foreach (string name in fallbackOrder)
            {
                index = Array.FindIndex(fontWeightNames, fw => (string)fw.Content == name);
                if (index >= 0)
                {
                    break;
                }
            }
        }

        MainWindowFontWeightComboBox.SelectedIndex = index < 0 ? 0 : index;
    }
}
