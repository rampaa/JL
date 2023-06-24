namespace JL.Core.Freqs;

public readonly record struct FrequencyRecord
{
    internal string Spelling { get; }
    internal int Frequency { get; }

    internal FrequencyRecord(string spelling, int frequency)
    {
        Spelling = spelling;
        Frequency = frequency;
    }
}
