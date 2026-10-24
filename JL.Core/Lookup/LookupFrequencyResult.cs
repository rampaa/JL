using System.Text.Json.Serialization;

namespace JL.Core.Lookup;

[method: JsonConstructor]
public readonly record struct LookupFrequencyResult(string Name, int Freq, bool HigherValueMeansHigherFrequency);
