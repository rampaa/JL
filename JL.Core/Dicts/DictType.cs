using System.ComponentModel;

namespace JL.Core.Dicts;

public enum DictType
{
    // built-in
    JMdict,
    JMnedict,
    Kanjidic,
    [Description("Custom Word Dictionary")]
    CustomWordDictionary,
    [Description("Custom Name Dictionary")]
    CustomNameDictionary,

    // Yomichan Import
    [Description("Kenkyuusha (Yomichan)")]
    Kenkyuusha,
    [Description("Daijirin (Yomichan)")]
    Daijirin,
    [Description("Daijisen (Yomichan)")]
    Daijisen,
    [Description("Koujien (Yomichan)")]
    Koujien,
    [Description("Meikyou (Yomichan)")]
    Meikyou,
    [Description("Gakken (Yomichan)")]
    Gakken,
    [Description("Kotowaza (Yomichan)")]
    Kotowaza,
    [Description("Pitch Accent (Yomichan)")]
    PitchAccentYomichan,
    [Description("Iwanami (Yomichan)")]
    IwanamiYomichan,
    [Description("Jitsuyou (Yomichan)")]
    JitsuyouYomichan,
    [Description("Shinmeikai (Yomichan)")]
    ShinmeikaiYomichan,
    [Description("Nikkoku (Yomichan)")]
    NikkokuYomichan,
    [Description("Shinjirin (Yomichan)")]
    ShinjirinYomichan,
    [Description("Oubunsha (Yomichan)")]
    OubunshaYomichan,
    [Description("Zokugo (Yomichan)")]
    ZokugoYomichan,
    [Description("Weblio Kogo (Yomichan)")]
    WeblioKogoYomichan,
    [Description("Gakken Yojijukugo (Yomichan)")]
    GakkenYojijukugoYomichan,
    [Description("Shinmeikai Yojijukugo (Yomichan)")]
    ShinmeikaiYojijukugoYomichan,
    [Description("Kirei Cake (Yomichan)")]
    KireiCakeYomichan,
    [Description("Kanjigen (Yomichan)")]
    KanjigenYomichan,
    [Description("Nonspecific Word Dictionary (Yomichan)")]
    NonspecificWordYomichan,
    [Description("Nonspecific Kanji Dictionary (Yomichan)")]
    NonspecificKanjiYomichan,
    [Description("Nonspecific Name Dictionary (Yomichan)")]
    NonspecificNameYomichan,
    [Description("Nonspecific (Yomichan)")]
    NonspecificYomichan,

    // Nazeka Epwing Converter
    [Description("Kenkyuusha (Nazeka)")]
    KenkyuushaNazeka,
    [Description("Daijirin (Nazeka)")]
    DaijirinNazeka,
    [Description("Shinmeikai (Nazeka)")]
    ShinmeikaiNazeka,
    [Description("Nonspecific Word Dictionary (Nazeka)")]
    NonspecificWordNazeka,
    [Description("Nonspecific Kanji Dictionary (Nazeka)")]
    NonspecificKanjiNazeka,
    [Description("Nonspecific Name Dictionary (Nazeka)")]
    NonspecificNameNazeka,
    [Description("Nonspecific (Nazeka)")]
    NonspecificNazeka,
}
