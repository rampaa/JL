namespace JL.Core.Frequency;

public class FrequencyEntry
{
    public string Spelling { get; init; }
    public int Frequency { get; init; }

    public FrequencyEntry(string spelling, int frequency)
    {
        Spelling = spelling;
        Frequency = frequency;
    }
}
