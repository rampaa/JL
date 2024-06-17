namespace JL.Core.Dicts.JMdict;

internal sealed class ReadingElement
{
    public string Reb { get; set; } = ""; // Reading in kana. e.g. むすめ
    public List<string> ReRestrList { get; } = []; // ReRestrList = Keb. The reading is only valid for this specific keb.
    public List<string> ReInfList { get; } = []; // e.g. gikun
    // public bool ReNokanji { get; set; } // = False; // Is kana insufficient to notate the right spelling?
    // public List<string> RePriList { get; } = [] // e.g. ichi1
}
