namespace JL.Core.Frequency;

public class FrequencyRecord
{
    public string Spelling { get; init; }
    public int Frequency { get; init; }

    public FrequencyRecord(string spelling, int frequency)
    {
        Spelling = spelling;
        Frequency = frequency;
    }
}
