using System.Text.Json.Serialization;

namespace JL.Core.Dicts.JMdict;

[method: JsonConstructor]
public readonly record struct LoanwordSource(string Language, bool IsPart, bool IsWasei, string? OriginalWord = null);
