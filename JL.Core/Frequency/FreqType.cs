using System.ComponentModel;

namespace JL.Core.Frequency;

public enum FreqType
{
    Nazeka,
    Yomichan,
    [Description("Yomichan (Kanji)")]
    YomichanKanji
}
