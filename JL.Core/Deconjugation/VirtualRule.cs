namespace JL.Core.Deconjugation;

internal readonly struct VirtualRule(RuleType type, string decEnd, string conEnd, string decTag, string conTag, string detail) : IEquatable<VirtualRule>
{
    public RuleType Type { get; } = type;
    public string DecEnd { get; } = decEnd;
    public string ConEnd { get; } = conEnd;
    public string DecTag { get; } = decTag;
    public string ConTag { get; } = conTag;
    public string Detail { get; } = detail;

    public bool Equals(VirtualRule other)
    {
        return Type == other.Type
            && DecEnd == other.DecEnd
            && ConEnd == other.ConEnd
            && DecTag == other.DecTag
            && ConTag == other.ConTag
            && Detail == other.Detail;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + Type.GetHashCode();
            hash = (hash * 37) + Detail.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + DecEnd.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + ConEnd.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + DecTag.GetHashCode(StringComparison.Ordinal);
            hash = (hash * 37) + ConTag.GetHashCode(StringComparison.Ordinal);
            return hash;
        }
    }


    public override bool Equals(object? obj)
    {
        return obj is VirtualRule other && Equals(other);
    }

    public static bool operator ==(VirtualRule left, VirtualRule right) => left.Equals(right);

    public static bool operator !=(VirtualRule left, VirtualRule right) => !(left == right);
}
