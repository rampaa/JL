using System.ComponentModel;

namespace JL.Core.Dicts;

public enum DictType
{
    // built-in
    JMdict,
    JMnedict,
    Kanjidic,
    [Description("Custom Word Dictionary")] CustomWordDictionary,
    [Description("Custom Name Dictionary")] CustomNameDictionary,
    [Description("Custom Word Dictionary (Profile)")] ProfileCustomWordDictionary,
    [Description("Custom Name Dictionary (Profile)")] ProfileCustomNameDictionary,

    // Yomichan Import
    [Description("Word Dictionary (Yomichan)")] NonspecificWordYomichan,
    [Description("Kanji Dictionary (Yomichan)")] NonspecificKanjiYomichan,
    [Description("Kanji Dictionary with Word Schema (Yomichan)")] NonspecificKanjiWithWordSchemaYomichan,
    [Description("Name Dictionary (Yomichan)")] NonspecificNameYomichan,
    [Description("Pitch Accent Dictionary (Yomichan)")] PitchAccentYomichan,
    [Description("Nonspecific Dictionary (Yomichan)")] NonspecificYomichan,

    // Nazeka Epwing Converter
    [Description("Word Dictionary (Nazeka)")] NonspecificWordNazeka,
    [Description("Kanji Dictionary (Nazeka)")] NonspecificKanjiNazeka,
    [Description("Name Dictionary (Nazeka)")] NonspecificNameNazeka,
    [Description("Nonspecific Dictionary (Nazeka)")] NonspecificNazeka
}
