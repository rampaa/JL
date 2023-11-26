namespace JL.Core.Freqs.Options;
public sealed class FreqOptions
{
    public UseDBOption? UseDB { get; }

    public FreqOptions(UseDBOption? useDB = null)
    {
        UseDB = useDB;
    }
}
