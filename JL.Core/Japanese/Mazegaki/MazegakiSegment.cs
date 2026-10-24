namespace JL.Core.Japanese.Mazegaki;

internal readonly record struct MazegakiSegment(int ExpressionStart, int ExpressionLength, int ReadingStart, int ReadingLength, bool IsKanji);
