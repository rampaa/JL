using System.ComponentModel;

namespace JL.Anki
{
    public enum JLField
    {
        Nothing,
        [Description("Found Spelling")] FoundSpelling,
        Readings,
        Definitions,
        [Description("Found Form")] FoundForm,
        Context,
        Audio,
        [Description("Edict ID")] EdictID,
        [Description("Time (Local)")] TimeLocal,
        [Description("Alternative Spellings")] AlternativeSpellings,
        Frequency,
        [Description("Stroke Count")] StrokeCount,
        Grade,
        Composition,
        [Description("Dict Type")] DictType,
        Process,
        // Source
    }
}
