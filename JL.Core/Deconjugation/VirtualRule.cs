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
        return true;
    }

    public override int GetHashCode()
    {
        return 0;
    }


    public override bool Equals(object? obj)
    {
        return obj is VirtualRule other && Equals(other);
    }

    public static bool operator ==(VirtualRule left, VirtualRule right) => left.Equals(right);

    public static bool operator !=(VirtualRule left, VirtualRule right) => !(left == right);
}
