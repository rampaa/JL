using System.Text.Json.Serialization;

namespace JL.Core.WordClass;

internal sealed class JmdictWordClass(string spelling, string[] wordClasses, string[]? readings = null) : IEquatable<JmdictWordClass>
{
    [JsonPropertyName("S")] public string Spelling { get; set; } = spelling;
    [JsonPropertyName("C")] public string[] WordClasses { get; } = wordClasses;
    [JsonPropertyName("R")] public string[]? Readings { get; } = readings;

    public override bool Equals(object? obj)
    {
        return obj is JmdictWordClass jmdictWordClass
               && Spelling == jmdictWordClass.Spelling
               && ((Readings is null && jmdictWordClass.Readings is null)
                    || (Readings is not null && jmdictWordClass.Readings is not null && Readings.SequenceEqual(jmdictWordClass.Readings)))
               && WordClasses.SequenceEqual(jmdictWordClass.WordClasses);
    }

    public bool Equals(JmdictWordClass? other)
    {
        return other is not null
               && Spelling == other.Spelling
               && ((Readings is null && other.Readings is null)
                    || (Readings is not null && other.Readings is not null && Readings.SequenceEqual(other.Readings)))
               && WordClasses.SequenceEqual(other.WordClasses);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Spelling.GetHashCode(StringComparison.Ordinal);
            if (Readings is not null)
            {
                foreach (string reading in Readings)
                {
                    hash = (hash * 37) + reading.GetHashCode(StringComparison.Ordinal);
                }
            }

            else
            {
                hash *= 37;
            }

            foreach (string wordClass in WordClasses)
            {
                hash = (hash * 37) + wordClass.GetHashCode(StringComparison.Ordinal);
            }

            return hash;
        }
    }
}
