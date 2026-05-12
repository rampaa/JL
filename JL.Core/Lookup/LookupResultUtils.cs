using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Deconjugation;
using JL.Core.Utilities;

namespace JL.Core.Lookup;

public static class LookupResultUtils
{
    internal static string? DeconjugationProcessesToText(ReadOnlySpan<ProcessNode?> processList)
    {
        if (processList.Length is 1)
        {
            ProcessNode? processNode = processList[0];
            Debug.Assert(processNode is not null);
            return processNode.GetCachedDeconjugationProcessText();
        }

        StringBuilder deconjugationProcessBuilder = ObjectPoolManager.StringBuilderPool.Get();
        for (int i = 0; i < processList.Length; i++)
        {
            ProcessNode? process = processList[i];
            Debug.Assert(process is not null);
            string? pathText = process.GetFormattedText();
            if (pathText is not null)
            {
                if (i is 0)
                {
                    _ = deconjugationProcessBuilder.Append('～').Append(pathText);
                }
                else
                {
                    _ = deconjugationProcessBuilder.Append("; ").Append(pathText);
                }
            }
        }

        string? deconjugationProcess = deconjugationProcessBuilder.Length is 0
            ? null
            : deconjugationProcessBuilder.ToString();

        ObjectPoolManager.StringBuilderPool.Return(deconjugationProcessBuilder);

        return deconjugationProcess;
    }

    public static string GradeToText(int grade)
    {
        return grade switch
        {
            >= 1 and <= 6 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Kyouiku)"),
            8 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jouyou)"),
            9 or 10 => string.Create(CultureInfo.InvariantCulture, $"{grade} (Jinmeiyou)"),
            _ => "Hyougai"
        };
    }

    public static string FrequenciesToText(ReadOnlySpan<LookupFrequencyResult> frequencies, bool forMining, bool singleDict)
    {
        if (!forMining && singleDict)
        {
            return string.Create(CultureInfo.InvariantCulture, $"#{frequencies[0].Freq}");
        }

        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        for (int i = 0; i < frequencies.Length; i++)
        {
            ref readonly LookupFrequencyResult lookupFreqResult = ref frequencies[i];
            _ = sb.Append(CultureInfo.InvariantCulture, $"{lookupFreqResult.Name}: {lookupFreqResult.Freq}");
            if (i + 1 != frequencies.Length)
            {
                _ = sb.Append(", ");
            }
        }

        string text = sb.ToString();
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return text;
    }

    public static string ElementWithOrthographyInfoToText(string[] elements, string[]?[] orthographyInfoList)
    {
        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
        string text = ElementWithOrthographyInfoToText(sb, elements, orthographyInfoList).ToString();
        ObjectPoolManager.StringBuilderPool.Return(sb);
        return text;
    }

    public static string ElementWithOrthographyInfoToTextWithParentheses(string[] alternativeSpellings, string[]?[] aOrthographyInfoList)
    {
        StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get().Append('(');

        string text = ElementWithOrthographyInfoToText(sb, alternativeSpellings, aOrthographyInfoList)
            .Append(')')
            .ToString();

        ObjectPoolManager.StringBuilderPool.Return(sb);
        return text;
    }

    private static StringBuilder ElementWithOrthographyInfoToText(StringBuilder sb, string[] elements, string[]?[] orthographyInfoList)
    {
        for (int index = 0; index < elements.Length; index++)
        {
            _ = sb.Append(elements[index]);

            if (index < orthographyInfoList.Length)
            {
                string[]? orthographyInfo = orthographyInfoList[index];
                if (orthographyInfo is not null)
                {
                    _ = sb.Append(" [").AppendJoin(", ", orthographyInfo).Append(']');
                }
            }

            if (index + 1 != elements.Length)
            {
                _ = sb.Append('、');
            }
        }

        return sb;
    }
}
