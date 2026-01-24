using System.ComponentModel;

namespace JL.Core.Mining;

public enum JLField
{
    // Shared
    Nothing,
    [Description("Selected Spelling")] SelectedSpelling,
    [Description("Primary Spelling")] PrimarySpelling,
    Readings,
    [Description("Selected Reading")] FirstReading,
    Definitions,
    [Description("Definitions from Multiple Dictionaries")] DefinitionsFromMultipleDictionaries,
    [Description("Primary Spelling and Selected Reading")] PrimarySpellingAndFirstReading,
    [Description("Primary Spelling and Readings")] PrimarySpellingAndReadings,
    [Description("Selected Definitions")] SelectedDefinitions,
    [Description("Dictionary Name")] DictionaryName,
    Audio,
    [Description("Sentence Audio")] SentenceAudio,
    [Description("Source Text Audio")] SourceTextAudio,
    [Description("Monitor Screenshot")] MonitorScreenshot,
    [Description("Image (Clipboard)")] Image,
    [Description("Images (Definitions)")] DefinitionsImages,
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
    [Description("Pitch Accent for Selected Reading")] PitchAccentForFirstReading,
    [Description("Pitch Accent for Selected Reading (Numeric)")] NumericPitchAccentForFirstReading,
    [Description("Pitch Accent Category for Selected Reading")] PitchAccentCategoryForFirstReading,

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
