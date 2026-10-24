namespace JL.Core.Deconjugation;

internal readonly record struct VirtualRule(RuleType Type, string DecEnd, string ConEnd, string DecTag, string ConTag, string Detail);
