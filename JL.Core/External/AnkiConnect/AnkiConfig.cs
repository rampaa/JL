using System.Collections.Frozen;
using System.Text.Json.Serialization;
using JL.Core.Mining;

namespace JL.Core.External.AnkiConnect;

public sealed class AnkiConfig(string deckName, string modelName, OrderedDictionary<string, JLField> fields, string[]? tags = null)
{
    [JsonPropertyName("deckName")] public string DeckName { get; } = deckName;
    [JsonPropertyName("modelName")] public string ModelName { get; } = modelName;
    [JsonPropertyName("fields")] public OrderedDictionary<string, JLField> Fields { get; internal set; } = fields;
    [JsonPropertyName("tags")] public string[]? Tags { get; } = tags;
    [JsonIgnore] internal FrozenSet<JLField> UsedJLFields { get; set; } = fields.Values.Where(static f => f is not JLField.Nothing).ToFrozenSet();
    [JsonIgnore] internal string? FirstFieldName { get; private set; }
    [JsonIgnore] internal JLField FirstField { get; private set; }
    [JsonIgnore] internal MineType MineType { get; set; }
    [JsonIgnore] internal string[]? ImageClipboardOverMonitorScreenshotFields { get; private set; }
    [JsonIgnore] internal string[]? ClipboardImageFields { get; private set; }
    [JsonIgnore] internal string[]? ScreenshotFields { get; private set; }
    [JsonIgnore] internal string[]? AudioFields { get; private set; }
    [JsonIgnore] internal string[]? SentenceAudioFields { get; private set; }
    [JsonIgnore] internal string[]? SourceTextAudioFields { get; private set; }
    [JsonIgnore] internal string[]? DefinitionsImagesFields { get; private set; }
    [JsonIgnore] internal string[]? ScreenshotWithFallbackFields { get; private set; }
    [JsonIgnore] internal string[]? ClipboardImageWithFallbackFields { get; private set; }

    internal void PrepareMiningFields()
    {
        if (Fields.Count > 0)
        {
            (FirstFieldName, FirstField) = Fields.GetAt(0);
        }
        else
        {
            FirstFieldName = null;
            FirstField = JLField.Nothing;
        }

        ImageClipboardOverMonitorScreenshotFields = GetFieldNames(JLField.ImageClipboardOverMonitorScreenshot);
        ClipboardImageFields = GetFieldNames(JLField.Image);
        ScreenshotFields = GetFieldNames(JLField.MonitorScreenshot);
        AudioFields = GetFieldNames(JLField.Audio);
        SentenceAudioFields = GetFieldNames(JLField.SentenceAudio);
        SourceTextAudioFields = GetFieldNames(JLField.SourceTextAudio);
        DefinitionsImagesFields = GetFieldNames(JLField.DefinitionsImages);

        ScreenshotWithFallbackFields = ScreenshotFields is null ? ImageClipboardOverMonitorScreenshotFields
            : ImageClipboardOverMonitorScreenshotFields is null ? ScreenshotFields
            : [.. ScreenshotFields, .. ImageClipboardOverMonitorScreenshotFields];

        ClipboardImageWithFallbackFields = ClipboardImageFields is null ? ImageClipboardOverMonitorScreenshotFields
            : ImageClipboardOverMonitorScreenshotFields is null ? ClipboardImageFields
            : [.. ClipboardImageFields, .. ImageClipboardOverMonitorScreenshotFields];
    }

    private string[]? GetFieldNames(JLField field)
    {
        int count = 0;
        foreach (JLField configuredField in Fields.Values)
        {
            if (configuredField == field)
            {
                ++count;
            }
        }

        if (count is 0)
        {
            return null;
        }

        string[] fieldNames = new string[count];
        int index = 0;
        foreach ((string fieldName, JLField configuredField) in Fields)
        {
            if (configuredField == field)
            {
                fieldNames[index] = fieldName;
                ++index;
            }
        }

        return fieldNames;
    }
}
