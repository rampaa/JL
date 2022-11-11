using System.ComponentModel;

namespace JL.Core.Freqs;

public enum FreqType
{
    Nazeka,
    Yomichan,
    [Description("Yomichan (Kanji)")]
    YomichanKanji
}
