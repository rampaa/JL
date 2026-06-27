namespace JL.Core.Utilities.Japanese.Mazegaki;

internal readonly record struct MazegakiVariantEnumerable
{
    private readonly string _expression;
    private readonly string _reading;

    public MazegakiVariantEnumerable(string expression, string reading)
    {
        _expression = expression;
        _reading = reading;
    }

    public MazegakiVariantEnumerator GetEnumerator() => new(_expression, _reading);
}
