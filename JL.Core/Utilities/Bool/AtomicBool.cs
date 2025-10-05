namespace JL.Core.Utilities.Bool;

internal sealed class AtomicBool : IEquatable<AtomicBool>, IEquatable<bool>
{
    private int _value;

    private bool Value
    {
        get => Volatile.Read(ref _value) is not 0;
        set => Volatile.Write(ref _value, value ? 1 : 0);
    }

    public AtomicBool(bool value)
    {
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            AtomicBool atomicBool => Value == atomicBool.Value,
            bool booleanValue => Value == booleanValue,
            _ => false
        };
    }

    public bool Equals(AtomicBool? other) => other is not null && other.Value == Value;

    public bool Equals(bool other) => Value == other;

    public static bool operator ==(AtomicBool? left, AtomicBool? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(AtomicBool? left, AtomicBool? right) => !(left == right);

    public static bool operator ==(AtomicBool? left, bool right) => left is not null && left.Value == right;
    public static bool operator !=(AtomicBool? left, bool right) => left is null || left.Value != right;

    public override int GetHashCode() => Value.GetHashCode();


    public static implicit operator bool(AtomicBool tsb) => tsb.Value;
    public bool ToBoolean() => Value;

    public void SetTrue()
    {
        Value = true;
    }

    public void SetFalse()
    {
        Value = false;
    }

    public bool TrySetTrue() => Interlocked.CompareExchange(ref _value, 1, 0) is 0;
    public bool TrySetFalse() => Interlocked.CompareExchange(ref _value, 0, 1) is 1;
}
