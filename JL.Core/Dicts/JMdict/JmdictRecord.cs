using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.Options;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Core.Utilities.Array;

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

        StringBuilder defBuilder = ObjectPoolManager.StringBuilderPool.Get();
        if (showWordClassesSharedByAllSenses || showMiscSharedByAllSenses || showDialectsSharedByAllSenses || showFieldsSharedByAllSenses)
        {
            if (showWordClassesSharedByAllSenses)
            {
                Debug.Assert(WordClassesSharedByAllSenses is not null);
                _ = defBuilder.Append('[').AppendJoin(", ", WordClassesSharedByAllSenses).Append("] ");
            }

            if (showMiscSharedByAllSenses)
            {
                Debug.Assert(MiscSharedByAllSenses is not null);
                _ = defBuilder.Append('[').AppendJoin(", ", MiscSharedByAllSenses).Append("] ");
            }

            if (showDialectsSharedByAllSenses)
            {
                Debug.Assert(DialectsSharedByAllSenses is not null);
                _ = defBuilder.Append('[').AppendJoin(", ", DialectsSharedByAllSenses).Append("] ");
            }

            if (showFieldsSharedByAllSenses)
            {
                Debug.Assert(FieldsSharedByAllSenses is not null);
                _ = defBuilder.Append('[').AppendJoin(", ", FieldsSharedByAllSenses).Append("] ");
            }

            if (multipleDefinitions && newlines)
            {
                _ = defBuilder.Replace(" ", "\n", defBuilder.Length - 1, 1);
            }
        }

        string[][] definitions = Definitions;
        string[]?[]? wordClasses = WordClasses;
        string[]?[]? misc = Misc;
        string[]?[]? dialects = Dialects;
        string[]?[]? fields = Fields;
        string?[]? definitionInfo = DefinitionInfo;
        LoanwordSource[]?[]? loanwordEtymology = LoanwordEtymology;
        string[]?[]? relatedTerms = RelatedTerms;
        string[]?[]? antonyms = Antonyms;

        for (int i = 0; i < definitions.Length; i++)
        {
            if (multipleDefinitions)
            {
                _ = defBuilder.Append(CultureInfo.InvariantCulture, $"{i + 1}. ");

                if (showWordClassInfo)
                {
                    Debug.Assert(wordClasses is not null);
                    string[]? wordClassesElement = wordClasses[i];
                    if (wordClassesElement is not null)
                    {
                        _ = defBuilder.Append('[').AppendJoin(", ", wordClassesElement).Append("] ");
                    }
                }

                if (showMiscInfo)
                {
                    Debug.Assert(misc is not null);
                    string[]? miscElement = misc[i];
                    if (miscElement is not null)
                    {
                        _ = defBuilder.Append('[').AppendJoin(", ", miscElement).Append("] ");
                    }
                }

                if (showDialectInfo)
                {
                    Debug.Assert(dialects is not null);
                    string[]? dialectsElement = dialects[i];
                    if (dialectsElement is not null)
                    {
                        _ = defBuilder.Append('[').AppendJoin(", ", dialectsElement).Append("] ");
                    }
                }

                if (showFieldsInfo)
                {
                    Debug.Assert(fields is not null);
                    string[]? fieldsElement = fields[i];
                    if (fieldsElement is not null)
                    {
                        _ = defBuilder.Append('[').AppendJoin(", ", fieldsElement).Append("] ");
                    }
                }
            }

            _ = defBuilder.AppendJoin("; ", definitions[i]).Append(' ');

            if (showExtraDefinitionInfo)
            {
                Debug.Assert(definitionInfo is not null);
                string? definitionInfoElement = definitionInfo[i];
                if (definitionInfoElement is not null)
                {
                    _ = defBuilder.Append(CultureInfo.InvariantCulture, $"({definitionInfoElement}) ");
                }
            }

            if (showSpellingRestrictionInfo)
            {
                AppendSpellingRestrictionInfo(defBuilder, showSpellingRestrictions, SpellingRestrictions, showReadingRestrictionss, ReadingRestrictions, i);
            }

            if (showLoanwordEtymology)
            {
                Debug.Assert(loanwordEtymology is not null);
                AppendLoanwordEtymology(defBuilder, loanwordEtymology[i]);
            }

            if (showRelatedTerms)
            {
                Debug.Assert(relatedTerms is not null);
                AppendRelatedTerms(defBuilder, relatedTerms[i]);
            }

            if (showAntonyms)
            {
                Debug.Assert(antonyms is not null);
                AppendAntonyms(defBuilder, antonyms[i]);
            }

            if (i + 1 != definitions.Length)
            {
                _ = defBuilder.Replace(' ', separator, defBuilder.Length - 1, 1);
            }
        }

        string def = defBuilder.ToString(0, defBuilder.Length - 1);
        ObjectPoolManager.StringBuilderPool.Return(defBuilder);
        return def;
    }

    private static void AppendSpellingRestrictionInfo(StringBuilder defBuilder, bool showSpellingRestrictions, string[]?[]? spellingRestrictions, bool showReadingRestrictionss, string[]?[]? readingRestrictions, int index)
    {
        string[]? spellingRestrictionsElement = null;
        if (showSpellingRestrictions)
        {
            Debug.Assert(spellingRestrictions is not null);
            spellingRestrictionsElement = spellingRestrictions[index];
        }

        string[]? readingRestrictionsElement = null;
        if (showReadingRestrictionss)
        {
            Debug.Assert(readingRestrictions is not null);
            readingRestrictionsElement = readingRestrictions[index];
        }

        bool spellingRestrictionsExist = spellingRestrictionsElement is not null;
        bool readingRestrictionsExist = readingRestrictionsElement is not null;
        if (spellingRestrictionsExist || readingRestrictionsExist)
        {
            _ = defBuilder.Append("(only applies to ");

            if (spellingRestrictionsExist)
            {
                Debug.Assert(spellingRestrictionsElement is not null);
                _ = defBuilder.AppendJoin("; ", spellingRestrictionsElement);
            }

            if (readingRestrictionsExist)
            {
                if (spellingRestrictionsExist)
                {
                    _ = defBuilder.Append("; ");
                }

                Debug.Assert(readingRestrictionsElement is not null);
                _ = defBuilder.AppendJoin("; ", readingRestrictionsElement);
            }

            _ = defBuilder.Append(") ");
        }
    }

    private static void AppendLoanwordEtymology(StringBuilder defBuilder, LoanwordSource[]? lSources)
    {
        if (lSources is not null)
        {
            _ = defBuilder.Append('(');

            for (int j = 0; j < lSources.Length; j++)
            {
                ref readonly LoanwordSource lSource = ref lSources[j];
                if (lSource.IsWasei)
                {
                    _ = defBuilder.Append("wasei ");
                }
                else if (j is 0)
                {
                    _ = defBuilder.Append("from ");
                }

                _ = defBuilder.Append(lSource.Language);

                if (lSource.OriginalWord is not null)
                {
                    _ = defBuilder.Append(CultureInfo.InvariantCulture, $": {lSource.OriginalWord}");
                }

                if (j + 1 < lSources.Length)
                {
                    _ = defBuilder.Append(lSource.IsPart ? " + " : ", ");
                }
            }

            _ = defBuilder.Append(") ");
        }
    }

    private static void AppendRelatedTerms(StringBuilder defBuilder, string[]? relatedTermsElement)
    {
        if (relatedTermsElement is not null)
        {
            if (relatedTermsElement.Length is 1)
            {
                _ = defBuilder.Append(CultureInfo.InvariantCulture, $"(related term: {relatedTermsElement[0]}) ");
            }
            else
            {
                _ = defBuilder.Append("(related terms: ").AppendJoin(", ", relatedTermsElement).Append(") ");
            }
        }
    }

    private static void AppendAntonyms(StringBuilder defBuilder, string[]? antonymsElement)
    {
        if (antonymsElement is not null)
        {
            if (antonymsElement.Length is 1)
            {
                _ = defBuilder.Append(CultureInfo.InvariantCulture, $"(antonym: {antonymsElement[0]}) ");
            }
            else
            {
                _ = defBuilder.Append("(antonyms: ").AppendJoin(", ", antonymsElement).Append(") ");
            }
        }
    }

    public int GetFrequency(IDictionary<string, IList<FrequencyRecord>> freqDict)
    {
        bool readingsExist = Readings is not null;
        if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(PrimarySpelling), out IList<FrequencyRecord>? freqResults))
        {
            int freqResultsCount = freqResults.Count;
            for (int i = 0; i < freqResultsCount; i++)
            {
                FrequencyRecord freqResult = freqResults[i];
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings.Contains(freqResult.Spelling)))
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
                if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(reading), out IList<FrequencyRecord>? readingFreqResults))
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
        if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(PrimarySpelling), out List<FrequencyRecord>? freqResults))
        {
            foreach (ref readonly FrequencyRecord freqResult in freqResults.AsReadOnlySpan())
            {
                if (freqResult.Spelling == PrimarySpelling
                    || (readingsExist && Readings.Contains(freqResult.Spelling)))
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
                if (freqDict.TryGetValue(JapaneseUtils.NormalizeText(reading), out List<FrequencyRecord>? readingFreqResults))
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
        unchecked
        {
            int hash = (17 * 37) + Id;
            hash = (hash * 37) + PrimarySpelling.GetHashCode(StringComparison.Ordinal);

            if (Readings is not null)
            {
                foreach (string reading in Readings)
                {
                    hash = (hash * 37) + reading.GetHashCode(StringComparison.Ordinal);
                }
            }
            else
            {
                hash *= 37;
            }

            foreach (string[] defs in Definitions)
            {
                foreach (string def in defs)
                {
                    hash = (hash * 37) + def.GetHashCode(StringComparison.Ordinal);
                }
            }

            return hash;
        }
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is JmdictRecord other
            && (ReferenceEquals(this, other) || (Id == other.Id
            && PrimarySpelling == other.PrimarySpelling
            && (other.Readings is not null
                ? Readings?.SequenceEqual(other.Readings) ?? false
                : Readings is null)
            && Definitions.SequenceEqual(other.Definitions, ArrayComparer<string>.Instance)));
    }

    public bool Equals(JmdictRecord? other)
    {
        return other is not null
            && (ReferenceEquals(this, other) || (Id == other.Id
            && PrimarySpelling == other.PrimarySpelling
            && (other.Readings is not null
                ? Readings?.SequenceEqual(other.Readings) ?? false
                : Readings is null)
            && Definitions.SequenceEqual(other.Definitions, ArrayComparer<string>.Instance)));
    }

    public static bool operator ==(JmdictRecord? left, JmdictRecord? right) => left?.Equals(right) ?? (right is null);
    public static bool operator !=(JmdictRecord? left, JmdictRecord? right) => !(left == right);
}
