using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JL.Core.Utilities.Japanese.Okurigana;

internal static class OkuriganaVariantGenerator
{
    private const int StackAllocBytesThreshold = 2048;
    private const int MaxStackRuns = 128;

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

        ExpressionRun[]? rentedRuns = null;
        byte[]? rentedWays = null;

        try
        {
            Span<ExpressionRun> runs = expression.Length <= MaxStackRuns
                ? stackalloc ExpressionRun[expression.Length]
                : (rentedRuns = ArrayPool<ExpressionRun>.Shared.Rent(expression.Length));

            int runCount = ParseRuns(expression, runs);
            ReadOnlySpan<ExpressionRun> activeRuns = runs[..runCount];

            int readingLength = reading.Length;
            int cols = readingLength + 1;
            int waysSize = (runCount + 1) * cols;

            Span<byte> ways = waysSize <= StackAllocBytesThreshold
                ? stackalloc byte[waysSize]
                : (rentedWays = ArrayPool<byte>.Shared.Rent(waysSize));

            return TryProcessIterative(expression, reading, activeRuns, ways, segments, out count, out kanjiCount);
        }
        finally
        {
            if (rentedRuns is not null)
            {
                ArrayPool<ExpressionRun>.Shared.Return(rentedRuns);
            }

            if (rentedWays is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedWays);
            }
        }
    }

    private static bool TryProcessIterative(
        ReadOnlySpan<char> expression,
        ReadOnlySpan<char> reading,
        ReadOnlySpan<ExpressionRun> runs,
        Span<byte> ways,
        Span<OkuriganaSegment> segments,
        out int segmentCount,
        out int kanjiCount)
    {
        segmentCount = 0;
        kanjiCount = 0;

        int runCount = runs.Length;
        int readingLength = reading.Length;
        int cols = readingLength + 1;

        int lastRowOffset = runCount * cols;
        ways.Slice(lastRowOffset, readingLength).Clear();
        ways[lastRowOffset + readingLength] = 1;

        for (int i = runCount - 1; i >= 0; i--)
        {
            ExpressionRun run = runs[i];

            Span<byte> currentRow = ways.Slice(i * cols, cols);
            Span<byte> nextRow = ways.Slice((i + 1) * cols, cols);

            if (run.IsKanji)
            {
                int runningSum = 0;
                for (int j = readingLength; j >= 0; j--)
                {
                    currentRow[j] = (byte)Math.Min(2, runningSum);

                    runningSum += nextRow[j];
                    if (runningSum > 2)
                    {
                        runningSum = 2;
                    }
                }
            }
            else
            {
                ReadOnlySpan<char> runText = expression.Slice(run.Start, run.Length);
                for (int j = 0; j <= readingLength; j++)
                {
                    int matchIdx = j + run.Length;
                    currentRow[j] = (matchIdx <= readingLength && reading[j..].StartsWith(runText))
                        ? nextRow[matchIdx]
                        : (byte)0;
                }
            }
        }

        if (ways[0] is not 1)
        {
            return false;
        }

        int currentReadingIndex = 0;
        for (int i = 0; i < runCount; i++)
        {
            ExpressionRun run = runs[i];
            Span<byte> nextRow = ways.Slice((i + 1) * cols, cols);

            if (run.IsKanji)
            {
                for (int readingEnd = currentReadingIndex + 1; readingEnd <= readingLength; readingEnd++)
                {
                    if (nextRow[readingEnd] is 1)
                    {
                        segments[segmentCount] = new OkuriganaSegment(run.Start, run.Length, currentReadingIndex, readingEnd - currentReadingIndex, true);

                        ++segmentCount;
                        currentReadingIndex = readingEnd;
                        ++kanjiCount;
                        break;
                    }
                }
            }
            else
            {
                segments[segmentCount] = new OkuriganaSegment(run.Start, run.Length, currentReadingIndex, run.Length, false);
                ++segmentCount;
                currentReadingIndex += run.Length;
            }
        }

        return true;
    }

    private static int ParseRuns(ReadOnlySpan<char> expression, Span<ExpressionRun> runs)
    {
        int runCount = 0;
        int i = 0;
        int expressionLength = expression.Length;

        while (i < expressionLength)
        {
            int start = i;
            bool isKanji = IsKanjiLike(expression, i, out int consumed);
            i += consumed;

            while (i < expressionLength)
            {
                if (IsKanjiLike(expression, i, out int nextConsumed) != isKanji)
                {
                    break;
                }

                i += nextConsumed;
            }

            runs[runCount] = new ExpressionRun(start, i - start, isKanji);
            ++runCount;
        }

        return runCount;
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
                    source = IsBitSet(state.mask, bitIndex)
                        ? state.reading.AsSpan(segment.ReadingStart, segment.ReadingLength)
                        : state.expression.AsSpan(segment.ExpressionStart, segment.ExpressionLength);

                    ++bitIndex;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsKanjiLike(ReadOnlySpan<char> span, int index, out int consumedCharCount)
    {
        char character = span[index];
        if (!char.IsHighSurrogate(character))
        {
            consumedCharCount = 1;
            return (ushort)character is (>= 0x4E00 and <= 0x9FFF) // CJK Unified Ideographs (4E00–9FFF)
                    or (>= 0x3400 and <= 0x4DBF) // CJK Unified Ideographs Extension A (3400–4DBF)
                    or (>= 0x2E80 and <= 0x2FDF) // CJK Radicals Supplement (2E80–2EFF), Kangxi Radicals (2F00–2FDF)
                    or (>= 0x3005 and <= 0x3007) // Iteration marks, etc. (々 〆 〇)
                    or (>= 0x3031 and <= 0x303B) // Vertical Kana Repeat Marks (〱–〺, 〻)
                    or (>= 0x309D and <= 0x309E) // Hiragana Iteration Marks (ゝ ゞ)
                    or (>= 0x30F5 and <= 0x30F6) // Small Ke/Ka (ヵ ヶ)
                    or (>= 0x30FD and <= 0x30FE) // Katakana Iteration Marks (ヽ ヾ)
                    or (>= 0x3190 and <= 0x319F) // Kanbun (3190–319F)
                    or (>= 0x31C0 and <= 0x31EF) // CJK Strokes (31C0–31EF)
                    or (>= 0xF900 and <= 0xFAFF) // CJK Compatibility Ideographs(F900–FAFF)
                    or (>= 0xFE10 and <= 0xFE1F) // Vertical Forms (FE10–FE1F)
                    or (>= 0xFE30 and <= 0xFE4F); // CJK Compatibility Forms (FE30–FE4F)
        }

        Debug.Assert(span.Length > index + 1);
        char nextChar = span[index + 1];
        Debug.Assert(char.IsLowSurrogate(nextChar));

        consumedCharCount = 2;

        int codePoint = char.ConvertToUtf32(character, nextChar);
        return codePoint is (>= 0x20000 and <= 0x2A6DF) // CJK Unified Ideographs Extension B (20000–2A6DF)
            or (>= 0x2A700 and <= 0x2EBEF) // CJK Unified Ideographs Extension C (2A700–2B73F), CJK Unified Ideographs Extension D (2B740–2B81F), CJK Unified Ideographs Extension E (2B820–2CEAF), CJK Unified Ideographs Extension F (2CEB0–2EBEF)
            or (>= 0x2F800 and <= 0x2FA1F) // CJK Compatibility Ideographs Supplement (2F800–2FA1F)
            or (>= 0x30000 and <= 0x3347F) // CJK Unified Ideographs Extension G (30000–3134F), CJK Unified Ideographs Extension H (31350–323AF), CJK Unified Ideographs Extension J (323B0-3347F)
            or (>= 0x1D360 and <= 0x1D37F); // Counting Rod Numerals (1D360-1D37F)
    }
}
