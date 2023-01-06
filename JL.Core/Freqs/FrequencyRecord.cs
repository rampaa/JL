namespace JL.Core.Freqs;

public sealed class FrequencyRecord
{
    internal string Spelling { get; }
    internal int Frequency { get; }

    internal FrequencyRecord(string spelling, int frequency)
    {
        Spelling = spelling;
        Frequency = frequency;
    }
}
