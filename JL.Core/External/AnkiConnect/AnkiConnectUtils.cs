using System.Diagnostics;
using System.Text.Json;
using JL.Core.Audio;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Frontend;
using JL.Core.Lookup;
using JL.Core.Mining;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Core.Utilities.Japanese;
namespace JL.Core.External.AnkiConnect;

public static class AnkiConnectUtils
{
    private static long s_lastAddedNoteId;

    internal static Dictionary<string, object?> AnkiOptions { get; } = [];
    internal static Dictionary<string, object?> CheckDuplicateOptions { get; } = [];

    public static async ValueTask<string[]?> GetDeckNames()
    {
        Response? response = await AnkiConnectClient.GetDeckNamesResponse().ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] deckNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? deckName = element.GetString();
            Debug.Assert(deckName is not null);
            deckNames[index] = deckName;
            ++index;
        }

        return deckNames;
    }

    public static async ValueTask<string[]?> GetModelNames()
    {
        Response? response = await AnkiConnectClient.GetModelNamesResponse().ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] modelNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? modelName = element.GetString();
            Debug.Assert(modelName is not null);
            modelNames[index] = modelName;
            ++index;
        }

        return modelNames;
    }

    public static async ValueTask<string[]?> GetFieldNames(string modelName, CancellationToken cancellationToken)
    {
        Response? response = await AnkiConnectClient.GetModelFieldNamesResponse(modelName, cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] fieldNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? fieldName = element.GetString();
            Debug.Assert(fieldName is not null);
            fieldNames[index] = fieldName;
            ++index;
        }

        return fieldNames;
    }

    internal static async Task<bool?> CanAddNote(Note note)
    {
        Response? response = await AnkiConnectClient.GetCanAddNotesResponse([note], CancellationToken.None).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        foreach (JsonElement element in result.EnumerateArray())
        {
            return element.GetBoolean();
        }

        return null;
    }

    internal static async ValueTask<bool[]?> CanAddNotes(List<Note> notes, CancellationToken cancellationToken)
    {
        Response? response = await AnkiConnectClient.GetCanAddNotesResponse(notes, cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        bool[] canAddNotesArray = new bool[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            canAddNotesArray[index] = element.GetBoolean();
            ++index;
        }

        return canAddNotesArray;
    }

    public static async Task OpenLastestNoteInAnki()
    {
        long noteId = s_lastAddedNoteId;
        if (noteId is not 0)
        {
            await AnkiConnectClient.GuiBrowse($"nid:{noteId}").ConfigureAwait(false);
        }
    }

    public static async Task Mine(LookupResult[] lookupResults, int currentLookupResultIndex, string currentText, string? formattedDefinitions, string? selectedDefinitions, int currentCharPosition, string selectedSpelling)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        if (!coreConfigManager.AnkiIntegration)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        Dictionary<MineType, AnkiConfig>? ankiConfigDict = await AnkiConfigUtils.ReadAnkiConfig(CancellationToken.None).ConfigureAwait(false);
        if (ankiConfigDict is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        AnkiConfig? ankiConfig;
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
        if (DictUtils.s_wordDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Word, out ankiConfig);
        }
        else if (DictUtils.KanjiDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Kanji, out ankiConfig);
        }
        else if (DictUtils.s_nameDictTypes.Contains(lookupResult.Dict.Type))
        {
            _ = ankiConfigDict.TryGetValue(MineType.Name, out ankiConfig);
        }
        else
        {
            _ = ankiConfigDict.TryGetValue(MineType.Other, out ankiConfig);
        }

        if (ankiConfig is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Please setup mining first in the preferences");
            return;
        }

        string sentence = JapaneseUtils.FindSentence(currentText, currentCharPosition);
        Dictionary<JLField, string> miningParams = MiningUtils.GetMiningParameters(lookupResults, currentLookupResultIndex, currentText, sentence, formattedDefinitions, selectedDefinitions, currentCharPosition, selectedSpelling, true, ankiConfig.UsedJLFields);
        OrderedDictionary<string, JLField> userFields = ankiConfig.Fields;
        Dictionary<string, string> fields = ConvertFields(userFields, miningParams);

        if (fields.Count is 0)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Cannot mine {selectedSpelling} because there is nothing to mine");
            LoggerManager.Logger.Information("Cannot mine {SelectedSpelling} because there is nothing to mine", selectedSpelling);
            return;
        }

        // Audio/Picture/Video shouldn't be set here
        // Otherwise AnkiConnect will place them under the "collection.media" folder even when it's a duplicate note
        Note note = new(ankiConfig.DeckName, ankiConfig.ModelName, fields, CheckDuplicateOptions);
        bool? canAddNote = await CanAddNote(note).ConfigureAwait(false);
        if (canAddNote is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            LoggerManager.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        if (!coreConfigManager.AllowDuplicateCards && !canAddNote.Value)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Cannot mine {selectedSpelling} because it is a duplicate card");
            LoggerManager.Logger.Information("Cannot mine {SelectedSpelling} because it is a duplicate card", selectedSpelling);
            return;
        }

        note.Tags = ankiConfig.Tags;
        note.Options = AnkiOptions;

        string selectedReading = selectedSpelling == lookupResult.PrimarySpelling && lookupResult.Readings is not null
            ? lookupResult.Readings[0]
            : selectedSpelling;

        List<Dictionary<string, object>>? imageDictionaries = null;

        List<string> imageClipboardOverMonitorScreenshotFields = FindFields(JLField.ImageClipboardOverMonitorScreenshot, userFields);
        bool imageClipboardOverMonitorScreenshotFieldsExist = imageClipboardOverMonitorScreenshotFields.Count > 0;

        List<string> screenshotFields = FindFields(JLField.MonitorScreenshot, userFields);
        bool screenshotFieldsExist = screenshotFields.Count > 0;

        byte[]? screenshotBytes = screenshotFieldsExist || imageClipboardOverMonitorScreenshotFieldsExist
            ? FrontendManager.Frontend.GetMonitorScreenshotAsByteArray()
            : null;

        if (screenshotBytes is not null)
        {
            List<string> imageFields = screenshotFieldsExist && imageClipboardOverMonitorScreenshotFieldsExist
                ? [.. screenshotFields, .. imageClipboardOverMonitorScreenshotFields]
                : imageClipboardOverMonitorScreenshotFieldsExist
                    ? imageClipboardOverMonitorScreenshotFields
                    : screenshotFields;

            Dictionary<string, object> screenshotDictionary = new(3, StringComparer.Ordinal)
            {
                {
                    "data", screenshotBytes
                },
                {
                    "filename", $"JL_SS_{selectedReading}_{lookupResult.PrimarySpelling}.jpg"
                },
                {
                    "fields", imageFields
                }
            };

            imageDictionaries = [screenshotDictionary];
        }

        if (lookupResult.ImagePaths is not null)
        {
            List<string> definitionsImagesFields = FindFields(JLField.DefinitionsImages, userFields);
            if (definitionsImagesFields.Count > 0)
            {
                if (imageDictionaries is null)
                {
                    imageDictionaries = new List<Dictionary<string, object>>(lookupResult.ImagePaths.Length + 1);
                }
                else
                {
                    _ = imageDictionaries.EnsureCapacity(imageDictionaries.Count + lookupResult.ImagePaths.Length + 1);
                }

                for (int i = 0; i < lookupResult.ImagePaths.Length; i++)
                {
                    string definitionsImagePath = lookupResult.ImagePaths[i];
                    string ext = Path.GetExtension(definitionsImagePath);
                    string definitionsImageFullPath = Path.GetFullPath(definitionsImagePath, AppInfo.ApplicationPath);
                    imageDictionaries.Add(new Dictionary<string, object>(3, StringComparer.Ordinal)
                    {
                        {
                            "path", definitionsImageFullPath
                        },
                        {
                            "filename", $"JL_definitions_image_{i}_{selectedReading}_{lookupResult.PrimarySpelling}{ext}"
                        },
                        {
                            "fields", definitionsImagesFields
                        }
                    });
                }
            }
        }

        List<string> clipboardImageFields = FindFields(JLField.Image, userFields);
        bool clipboardImageFieldsExist = clipboardImageFields.Count > 0;
        bool fallbackToClipboardImage = imageClipboardOverMonitorScreenshotFieldsExist && screenshotBytes is null;

        byte[]? clipboardImageBytes = clipboardImageFieldsExist || fallbackToClipboardImage
            ? await FrontendManager.Frontend.GetImageFromClipboardAsByteArray().ConfigureAwait(false)
            : null;

        if (clipboardImageBytes is not null)
        {
            List<string> imageFields = clipboardImageFieldsExist && fallbackToClipboardImage
                ? [.. clipboardImageFields, .. imageClipboardOverMonitorScreenshotFields]
                : clipboardImageFieldsExist
                    ? clipboardImageFields
                    : imageClipboardOverMonitorScreenshotFields;

            Dictionary<string, object> clipboardImageDictionary = new(3, StringComparer.Ordinal)
            {
                {
                    "data", clipboardImageBytes
                },
                {
                    "filename", $"JL_image_{selectedReading}_{lookupResult.PrimarySpelling}.png"
                },
                {
                    "fields", imageFields
                }
            };

            if (imageDictionaries is not null)
            {
                imageDictionaries.Add(clipboardImageDictionary);
            }
            else
            {
                note.Pictures = [clipboardImageDictionary];
            }
        }

        if (imageDictionaries is not null)
        {
            note.Pictures = imageDictionaries.ToArray();
        }

        List<string> audioFields = FindFields(JLField.Audio, userFields);
        bool needsAudio = audioFields.Count > 0;
        AudioResponse? audioResponse = needsAudio
            ? await AudioUtils.GetPrioritizedAudio(lookupResult.PrimarySpelling, selectedReading).ConfigureAwait(false)
            : null;

        byte[]? audioData = audioResponse?.AudioData;
        if (audioResponse?.AudioSource is AudioSourceType.TextToSpeech)
        {
            audioData = FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(selectedReading);
        }

        List<string> sentenceAudioFields = FindFields(JLField.SentenceAudio, userFields);
        bool needsSentenceAudio = sentenceAudioFields.Count > 0;
        bool sentenceAudioIsSameAsAudio = needsSentenceAudio && audioData is not null && sentence == lookupResult.PrimarySpelling;
        byte[]? sentenceAudioData = needsSentenceAudio
            ? sentenceAudioIsSameAsAudio
                ? audioData
                : FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(sentence)
            : null;

        List<string> sourceTextAudioFields = FindFields(JLField.SourceTextAudio, userFields);
        bool needsSourceTextAudio = sourceTextAudioFields.Count > 0;
        bool sourceTextAudioIsSameAsSentenceAudio = needsSourceTextAudio && sentenceAudioData is not null && currentText == sentence;
        byte[]? sourceTextAudioData = needsSourceTextAudio
            ? sourceTextAudioIsSameAsSentenceAudio
                ? sentenceAudioData
                : FrontendManager.Frontend.GetAudioResponseFromTextToSpeech(currentText)
            : null;

        int totalAudioCount = 0;
        if (audioData is not null)
        {
            ++totalAudioCount;
        }
        if (sentenceAudioData is not null)
        {
            ++totalAudioCount;
        }
        if (sourceTextAudioData is not null)
        {
            ++totalAudioCount;
        }

        if (totalAudioCount > 0)
        {
            note.Audios = new Dictionary<string, object>[totalAudioCount];

            int audioIndex = 0;
            if (audioData is not null)
            {
                Debug.Assert(audioResponse is not null);
                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", audioData
                        },
                        {
                            "filename", $"JL_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{audioResponse.AudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", audioFields
                        }
                    };

                ++audioIndex;
            }

            string? sentenceAudioFormat = null;
            if (sentenceAudioData is not null)
            {
                Debug.Assert(!sentenceAudioIsSameAsAudio || audioResponse is not null);
                sentenceAudioFormat = sentenceAudioIsSameAsAudio
                    ? audioResponse!.AudioFormat
                    : AudioUtils.s_textToSpeechAudioResponse.AudioFormat;

                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", sentenceAudioData
                        },
                        {
                            "filename", $"JL_sentence_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{sentenceAudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", sentenceAudioFields
                        }
                    };

                ++audioIndex;
            }

            if (sourceTextAudioData is not null)
            {
                Debug.Assert(!sourceTextAudioIsSameAsSentenceAudio || sentenceAudioFormat is not null);
                string sourceTextAudioFormat = sourceTextAudioIsSameAsSentenceAudio
                    ? sentenceAudioFormat!
                    : AudioUtils.s_textToSpeechAudioResponse.AudioFormat;

                note.Audios[audioIndex] = new Dictionary<string, object>(4, StringComparer.Ordinal)
                    {
                        {
                            "data", sourceTextAudioData
                        },
                        {
                            "filename", $"JL_source_text_audio_{selectedReading}_{lookupResult.PrimarySpelling}.{sourceTextAudioFormat}"
                        },
                        {
                            "skipHash", NetworkUtils.Jpod101NoAudioMd5Hash
                        },
                        {
                            "fields", sourceTextAudioFields
                        }
                    };
            }
        }

        Response? response = await AnkiConnectClient.AddNoteToDeck(note).ConfigureAwait(false);
        if (response is null)
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, $"Mining failed for {selectedSpelling}");
            LoggerManager.Logger.Error("Mining failed for {SelectedSpelling}", selectedSpelling);
            return;
        }

        bool showNoAudioMessage = needsAudio && audioData is null;
        bool showDuplicateCardMessage = !canAddNote.Value;
        string message = $"Mined {selectedSpelling}{(showNoAudioMessage ? " (No Audio)" : "")}{(showDuplicateCardMessage ? " (Duplicate)" : "")}";

        LoggerManager.Logger.Information("{Message}", message);
        if (coreConfigManager.NotifyWhenMiningSucceeds)
        {
            FrontendManager.Frontend.Alert(showNoAudioMessage || showDuplicateCardMessage ? AlertLevel.Warning : AlertLevel.Success, message);
        }

        if (coreConfigManager.ForceSyncAnki)
        {
            await AnkiConnectClient.Sync().ConfigureAwait(false);
        }

        long noteId = response.Result.GetInt64();
        s_lastAddedNoteId = noteId;

        if (coreConfigManager.AutoShowAnkiNoteAfterMining)
        {
            await AnkiConnectClient.GuiBrowse($"nid:{noteId}").ConfigureAwait(false);
        }

        StatsUtils.IncrementStat(StatType.CardsMined);
    }

    /// <summary>
    /// Converts JLField,Value pairs to UserField,Value pairs <br/>
    /// JLField is our internal name of a mining field <br/>
    /// Value is the actual content of a mining field (e.g. if the field name is LocalTime, then it should contain the current time) <br/>
    /// UserField is the name of the user's field in Anki (e.g. Expression) <br/>
    /// </summary>
    private static Dictionary<string, string> ConvertFields(OrderedDictionary<string, JLField> userFields, Dictionary<JLField, string> miningParams)
    {
        Dictionary<string, string> dict = new(userFields.Count, StringComparer.Ordinal);
        int userFieldsCount = userFields.Count;
        for (int i = 0; i < userFieldsCount; i++)
        {
            (string key, JLField value) = userFields.GetAt(i);
            if (miningParams.TryGetValue(value, out string? fieldValue))
            {
                dict.Add(key, fieldValue);
            }
        }

        return dict;
    }

    private static List<string> FindFields(JLField jlField, OrderedDictionary<string, JLField> userFields)
    {
        List<string> matchingFieldNames = [];
        foreach ((string fieldName, JLField fieldValue) in userFields)
        {
            if (fieldValue == jlField)
            {
                matchingFieldNames.Add(fieldName);
            }
        }

        return matchingFieldNames;
    }
}
