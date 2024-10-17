namespace JL.Core.Dicts.JMdict;

internal sealed class ReadingElement(string reb, List<string> reRestrList, List<string> reInfList)
{
    public string Reb { get; } = reb; // Reading in kana. e.g. むすめ
    public List<string> ReRestrList { get; } = reRestrList; // ReRestrList = Keb. The reading is only valid for this specific keb.
    public List<string> ReInfList { get; } = reInfList; // e.g. gikun
    // public bool ReNokanji { get; } // Is kana insufficient to notate the right spelling?
    // public List<string> RePriList { get; } // e.g. ichi1
}
