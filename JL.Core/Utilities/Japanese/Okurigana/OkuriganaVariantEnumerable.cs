namespace JL.Core.Utilities.Japanese.Okurigana;

internal readonly record struct OkuriganaVariantEnumerable
{
    private readonly string _expression;
    private readonly string _reading;

    public OkuriganaVariantEnumerable(string expression, string reading)
    {
        _expression = expression;
        _reading = reading;
    }

    public OkuriganaVariantEnumerator GetEnumerator() => new(_expression, _reading);
}
