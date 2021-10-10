namespace JapaneseLookup
{
    public enum LookupResult
    {
        // common (required for sorting)
        FoundForm,
        Frequency,
        DictType,

        // EDICT (and EPWING?)
        FoundSpelling,
        KanaSpellings,
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