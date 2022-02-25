namespace JL.Lookup
{
    public enum LookupResult
    {
        // common (required for sorting)
        FoundForm,
        Frequency,
        DictType,
        FoundSpelling,

        // KanaSpellings,
        Readings,
        Definitions,
        EdictID,
        AlternativeSpellings,
        Process,
        POrthographyInfoList,
        ROrthographyInfoList,
        AOrthographyInfoList,

        // KANJIDIC
        OnReadings,
        KunReadings,
        Nanori,
        StrokeCount,
        Grade,
        Composition
    }
}
