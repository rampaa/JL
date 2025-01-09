namespace JL.Core.Freqs;

public readonly struct FrequencyRecord(string spelling, int frequency) : IEquatable<FrequencyRecord>
{
    internal string Spelling { get; } = spelling;
    internal int Frequency { get; } = frequency;

    public override bool Equals(object? obj)
    {
        return obj is FrequencyRecord record && Spelling == record.Spelling;
    }

    public override int GetHashCode()
    {
        return Spelling.GetHashCode(StringComparison.InvariantCulture);
    }

    public bool Equals(FrequencyRecord other)
    {
        return Spelling == other.Spelling;
    }

    public static bool operator ==(FrequencyRecord left, FrequencyRecord right) => left.Equals(right);
    public static bool operator !=(FrequencyRecord left, FrequencyRecord right) => !left.Equals(right);
}
