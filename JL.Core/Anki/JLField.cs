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
    Image,
    Sentence,
    [Description("Source Text")] SourceText,
    [Description("Matched Text")] MatchedText,
    [Description("EDICT ID")] EdictId,
    [Description("Local Time")] LocalTime,
    Frequencies,
    [Description("Raw Frequencies")] RawFrequencies,

    // JMdict
    [Description("Primary Spelling with Orthography Info")] PrimarySpellingWithOrthographyInfo,
    [Description("Readings with Orthography Info")] ReadingsWithOrthographyInfo,
    [Description("Alternative Spellings with Orthography Info")] AlternativeSpellingsWithOrthographyInfo,

    // Word dictionaries
    [Description("Deconjugated Matched Text")] DeconjugatedMatchedText,
    [Description("Deconjugation Process")] DeconjugationProcess,

    //Kanji dictionaries
    [Description("Kun Readings")] KunReadings,
    [Description("On Readings")] OnReadings,
    [Description("Nanori Readings")] NanoriReadings,
    [Description("Stroke Count")] StrokeCount,
    [Description("Kanji Grade")] KanjiGrade,
    [Description("Kanji Composition")] KanjiComposition,
    [Description("Kanji Statistics")] KanjiStats
}
