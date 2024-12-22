namespace JL.Core.Dicts.JMdict;

internal readonly record struct LoanwordSource(string Language, bool IsPart, bool IsWasei, string? OriginalWord = null);
