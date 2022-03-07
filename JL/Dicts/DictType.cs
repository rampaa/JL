using System.ComponentModel;

namespace JL.Dicts
{
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

        // user-added
        Kenkyuusha,
        Daijirin,
        Daijisen,
        Koujien,
        Meikyou,
        Gakken,
        Kotowaza,
    }
}
