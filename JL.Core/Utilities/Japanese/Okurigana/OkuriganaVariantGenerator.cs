using System.Runtime.CompilerServices;
using JL.Core.Utilities;

namespace JL.Core.Utilities.Japanese.Okurigana;

internal static class OkuriganaVariantGenerator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBitSet(ulong mask, int index)
    {
        return (mask & (1UL << index)) is not 0;
    }

    public static OkuriganaVariantEnumerable GenerateMixedVariants(string expression, string reading)
    {
        return new OkuriganaVariantEnumerable(expression, reading);
    }

    public static bool TryGetUniqueSegmentation(ReadOnlySpan<char> expression, ReadOnlySpan<char> reading, Span<OkuriganaSegment> segments, out int count, out int kanjiCount)
    {
        count = 0;
        kanjiCount = 0;

        int expressionIndex = 0;
        int readingIndex = 0;

        while (expressionIndex < expression.Length)
        {
            int startIndex = expressionIndex;
            if (!IsKanji(expression, expressionIndex, out int consumedCharCount))
            {
                do
                {
                    expressionIndex += consumedCharCount;
                } while (expressionIndex < expression.Length && !IsKanji(expression, expressionIndex, out consumedCharCount));

                int length = expressionIndex - startIndex;
                if (readingIndex + length > reading.Length
                    || !reading.Slice(readingIndex, length).SequenceEqual(expression.Slice(startIndex, length)))
                {
                    return false;
                }

                segments[count] = new OkuriganaSegment(startIndex, length, readingIndex, length, false);
                ++count;
                readingIndex += length;
            }
            else
            {
                do
                {
                    expressionIndex += consumedCharCount;
                } while (expressionIndex < expression.Length && IsKanji(expression, expressionIndex, out consumedCharCount));

                int expressionLength = expressionIndex - startIndex;
                int readingStartIndex = readingIndex;

                if (expression.Length > expressionIndex)
                {
                    int anchorStartIndex = expressionIndex;
                    while (expressionIndex < expression.Length && !IsKanji(expression, expressionIndex, out consumedCharCount))
                    {
                        expressionIndex += consumedCharCount;
                    }

                    ReadOnlySpan<char> anchor = expression[anchorStartIndex..expressionIndex];
                    int searchStartIndex = readingStartIndex + 1;

                    if (searchStartIndex >= reading.Length)
                    {
                        return false;
                    }

                    // Efficient uniqueness check (Single scan with forward slice)
                    ReadOnlySpan<char> readingSlice = reading[searchStartIndex..];
                    int firstMatch = readingSlice.IndexOf(anchor);

                    if (firstMatch is -1 || readingSlice[(firstMatch + 1)..].IndexOf(anchor) is not -1)
                    {
                        return false;
                    }

                    readingIndex = searchStartIndex + firstMatch;
                    expressionIndex = anchorStartIndex;
                }
                else
                {
                    readingIndex = reading.Length;
                }

                int readingLength = readingIndex - readingStartIndex;
                if (readingLength <= 0)
                {
                    return false;
                }

                segments[count] = new OkuriganaSegment(startIndex, expressionLength, readingStartIndex, readingLength, true);
                ++count;
                ++kanjiCount;
            }
        }

        return readingIndex == reading.Length;
    }

    public static string Assemble(string expression, string reading, OkuriganaSegment[] segments, int segmentCount, ulong mask)
    {
        int finalLength = 0;
        int bitIndex = 0;

        for (int i = 0; i < segmentCount; i++)
        {
            ref readonly OkuriganaSegment segment = ref segments[i];
            if (segment.IsKanji)
            {
                finalLength += IsBitSet(mask, bitIndex)
                    ? segment.ReadingLength
                    : segment.ExpressionLength;

                ++bitIndex;
            }
            else
            {
                finalLength += segment.ExpressionLength;
            }
        }

        return string.Create(finalLength, (expression, reading, segments, segmentCount, mask), static (span, state) =>
        {
            int position = 0;
            int bitIndex = 0;

            for (int i = 0; i < state.segmentCount; i++)
            {
                ref readonly OkuriganaSegment segment = ref state.segments[i];
                ReadOnlySpan<char> source;

                if (segment.IsKanji)
                {
                    bool useReading = IsBitSet(state.mask, bitIndex);
                    ++bitIndex;

                    source = useReading
                        ? state.reading.AsSpan(segment.ReadingStart, segment.ReadingLength)
                        : state.expression.AsSpan(segment.ExpressionStart, segment.ExpressionLength);
                }
                else
                {
                    source = state.expression.AsSpan(segment.ExpressionStart, segment.ExpressionLength);
                }

                source.CopyTo(span[position..]);
                position += source.Length;
            }
        });
    }

    private static bool IsKanji(ReadOnlySpan<char> span, int index, out int consumedCharCount)
    {
        char character = span[index];
        if (!char.IsHighSurrogate(character))
        {
            consumedCharCount = 1;
            int codePoint = character;
            return codePoint is (>= 0x4E00 and <= 0x9FFF) // CJK Unified Ideographs (4E00–9FFF)
                    or (>= 0x2E80 and <= 0x2FDF) // CJK Radicals Supplement (2E80–2EFF), Kangxi Radicals (2F00–2FDF)
                    or (>= 0x3190 and <= 0x319F) // Kanbun (3190–319F)
                    or (>= 0x31C0 and <= 0x31EF) // CJK Strokes (31C0–31EF)
                    or (>= 0x3400 and <= 0x4DBF) // CJK Unified Ideographs Extension A (3400–4DBF)
                    or (>= 0xF900 and <= 0xFAFF) // CJK Compatibility Ideographs(F900–FAFF)
                    or (>= 0xFE30 and <= 0xFE4F); // CJK Compatibility Forms (FE30–FE4F)
        }

        if (index + 1 < span.Length)
        {
            char nextChar = span[index + 1];
            if (char.IsLowSurrogate(nextChar))
            {
                consumedCharCount = 2;
                int codePoint = char.ConvertToUtf32(character, nextChar);

                return codePoint is (>= 0x20000 and <= 0x2A6DF) // CJK Unified Ideographs Extension B (20000–2A6DF)
                    or (>= 0x2A700 and <= 0x2EBEF) // CJK Unified Ideographs Extension C (2A700–2B73F), CJK Unified Ideographs Extension D (2B740–2B81F), CJK Unified Ideographs Extension E (2B820–2CEAF), CJK Unified Ideographs Extension F (2CEB0–2EBEF)
                    or (>= 0x2F800 and <= 0x2FA1F) // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
                    or (>= 0x30000 and <= 0x323AF); // CJK Unified Ideographs Extension G (30000–3134F), CJK Unified Ideographs Extension H (31350–323AF)
            }
        }

        consumedCharCount = 1;
        return false;
    }
}
