using System.ComponentModel;

namespace JL.Core.Mining;

public enum JLField
{
    // Shared
    Nothing,
    [Description("Primary Spelling")] PrimarySpelling,
    Readings,
    [Description("First Reading")] FirstReading,
    Definitions,
    [Description("Primary Spelling and First Reading")] PrimarySpellingAndFirstReading,
    [Description("Primary Spelling and Readings")] PrimarySpellingAndReadings,
    [Description("Selected Definitions")] SelectedDefinitions,
    [Description("Dictionary Name")] DictionaryName,
    Audio,
    Image,
    [Description("Source Text")] SourceText,
    [Description("Leading Source Text Part")] LeadingSourceTextPart,
    [Description("Trailing Source Text Part")] TrailingSourceTextPart,
    Sentence,
    [Description("Leading Sentence Part")] LeadingSentencePart,
    [Description("Trailing Sentence Part")] TrailingSentencePart,
    [Description("Matched Text")] MatchedText,
    [Description("Local Time")] LocalTime,
    Frequencies,
    [Description("Raw Frequencies")] RawFrequencies,
    [Description("Preferred Frequency")] PreferredFrequency,
    [Description("Frequency (Harmonic Mean)")] FrequencyHarmonicMean,
    [Description("Pitch Accents")] PitchAccents,
    [Description("Pitch Accents (Numeric)")] NumericPitchAccents,
    [Description("Pitch Accent Categories")] PitchAccentCategories,
    [Description("Pitch Accent for First Reading")] PitchAccentForFirstReading,
    [Description("Pitch Accent for First Reading (Numeric)")] NumericPitchAccentForFirstReading,
    [Description("Pitch Accent Category for First Reading")] PitchAccentCategoryForFirstReading,

    // JMdict, JMnedict
    [Description("Entry ID")] EdictId,

    // Word dictionaries
    [Description("Deconjugated Matched Text")] DeconjugatedMatchedText,
    [Description("Deconjugation Process")] DeconjugationProcess,
    [Description("Word Classes")] WordClasses,
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
