using System.Text;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Deconjugation;

internal sealed class ProcessNode(string detail, ProcessNode? parent) : IEquatable<ProcessNode>
{
    public ProcessNode? Parent { get; } = parent;
    public string Detail { get; } = detail;
    public int ProperStepCount { get; } = parent is null ? 1 : parent.ProperStepCount + (detail.Length > 0 && detail[0] is not '(' ? 1 : 0);

    private string? _cachedText;

    private string? _cachedDeconjugationProcessText;

    public string? GetFormattedText()
    {
        if (_cachedText is not null)
        {
            return _cachedText;
        }

        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        AppendFormattedText(sb);
        _cachedText = sb.Length > 0 ? sb.ToString() : null;
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return _cachedText;
    }

    public string? GetCachedDeconjugationProcessText()
    {
        if (_cachedDeconjugationProcessText is not null)
        {
            return _cachedDeconjugationProcessText;
        }

        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        AppendFormattedText(sb.Append('～'));
        _cachedDeconjugationProcessText = sb.Length > 1 ? sb.ToString() : null;
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return _cachedDeconjugationProcessText;
    }

    private void AppendFormattedText(StringBuilder sb)
    {
        ProcessNode? currentNode = this;
        bool added = false;
        while (currentNode is not null)
        {
            string info = currentNode.Detail;
            if (info.Length is not 0)
            {
                if (info[0] is '(')
                {
                    if (currentNode.Parent is null)
                    {
                        if (added)
                        {
                            _ = sb.Append('→');
                        }

                        _ = sb.Append(info.AsSpan(1, info.Length - 2));
                        added = true;
                    }
                }
                else
                {
                    if (added)
                    {
                        _ = sb.Append('→');
                    }

                    _ = sb.Append(info);
                    added = true;
                }
            }

            currentNode = currentNode.Parent;
        }
    }

    public bool Equals(ProcessNode? other)
    {
        return other is not null
            && (ReferenceEquals(this, other)
                || (ProperStepCount == other.ProperStepCount
                    && Detail == other.Detail
                    && ReferenceEquals(Parent, other.Parent)));
    }

    public override bool Equals(object? obj)
    {
        return obj is ProcessNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 37) + ProperStepCount.GetHashCode();
            hash = (hash * 37) + Detail.GetHashCode(StringComparison.Ordinal);
            return hash;
        }
    }

    public static bool operator ==(ProcessNode? left, ProcessNode? right) => left?.Equals(right) ?? (right is null);

    public static bool operator !=(ProcessNode? left, ProcessNode? right) => !(left == right);
}
