namespace JL.Core.Freqs;

public class FrequencyRecord
{
    public string Spelling { get; }
    public int Frequency { get; }

    public FrequencyRecord(string spelling, int frequency)
    {
        Spelling = spelling;
        Frequency = frequency;
    }
}
