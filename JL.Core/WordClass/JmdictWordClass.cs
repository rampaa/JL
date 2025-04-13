using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.WordClass;

internal sealed class JmdictWordClass(string spelling, string[] wordClasses, string[]? readings = null) : IEquatable<JmdictWordClass>
{
    [JsonPropertyName("S")] public string Spelling { get; } = spelling.GetPooledString();
    [JsonPropertyName("C")] public string[] WordClasses { get; } = wordClasses;
    [JsonPropertyName("R")] public string[]? Readings { get; } = readings;

    public override bool Equals(object? obj)
    {
        return obj is JmdictWordClass jmdictWordClass
               && Spelling == jmdictWordClass.Spelling
               && ((Readings is null && jmdictWordClass.Readings is null)
                    || (Readings is not null && jmdictWordClass.Readings is not null && Readings.AsSpan().SequenceEqual(jmdictWordClass.Readings)))
               && WordClasses.AsSpan().SequenceEqual(jmdictWordClass.WordClasses);
    }

    public bool Equals(JmdictWordClass? other)
    {
        return other is not null
               && Spelling == other.Spelling
               && ((Readings is null && other.Readings is null)
                    || (Readings is not null && other.Readings is not null && Readings.AsSpan().SequenceEqual(other.Readings)))
               && WordClasses.AsSpan().SequenceEqual(other.WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Spelling.GetHashCode(StringComparison.Ordinal);
            string[]? readings = Readings;
            if (readings is not null)
            {
                foreach (string reading in readings)
                {
                    hash = (hash * 37) + reading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            string[] wordClasses = WordClasses;
            foreach (string wordClass in wordClasses)
            {
                hash = (hash * 37) + wordClass.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }

    public static bool operator ==(JmdictWordClass? left, JmdictWordClass? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(JmdictWordClass? left, JmdictWordClass? right) => !left?.Equals(right) ?? right is not null;
}
