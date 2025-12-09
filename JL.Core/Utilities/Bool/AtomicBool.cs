namespace JL.Core.Utilities.Bool;

public sealed class AtomicBool
{
    private const int False = 0;
    private const int True = 1;

    private int _value;

    public AtomicBool(bool initialValue)
    {
        _value = initialValue ? True : False;
    }

    public bool Read()
    {
        return Volatile.Read(ref _value) is not False;
    }

    public void SetTrue()
    {
        Volatile.Write(ref _value, True);
    }

    public void SetFalse()
    {
        Volatile.Write(ref _value, False);
    }

    public bool TrySetTrue() => Interlocked.CompareExchange(ref _value, True, False) is False;

    // ReSharper disable once UnusedMember.Global
    public bool TrySetFalse() => Interlocked.CompareExchange(ref _value, False, True) is True;
}
