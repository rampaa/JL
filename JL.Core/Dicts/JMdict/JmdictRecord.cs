using System.Diagnostics;
using System.Globalization;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal sealed class JmdictRecord : IDictRecordWithMultipleReadings, IGetFrequency, IEquatable<JmdictRecord>
{
    public int Id { get; }
    public string PrimarySpelling { get; }
    public string[]? PrimarySpellingOrthographyInfo { get; }
    public string[]? AlternativeSpellings { get; }
    public string[]?[]? AlternativeSpellingsOrthographyInfo { get; }
    public string[]? Readings { get; }
    public string[]?[]? ReadingsOrthographyInfo { get; }
    public string[][] Definitions { get; }
    public string[]?[]? WordClasses { get; } //e.g. noun +
    public string[]? WordClassesSharedByAllSenses { get; }
    public string[]?[]? SpellingRestrictions { get; }
    public string[]?[]? ReadingRestrictions { get; }
    public string[]?[]? Fields { get; } // e.g. "martial arts"
    public string[]? FieldsSharedByAllSenses { get; }
    public string[]?[]? Misc { get; } // e.g. "abbr" +
    public string[]? MiscSharedByAllSenses { get; }
    public string?[]? DefinitionInfo { get; } // e.g. "often derog" +
    public string[]?[]? Dialects { get; } // e.g. ksb
    public string[]? DialectsSharedByAllSenses { get; }
    public LoanwordSource[]?[]? LoanwordEtymology { get; }
    public string[]?[]? RelatedTerms { get; }
    public string[]?[]? Antonyms { get; }
    //public string[] Priorities { get; } // e.g. gai1

    public JmdictRecord(int id,
        string primarySpelling,
        string[][] definitions,
        string[]?[]? wordClasses,
        string[]? wordClassesSharedByAllSenses,
        string[]? primarySpellingOrthographyInfo,
        string[]? alternativeSpellings,
        string[]?[]? alternativeSpellingsOrthographyInfo,
        string[]? readings,
        string[]?[]? readingsOrthographyInfo,
        string[]?[]? spellingRestrictions,
        string[]?[]? readingRestrictions,
        string[]?[]? fields,
        string[]? fieldsSharedByAllSenses,
        string[]?[]? misc,
        string[]? miscSharedByAllSenses,
        string?[]? definitionInfo,
        string[]?[]? dialects,
        string[]? dialectsSharedByAllSenses,
        LoanwordSource[]?[]? loanwordEtymology,
        string[]?[]? relatedTerms,
        string[]?[]? antonyms)
    {
        Id = id;
        PrimarySpelling = primarySpelling;
        PrimarySpellingOrthographyInfo = primarySpellingOrthographyInfo;
        AlternativeSpellings = alternativeSpellings;
        AlternativeSpellingsOrthographyInfo = alternativeSpellingsOrthographyInfo;
        Readings = readings;
        ReadingsOrthographyInfo = readingsOrthographyInfo;
        Definitions = definitions;
        WordClasses = wordClasses;
        WordClassesSharedByAllSenses = wordClassesSharedByAllSenses;
        SpellingRestrictions = spellingRestrictions;
        ReadingRestrictions = readingRestrictions;
        Fields = fields;
        FieldsSharedByAllSenses = fieldsSharedByAllSenses;
        Misc = misc;
        MiscSharedByAllSenses = miscSharedByAllSenses;
        DefinitionInfo = definitionInfo;
        Dialects = dialects;
        DialectsSharedByAllSenses = dialectsSharedByAllSenses;
        LoanwordEtymology = loanwordEtymology;
        RelatedTerms = relatedTerms;
        Antonyms = antonyms;
    }

    public string BuildFormattedDefinition(DictOptions options)
    {
        Debug.Assert(options.NewlineBetweenDefinitions is not null);
        bool newlines = options.NewlineBetweenDefinitions.Value;
        char separator = newlines ? '\n' : '；';

        bool multipleDefinitions = Definitions.Length > 1;

        Debug.Assert(options.WordClassInfo is not null);
        bool showWordClassInfoOptionValue = options.WordClassInfo.Value;
        bool showWordClassInfo = showWordClassInfoOptionValue && WordClasses is not null;
        bool showWordClassesSharedByAllSenses = showWordClassInfoOptionValue && WordClassesSharedByAllSenses is not null;

        Debug.Assert(options.MiscInfo is not null);
        bool showMiscInfoOptionValue = options.MiscInfo.Value;
        bool showMiscInfo = showMiscInfoOptionValue && Misc is not null;
        bool showMiscSharedByAllSenses = showMiscInfoOptionValue && MiscSharedByAllSenses is not null;

        Debug.Assert(options.DialectInfo is not null);
        bool showDialectInfoOptionValue = options.DialectInfo.Value;
        bool showDialectInfo = showDialectInfoOptionValue && Dialects is not null;
        bool showDialectsSharedByAllSenses = showDialectInfoOptionValue && DialectsSharedByAllSenses is not null;

        Debug.Assert(options.WordTypeInfo is not null);
        bool showFieldInfoOptionValue = options.WordTypeInfo.Value;
        bool showFieldsInfo = showFieldInfoOptionValue && Fields is not null;
        bool showFieldsSharedByAllSenses = showFieldInfoOptionValue && FieldsSharedByAllSenses is not null;

        Debug.Assert(options.ExtraDefinitionInfo is not null);
        bool showExtraDefinitionInfo = options.ExtraDefinitionInfo.Value && DefinitionInfo is not null;

        Debug.Assert(options.SpellingRestrictionInfo is not null);
        bool showSpellingRestrictionInfo = options.SpellingRestrictionInfo.Value;
        bool showSpellingRestrictions = showSpellingRestrictionInfo && SpellingRestrictions is not null;
        bool showReadingRestrictionss = showSpellingRestrictionInfo && ReadingRestrictions is not null;

        Debug.Assert(options.LoanwordEtymology is not null);
        bool showLoanwordEtymology = options.LoanwordEtymology.Value && LoanwordEtymology is not null;

        Debug.Assert(options.RelatedTerm is not null);
        bool showRelatedTerms = options.RelatedTerm.Value && RelatedTerms is not null;

        Debug.Assert(options.Antonym is not null);
        bool showAntonyms = options.Antonym.Value && Antonyms is not null;

        StringBuilder defResult = new();
        if (showWordClassesSharedByAllSenses || showMiscSharedByAllSenses || showDialectsSharedByAllSenses || showFieldsSharedByAllSenses)
        {
            if (showWordClassesSharedByAllSenses)
            {
                Debug.Assert(WordClassesSharedByAllSenses is not null);
                _ = defResult.Append('[').AppendJoin(", ", WordClassesSharedByAllSenses).Append("] ");
            }

            if (showMiscSharedByAllSenses)
            {
                Debug.Assert(MiscSharedByAllSenses is not null);
                _ = defResult.Append('[').AppendJoin(", ", MiscSharedByAllSenses).Append("] ");
            }

            if (showDialectsSharedByAllSenses)
            {
                Debug.Assert(DialectsSharedByAllSenses is not null);
                _ = defResult.Append('[').AppendJoin(", ", DialectsSharedByAllSenses).Append("] ");
            }

            if (showFieldsSharedByAllSenses)
            {
                Debug.Assert(FieldsSharedByAllSenses is not null);
                _ = defResult.Append('[').AppendJoin(", ", FieldsSharedByAllSenses).Append("] ");
            }

            if (multipleDefinitions && newlines)
            {
                _ = defResult.Replace(" ", "\n", defResult.Length - 1, 1);
            }
        }

        string[][] definitions = Definitions;
        string[]?[]? wordClasses = WordClasses;
        string[]?[]? misc = Misc;
        string[]?[]? dialects = Dialects;
        string[]?[]? fields = Fields;
        string?[]? definitionInfo = DefinitionInfo;
        string[]?[]? spellingRestrictions = SpellingRestrictions;
        string[]?[]? readingRestrictions = ReadingRestrictions;
        LoanwordSource[]?[]? loanwordEtymology = LoanwordEtymology;
        string[]?[]? relatedTerms = RelatedTerms;
        string[]?[]? antonyms = Antonyms;

        for (int i = 0; i < definitions.Length; i++)
        {
            if (multipleDefinitions)
            {
                _ = defResult.Append(CultureInfo.InvariantCulture, $"{i + 1}. ");

                if (showWordClassInfo)
                {
                    Debug.Assert(wordClasses is not null);
                    string[]? wordClassesElement = wordClasses[i];
                    if (wordClassesElement is not null)
                    {
                        _ = defResult.Append('[').AppendJoin(", ", wordClassesElement).Append("] ");
                    }
                }

                if (showMiscInfo)
                {
                    Debug.Assert(misc is not null);
                    string[]? miscElement = misc[i];
                    if (miscElement is not null)
                    {
                        _ = defResult.Append('[').AppendJoin(", ", miscElement).Append("] ");
                    }
                }

                if (showDialectInfo)
                {
                    Debug.Assert(dialects is not null);
                    string[]? dialectsElement = dialects[i];
                    if (dialectsElement is not null)
                    {
                        _ = defResult.Append('[').AppendJoin(", ", dialectsElement).Append("] ");
                    }
                }

                if (showFieldsInfo)
                {
                    Debug.Assert(fields is not null);
                    string[]? fieldsElement = fields[i];
                    if (fieldsElement is not null)
                    {
                        _ = defResult.Append('[').AppendJoin(", ", fieldsElement).Append("] ");
                    }
                }
            }

            _ = defResult.AppendJoin(", ", definitions[i]).Append(' ');

            if (showExtraDefinitionInfo)
            {
                Debug.Assert(definitionInfo is not null);
                string? definitionInfoElement = definitionInfo[i];
                if (definitionInfoElement is not null)
                {
                    _ = defResult.Append(CultureInfo.InvariantCulture, $"({definitionInfoElement}) ");
                }
            }

            if (showSpellingRestrictionInfo)
            {
                string[]? spellingRestrictionsElement = null;
                if (showSpellingRestrictions)
                {
                    Debug.Assert(spellingRestrictions is not null);
                    spellingRestrictionsElement = spellingRestrictions[i];
                }

                string[]? readingRestrictionsElement = null;
                if (showReadingRestrictionss)
                {
                    Debug.Assert(readingRestrictions is not null);
                    readingRestrictionsElement = readingRestrictions[i];
                }

                bool spellingRestrictionsExist = spellingRestrictionsElement is not null;
                bool readingRestrictionsExist = readingRestrictionsElement is not null;
                if (spellingRestrictionsExist || readingRestrictionsExist)
                {
                    _ = defResult.Append("(only applies to ");

                    if (spellingRestrictionsExist)
                    {
                        Debug.Assert(spellingRestrictionsElement is not null);
                        _ = defResult.AppendJoin("; ", spellingRestrictionsElement);
                    }

                    if (readingRestrictionsExist)
                    {
                        if (spellingRestrictionsExist)
                        {
                            _ = defResult.Append("; ");
                        }

                        Debug.Assert(readingRestrictionsElement is not null);
                        _ = defResult.AppendJoin("; ", readingRestrictionsElement);
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showLoanwordEtymology)
            {
                Debug.Assert(loanwordEtymology is not null);
                ref readonly LoanwordSource[]? lSources = ref loanwordEtymology[i];
                if (lSources is not null)
                {
                    _ = defResult.Append('(');

                    for (int j = 0; j < lSources.Length; j++)
                    {
                        ref readonly LoanwordSource lSource = ref lSources[j];
                        if (lSource.IsWasei)
                        {
                            _ = defResult.Append("wasei ");
                        }
                        else if (j is 0)
                        {
                            _ = defResult.Append("from ");
                        }

                        _ = defResult.Append(lSource.Language);

                        if (lSource.OriginalWord is not null)
                        {
                            _ = defResult.Append(CultureInfo.InvariantCulture, $": {lSource.OriginalWord}");
                        }

                        if (j + 1 < lSources.Length)
                        {
                            _ = defResult.Append(lSource.IsPart ? " + " : ", ");
                        }
                    }

                    _ = defResult.Append(") ");
                }
            }

            if (showRelatedTerms)
            {
                Debug.Assert(relatedTerms is not null);
                string[]? relatedTermsElement = relatedTerms[i];
                if (relatedTermsElement is not null)
                {
                    if (relatedTermsElement.Length is 1)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"(related term: {relatedTermsElement[0]}) ");
                    }
                    else
                    {
                        _ = defResult.Append("(related terms: ").AppendJoin(", ", relatedTermsElement).Append(") ");
                    }
                }
            }

            if (showAntonyms)
            {
                Debug.Assert(antonyms is not null);
                string[]? antonymsElement = antonyms[i];
                if (antonymsElement is not null)
                {
                    if (antonymsElement.Length is 1)
                    {
                        _ = defResult.Append(CultureInfo.InvariantCulture, $"(antonym: {antonymsElement[0]}) ");
                    }
                    else
                    {
                        _ = defResult.Append("(antonyms: ").AppendJoin(", ", antonymsElement).Append(") ");
                    }
                }
            }

            if (i + 1 != definitions.Length)
            {
                _ = defResult.Replace(' ', separator, defResult.Length - 1, 1);
            }
        }

        return defResult.ToString(0, defResult.Length - 1);
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings.AsReadOnlySpan().Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            Debug.Assert(Readings is not null);
            string[] readings = Readings;
            for (int i = 0; i < readings.Length; i++)
            {
                string reading = readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out IList<FrequencyRecord>? readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyRecord readingFreqResult = readingFreqResults[j];
                        if (readingFreqResult.Spelling == PrimarySpelling
                            || (reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0])))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public int GetFrequency(Dictionary<string, List<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            foreach (ref readonly FrequencyRecord freqResult in freqResults.AsReadOnlySpan())
            {
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings.AsReadOnlySpan().Contains(freqResult.Spelling)))
                {
                    return freqResult.Frequency;
                }
            }
        }

        else if (readingsExist)
        {
            Debug.Assert(Readings is not null);
            string[] readings = Readings;
            for (int i = 0; i < readings.Length; i++)
            {
                string reading = readings[i];
                if (freqDict.TryGetValue(JapaneseUtils.KatakanaToHiragana(reading), out List<FrequencyRecord>? readingFreqResults))
                {
                    foreach (ref readonly FrequencyRecord readingFreqResult in readingFreqResults.AsReadOnlySpan())
                    {
                        if (readingFreqResult.Spelling == PrimarySpelling
                            || (reading == readingFreqResult.Spelling && JapaneseUtils.IsKatakana(reading[0])))
                        {
                            return readingFreqResult.Frequency;
                        }
                    }
                }
            }
        }

        return int.MaxValue;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, PrimarySpelling.GetHashCode(StringComparison.Ordinal));
    }

    public override bool Equals(object? obj)
    {
        return obj is JmdictRecord jmdictRecord
               && Id == jmdictRecord.Id
               && PrimarySpelling == jmdictRecord.PrimarySpelling;
    }

    public bool Equals(JmdictRecord? other)
    {
        return other is not null
            && Id == other.Id
            && PrimarySpelling == other.PrimarySpelling;
    }

    public static bool operator ==(JmdictRecord? left, JmdictRecord? right) => left?.Equals(right) ?? right is null;
    public static bool operator !=(JmdictRecord? left, JmdictRecord? right) => !left?.Equals(right) ?? right is not null;
}
