using System.ComponentModel;

namespace JL.Core.Anki;

public enum JLField
{
    // Shared
    Nothing,
    [Description("Primary Spelling")] PrimarySpelling,
    Readings,
    Definitions,
    [Description("Dictionary Name")] DictionaryName,
    Audio,
    Image,
    Sentence,
    [Description("Source Text")] SourceText,
    [Description("Matched Text")] MatchedText,
    [Description("Local Time")] LocalTime,
    Frequencies,
    [Description("Raw Frequencies")] RawFrequencies,

    // JMdict, JMnedict, KANJIDIC2
    [Description("EDICT ID")] EdictId,

    // Word dictionaries
    [Description("Deconjugated Matched Text")] DeconjugatedMatchedText,
    [Description("Deconjugation Process")] DeconjugationProcess,
    // JMdict, Nazeka EPWING
    [Description("Alternative Spellings")] AlternativeSpellings,
    // JMdict
    [Description("Primary Spelling with Orthography Info")] PrimarySpellingWithOrthographyInfo,
    [Description("Readings with Orthography Info")] ReadingsWithOrthographyInfo,
    [Description("Alternative Spellings with Orthography Info")] AlternativeSpellingsWithOrthographyInfo,

    //Kanji dictionaries
    [Description("Kun Readings")] KunReadings,
    [Description("On Readings")] OnReadings,
    [Description("Kanji Composition")] KanjiComposition,
    [Description("Kanji Statistics")] KanjiStats,
    //KANJIDIC2
    [Description("Nanori Readings")] NanoriReadings,
    [Description("Stroke Count")] StrokeCount,
    [Description("Kanji Grade")] KanjiGrade,
    [Description("Radical Names")] RadicalNames
}
