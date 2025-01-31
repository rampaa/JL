namespace JL.Core.Dicts.JMdict;

internal readonly struct ReadingElement(string reb, List<string> reRestrList, List<string> reInfList) : IEquatable<ReadingElement>
{
    public string Reb { get; } = reb; // Reading in kana. e.g. むすめ
    public List<string> ReRestrList { get; } = reRestrList; // ReRestrList = Keb. The reading is only valid for this specific keb.
    public List<string> ReInfList { get; } = reInfList; // e.g. gikun
    // public bool ReNokanji { get; } // Is kana insufficient to notate the right spelling?
    // public List<string> RePriList { get; } // e.g. ichi1

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Reb.GetHashCode(StringComparison.Ordinal);
            foreach (string reRestr in ReRestrList)
            {
                hash = (hash * 37) + reRestr.GetHashCode(StringComparison.Ordinal);
            }

            foreach (string reInf in ReInfList)
            {
                hash = (hash * 37) + reInf.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ReadingElement other
            && Reb == other.Reb
            && ReRestrList.SequenceEqual(other.ReRestrList)
            && ReInfList.SequenceEqual(other.ReInfList);
    }

    public bool Equals(ReadingElement other)
    {
        return Reb == other.Reb
            && ReRestrList.SequenceEqual(other.ReRestrList)
            && ReInfList.SequenceEqual(other.ReInfList);
    }
}
