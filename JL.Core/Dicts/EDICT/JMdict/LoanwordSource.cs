namespace JL.Core.Dicts.EDICT.JMdict;

internal readonly record struct LoanwordSource(string Language, bool IsPart, bool IsWasei, string? OriginalWord);
