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
    [Description("Nonspecific (Yomichan)")]
    NonspecificYomichan,
    [Obsolete("Superseded by PitchAccentYomichan. Will be deleted later on.")]
    Kanjium,

    // Nazeka Epwing Converter
    [Description("Kenkyuusha (Nazeka)")]
    KenkyuushaNazeka,
    [Description("Daijirin (Nazeka)")]
    DaijirinNazeka,
    [Description("Shinmeikai (Nazeka)")]
    ShinmeikaiNazeka,
    [Description("Nonspecific (Nazeka)")]
    NonspecificNazeka,
}
