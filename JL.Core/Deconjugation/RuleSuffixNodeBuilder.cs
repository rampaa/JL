namespace JL.Core.Deconjugation;

internal sealed class RuleSuffixNodeBuilder
{
    internal Dictionary<char, int> NextNodeIndicesByCharacter { get; } = [];
    internal List<int> RuleIndices { get; } = [];
}
