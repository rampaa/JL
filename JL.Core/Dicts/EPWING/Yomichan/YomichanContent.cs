namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly record struct YomichanContent<TTag>(TTag Tag, string? Content, bool AppendWhitespace, string? Marker);
