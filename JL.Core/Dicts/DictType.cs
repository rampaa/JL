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

    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Kenkyuusha (Yomichan)")] Kenkyuusha,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Daijisen (Yomichan)")] Daijisen,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Daijirin (Yomichan)")] Daijirin,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Koujien (Yomichan)")] Koujien,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Meikyou (Yomichan)")] Meikyou,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Gakken (Yomichan)")] Gakken,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Kotowaza (Yomichan)")] Kotowaza,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Iwanami (Yomichan)")] IwanamiYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Jitsuyou (Yomichan)")] JitsuyouYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Shinmeikai (Yomichan)")] ShinmeikaiYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Nikkoku (Yomichan)")] NikkokuYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Shinjirin (Yomichan)")] ShinjirinYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Oubunsha (Yomichan)")] OubunshaYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Zokugo (Yomichan)")] ZokugoYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Weblio Kogo (Yomichan)")] WeblioKogoYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Gakken Yojijukugo (Yomichan)")] GakkenYojijukugoYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Shinmeikai Yojijukugo (Yomichan)")] ShinmeikaiYojijukugoYomichan,
    [Obsolete("Will be replaced by NonspecificWordYomichan")][Description("Kirei Cake (Yomichan)")] KireiCakeYomichan,
    [Obsolete("Will be replaced by NonspecificKanjiWithWordSchemaYomichan")][Description("Kanjigen (Yomichan)")] KanjigenYomichan,

    // Nazeka Epwing Converter
    [Description("Word Dictionary (Nazeka)")] NonspecificWordNazeka,
    [Description("Kanji Dictionary (Nazeka)")] NonspecificKanjiNazeka,
    [Description("Name Dictionary (Nazeka)")] NonspecificNameNazeka,
    [Description("Nonspecific Dictionary (Nazeka)")] NonspecificNazeka,

    [Obsolete("Will be replaced by NonspecificWordNazeka")][Description("Kenkyuusha (Nazeka)")] KenkyuushaNazeka,
    [Obsolete("Will be replaced by NonspecificWordNazeka")][Description("Daijirin (Nazeka)")] DaijirinNazeka,
    [Obsolete("Will be replaced by NonspecificWordNazeka")][Description("Shinmeikai (Nazeka)")] ShinmeikaiNazeka
}
