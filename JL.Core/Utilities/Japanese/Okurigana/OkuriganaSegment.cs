namespace JL.Core.Utilities.Japanese.Okurigana;

internal readonly record struct OkuriganaSegment(int ExpressionStart, int ExpressionLength, int ReadingStart, int ReadingLength, bool IsKanji)
{
    public int ExpressionStart { get; } = ExpressionStart;
    public int ExpressionLength { get; } = ExpressionLength;
    public int ReadingStart { get; } = ReadingStart;
    public int ReadingLength { get; } = ReadingLength;
    public bool IsKanji { get; } = IsKanji;
}
