using System.Text.Json.Serialization;

namespace JL.Core.Freqs;

[method: JsonConstructor]
public readonly struct FrequencyRecord(string spelling, int frequency) : IEquatable<FrequencyRecord>
{
    internal string Spelling { get; } = spelling;
    internal int Frequency { get; } = frequency;

    public override int GetHashCode()
    {
        return Spelling.GetHashCode(StringComparison.InvariantCulture);
    }

    public override bool Equals(object? obj)
    {
        return obj is FrequencyRecord frequencyRecord && Equals(frequencyRecord);
    }

    public bool Equals(FrequencyRecord other)
    {
        return Spelling == other.Spelling;
    }

    public static bool operator ==(FrequencyRecord left, FrequencyRecord right) => left.Equals(right);
    public static bool operator !=(FrequencyRecord left, FrequencyRecord right) => !left.Equals(right);
}
