namespace JL.Core.Anki;

public static class JLFieldUtils
{
    public static readonly JLField[] JLFieldsForWordDicts = new[]
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.AlternativeSpellings,
        JLField.Readings,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.MatchedText,
        JLField.DeconjugatedMatchedText,
        JLField.DeconjugationProcess,
        JLField.Frequencies,
        JLField.EdictId,
        JLField.LocalTime
    };

    public static readonly JLField[] JLFieldsForKanjiDicts = new[]
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.KunReadings,
        JLField.OnReadings,
        JLField.NanoriReadings,
        JLField.StrokeCount,
        JLField.KanjiGrade,
        JLField.KanjiComposition,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.Frequencies,
        JLField.EdictId,
        JLField.LocalTime
    };

    public static readonly JLField[] JLFieldsForNameDicts = new[]
    {
        JLField.Nothing,
        JLField.PrimarySpelling,
        JLField.Readings,
        JLField.AlternativeSpellings,
        JLField.Definitions,
        JLField.DictionaryName,
        JLField.Audio,
        JLField.Image,
        JLField.SourceText,
        JLField.Sentence,
        JLField.EdictId,
        JLField.LocalTime
    };
}
