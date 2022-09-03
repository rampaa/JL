using System.ComponentModel;

namespace JL.Core.Anki;

public enum JLField
{
    // Shared
    Nothing,
    [Description("Primary Spelling")] PrimarySpelling,
    Readings,
    Definitions,
    [Description("Alternative Spellings")] AlternativeSpellings,
    [Description("Dictionary Name")] DictionaryName,
    Audio,
    Sentence,
    [Description("Clipboard Text")] ClipboardText,
    [Description("Matched Text")] MatchedText,
    [Description("EDICT ID")] EdictId,
    [Description("Local Time")] LocalTime,
    Frequencies,
    // Screenshot? Image? Picture?

    // Word dictionaries
    [Description("Deconjugated Matched Text")] DeconjugatedMatchedText,
    [Description("Deconjugation Process")] DeconjugationProcess,

    //Kanji dictionaries
    [Description("Kun Readings")] KunReadings,
    [Description("On Readings")] OnReadings,
    Nanori,
    [Description("Stroke Count")] StrokeCount,
    [Description("Kanji Grade")] KanjiGrade,
    [Description("Kanji Composition")] KanjiComposition,
}
