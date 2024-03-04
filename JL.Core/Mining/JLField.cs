using System.ComponentModel;

namespace JL.Core.Mining;

public enum JLField
{
    // Shared
    Nothing,
    [Description("Primary Spelling")] PrimarySpelling,
    Readings,
    Definitions,
    [Description("Selected Definitions")] SelectedDefinitions,
    [Description("Dictionary Name")] DictionaryName,
    Audio,
    Image,
    Sentence,
    [Description("Leading Sentence Part")] LeadingSentencePart,
    [Description("Trailing Sentence Part")] TrailingSentencePart,
    [Description("Source Text")] SourceText,
    [Description("Matched Text")] MatchedText,
    [Description("Local Time")] LocalTime,
    Frequencies,
    [Description("Raw Frequencies")] RawFrequencies,
    [Description("Pitch Accents")] PitchAccents,
    [Description("Pitch Accents (Numeric)")] NumericPitchAccents,

    // JMdict, JMnedict, KANJIDIC2
    [Description("Entry ID")] EdictId,

    // Word dictionaries
    [Description("Deconjugated Matched Text")] DeconjugatedMatchedText,
    [Description("Deconjugation Process")] DeconjugationProcess,
    // JMdict, Nazeka EPWING
    [Description("Alternative Spellings")] AlternativeSpellings,
    // JMdict
    [Description("Primary Spelling with Orthography Info")] PrimarySpellingWithOrthographyInfo,
    [Description("Readings with Orthography Info")] ReadingsWithOrthographyInfo,
    [Description("Alternative Spellings with Orthography Info")] AlternativeSpellingsWithOrthographyInfo,

    // Kanji dictionaries
    [Description("On Readings")] OnReadings,
    [Description("Kun Readings")] KunReadings,
    [Description("Kanji Composition")] KanjiComposition,
    [Description("Kanji Statistics")] KanjiStats,
    // KANJIDIC2
    [Description("Nanori Readings")] NanoriReadings,
    [Description("Stroke Count")] StrokeCount,
    [Description("Kanji Grade")] KanjiGrade,
    [Description("Radical Names")] RadicalNames
}
