using System.Text;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;

namespace JL.Core.Dicts.EPWING.EpwingNazeka;

public class EpwingNazekaRecord : IEpwingRecord, IDictRecordWithGetFrequency
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public List<string>? AlternativeSpellings { get; }
    public List<string>? Definitions { get; set; }

    public EpwingNazekaRecord(string primarySpelling, string? reading, List<string>? alternativeSpellings, List<string>? definitions)
    {
        PrimarySpelling = primarySpelling;
        Reading = reading;
        AlternativeSpellings = alternativeSpellings;
        Definitions = definitions;
    }

    public string? BuildFormattedDefinition(DictOptions? options)
    {
        if (Definitions is null)
            return null;

        StringBuilder defResult = new();

        string separator = options?.NewlineBetweenDefinitions?.Value ?? true
            ? "\n"
            : "; ";

        for (int i = 0; i < Definitions.Count; i++)
        {
            defResult.Append(Definitions[i] + separator);
        }

        return defResult.ToString().TrimEnd(' ', '\n');
    }

    public int GetFrequency(Freq freq)
    {
        int frequency = int.MaxValue;

        if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(PrimarySpelling),
                out List<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];

                if (Reading == freqResult.Spelling || PrimarySpelling == freqResult.Spelling)
                {
                    if (frequency > freqResult.Frequency)
                    {
                        frequency = freqResult.Frequency;
                    }
                }
            }

            if (frequency == int.MaxValue && AlternativeSpellings != null)
            {
                int alternativeSpellingsCount = AlternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (freq.Contents.TryGetValue(
                            Kana.KatakanaToHiraganaConverter(AlternativeSpellings[i]),
                            out List<FrequencyRecord>? alternativeSpellingFreqResults))
                    {
                        int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                        for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                        {
                            FrequencyRecord alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                            if (Reading == alternativeSpellingFreqResult.Spelling)
                            {
                                if (frequency > alternativeSpellingFreqResult.Frequency)
                                {
                                    frequency = alternativeSpellingFreqResult.Frequency;
                                }
                            }
                        }
                    }
                }
            }
        }

        else if (Reading != null)
        {
            string reading = Reading;

            if (freq.Contents.TryGetValue(Kana.KatakanaToHiraganaConverter(reading),
                    out List<FrequencyRecord>? readingFreqResults))
            {
                int readingFreqResultsCount = readingFreqResults.Count;
                for (int j = 0; j < readingFreqResultsCount; j++)
                {
                    FrequencyRecord readingFreqResult = readingFreqResults[j];

                    if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                        || (AlternativeSpellings?.Contains(readingFreqResult.Spelling) ?? false))
                    {
                        if (frequency > readingFreqResult.Frequency)
                        {
                            frequency = readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return frequency;
    }
}
