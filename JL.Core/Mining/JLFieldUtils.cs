namespace JL.Core.Mining;

public static class JLFieldUtils
{
    public static readonly JLField[] JLFieldsForWordDicts = {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.PrimarySpellingWithOrthographyInfo,
        JLField.AlternativeSpellings,
        JLField.AlternativeSpellingsWithOrthographyInfo,
        JLField.Readings,
        JLField.ReadingsWithOrthographyInfo,
        JLField.Definitions,
        JLField.SelectedDefinitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.MatchedText,
        JLField.DeconjugatedMatchedText,
        JLField.DeconjugationProcess,
        JLField.Frequencies,
        JLField.RawFrequencies,
        JLField.EdictId,
        JLField.LocalTime
    };

    public static readonly JLField[] JLFieldsForKanjiDicts = {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.KunReadings,
        JLField.OnReadings,
        JLField.NanoriReadings,
        JLField.RadicalNames,
        JLField.StrokeCount,
        JLField.KanjiGrade,
        JLField.KanjiComposition,
        JLField.Definitions,
        JLField.SelectedDefinitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.Frequencies,
        JLField.RawFrequencies,
        JLField.EdictId,
        JLField.LocalTime
    };

    public static readonly JLField[] JLFieldsForNameDicts = {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.AlternativeSpellings,
        JLField.Definitions,
        JLField.SelectedDefinitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.EdictId,
        JLField.LocalTime
    };
}
