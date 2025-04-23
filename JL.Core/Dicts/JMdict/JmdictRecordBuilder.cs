using System.Diagnostics;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictRecordBuilder
{
    public static void AddToDictionary(in JmdictEntry entry, IDictionary<string, IList<IDictRecord>> jmdictDictionary)
    {
        ReadOnlySpan<KanjiElement> kanjiElementsWithoutSearchOnlyForms = entry.KanjiElements.Where(static ke => !ke.KeInfList.AsReadOnlySpan().Contains("sK")).ToList().AsReadOnlySpan();
        string[] allSpellingsWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length];
        string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length][];
        for (int i = 0; i < kanjiElementsWithoutSearchOnlyForms.Length; i++)
        {
            ref readonly KanjiElement kanjiElement = ref kanjiElementsWithoutSearchOnlyForms[i];
            allSpellingsWithoutSearchOnlyForms[i] = kanjiElement.Keb;
            allKanjiOrthographyInfoWithoutSearchOnlyForms[i] = kanjiElement.KeInfList.TrimToArray();
        }

        string? firstPrimarySpelling;
        string[]? alternativeSpellingsForFirstPrimarySpelling;

        bool spellingsWithoutSearchOnlyFormsExist = allSpellingsWithoutSearchOnlyForms.Length > 0;
        if (spellingsWithoutSearchOnlyFormsExist)
        {
            firstPrimarySpelling = allSpellingsWithoutSearchOnlyForms[0];
            alternativeSpellingsForFirstPrimarySpelling = allSpellingsWithoutSearchOnlyForms.RemoveAt(0);
        }
        else
        {
            firstPrimarySpelling = null;
            alternativeSpellingsForFirstPrimarySpelling = null;
        }

        int index = 0;
        ReadOnlySpan<KanjiElement> kanjiElementsSpan = entry.KanjiElements.AsReadOnlySpan();
        ReadOnlySpan<ReadingElement> readingElementsSpan = entry.ReadingElements.AsReadOnlySpan();
        ReadOnlySpan<Sense> senseListSpan = entry.SenseList.AsReadOnlySpan();

        int kanjiElemensSpanLength = kanjiElementsSpan.Length;
        int readingElementsLength = readingElementsSpan.Length;
        int senseListSpanLength = senseListSpan.Length;

        string? firstPrimarySpellingInHiragana = firstPrimarySpelling is not null
            ? JapaneseUtils.KatakanaToHiragana(firstPrimarySpelling)
            : null;

        Dictionary<string, JmdictRecord> recordDictionary = new(kanjiElemensSpanLength + readingElementsLength, StringComparer.Ordinal);
        if (spellingsWithoutSearchOnlyFormsExist)
        {
            foreach (ref readonly KanjiElement kanjiElement in kanjiElementsSpan)
            {
                ReadOnlySpan<string> keInfListSpan = kanjiElement.KeInfList.AsReadOnlySpan();
                string key = JapaneseUtils.KatakanaToHiragana(kanjiElement.Keb).GetPooledString();
                if (recordDictionary.ContainsKey(key))
                {
                    if (!keInfListSpan.Contains("sK"))
                    {
                        ++index;
                    }

                    continue;
                }

                if (keInfListSpan.Contains("sK"))
                {
                    Debug.Assert(firstPrimarySpellingInHiragana is not null);
                    if (recordDictionary.TryGetValue(firstPrimarySpellingInHiragana, out JmdictRecord? primaryRecord))
                    {
                        recordDictionary.Add(key, primaryRecord);
                    }

                    continue;
                }

                List<string> readingList = new(readingElementsLength);
                List<string[]?> readingsOrthographyInfoList = new(readingElementsLength);

                foreach (ref readonly ReadingElement readingElement in readingElementsSpan)
                {
                    if (!readingElement.ReInfList.AsReadOnlySpan().Contains("sk"))
                    {
                        ReadOnlySpan<string> reRestrListSpan = readingElement.ReRestrList.AsReadOnlySpan();
                        if (reRestrListSpan.Length is 0 || reRestrListSpan.Contains(kanjiElement.Keb))
                        {
                            readingList.Add(readingElement.Reb);
                            readingsOrthographyInfoList.Add(readingElement.ReInfList.TrimToArray());
                        }
                    }
                }

                List<string[]> definitionList = new(senseListSpanLength);
                List<string[]> wordClassList = new(senseListSpanLength);
                List<string[]?> readingRestrictionList = new(senseListSpanLength);
                List<string[]?> spellingRestrictionList = new(senseListSpanLength);
                List<string[]?> fieldList = new(senseListSpanLength);
                List<string[]?> miscList = new(senseListSpanLength);
                List<string[]?> dialectList = new(senseListSpanLength);
                List<string?> definitionInfoList = new(senseListSpanLength);
                List<string[]?> relatedTermList = new(senseListSpanLength);
                List<string[]?> antonymList = new(senseListSpanLength);
                List<LoanwordSource[]?> loanwordSourceList = new(senseListSpanLength);

                ReadOnlySpan<string> readingListSpan = readingList.AsReadOnlySpan();
                foreach (ref readonly Sense sense in senseListSpan)
                {
                    ReadOnlySpan<string> stagKListSpan = sense.StagKList.AsReadOnlySpan();
                    ReadOnlySpan<string> stagRListSpan = sense.StagRList.AsReadOnlySpan();

                    if ((stagKListSpan.Length is 0 && stagRListSpan.Length is 0)
                        || stagKListSpan.Contains(kanjiElement.Keb)
                        || stagRListSpan.ContainsAny(readingListSpan))
                    {
                        definitionList.Add(sense.GlossList.ToArray());
                        wordClassList.Add(sense.PosList.ToArray());
                        readingRestrictionList.Add(sense.StagRList.TrimToArray());
                        spellingRestrictionList.Add(sense.StagKList.TrimToArray());
                        fieldList.Add(sense.FieldList.TrimToArray());
                        miscList.Add(sense.MiscList.TrimToArray());
                        dialectList.Add(sense.DialList.TrimToArray());
                        definitionInfoList.Add(sense.SInf);
                        relatedTermList.Add(sense.XRefList.TrimToArray());
                        antonymList.Add(sense.AntList.TrimToArray());
                        loanwordSourceList.Add(sense.LSourceList.TrimToArray());
                    }
                }

                (string[]?[]? exclusiveWordClasses, string[]? wordClassesSharedByAllSenses) = GetExclusiveAndSharedValuesForNonNullableSenseField(wordClassList);
                (string[]?[]? exclusiveMiscValues, string[]? miscValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(miscList);
                (string[]?[]? exclusiveFieldValues, string[]? fieldValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(fieldList);
                (string[]?[]? exclusiveDialectValues, string[]? dialectValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(dialectList);

                JmdictRecord record = new(entry.Id,
                    kanjiElement.Keb,
                    definitionList.ToArray(),
                    exclusiveWordClasses,
                    wordClassesSharedByAllSenses,
                    allKanjiOrthographyInfoWithoutSearchOnlyForms[index],
                    allSpellingsWithoutSearchOnlyForms.RemoveAt(index),
                    allKanjiOrthographyInfoWithoutSearchOnlyForms.RemoveAt(index),
                    readingList.TrimToArray(),
                    readingsOrthographyInfoList.TrimListOfNullableElementsToArray(),
                    spellingRestrictionList.TrimListOfNullableElementsToArray(),
                    readingRestrictionList.TrimListOfNullableElementsToArray(),
                    exclusiveFieldValues,
                    fieldValuesSharedByAllSenses,
                    exclusiveMiscValues,
                    miscValuesSharedByAllSenses,
                    definitionInfoList.TrimListOfNullableElementsToArray(),
                    exclusiveDialectValues,
                    dialectValuesSharedByAllSenses,
                    loanwordSourceList.TrimListOfNullableElementsToArray(),
                    relatedTermList.TrimListOfNullableElementsToArray(),
                    antonymList.TrimListOfNullableElementsToArray());

                recordDictionary.Add(key, record);

                ++index;
            }
        }

        ReadOnlySpan<ReadingElement> readingElementsWithoutSearchOnlyForms = entry.ReadingElements.Where(static ke => !ke.ReInfList.AsReadOnlySpan().Contains("sk")).ToList().AsReadOnlySpan();
        bool readingElementsWithoutSearchOnlyFormsExist = readingElementsWithoutSearchOnlyForms.Length > 0;
        if (readingElementsWithoutSearchOnlyFormsExist)
        {
            string[] allReadingsWithoutSearchOnlyForms = new string[readingElementsWithoutSearchOnlyForms.Length];
            string[]?[] allROrthographyInfoWithoutSearchOnlyForms = new string[readingElementsWithoutSearchOnlyForms.Length][];
            for (int i = 0; i < readingElementsWithoutSearchOnlyForms.Length; i++)
            {
                ref readonly ReadingElement readingElement = ref readingElementsWithoutSearchOnlyForms[i];
                allReadingsWithoutSearchOnlyForms[i] = readingElement.Reb;
                allROrthographyInfoWithoutSearchOnlyForms[i] = readingElement.ReInfList.TrimToArray();
            }

            string firstReadingInHiragana = JapaneseUtils.KatakanaToHiragana(allReadingsWithoutSearchOnlyForms[0]);

            index = 0;
            for (int i = 0; i < readingElementsSpan.Length; i++)
            {
                ref readonly ReadingElement readingElement = ref readingElementsSpan[i];

                ReadOnlySpan<string> reInfListSpan = readingElement.ReInfList.AsReadOnlySpan();
                string key = JapaneseUtils.KatakanaToHiragana(readingElement.Reb).GetPooledString();
                if (recordDictionary.ContainsKey(key))
                {
                    if (!reInfListSpan.Contains("sk"))
                    {
                        ++index;
                    }

                    continue;
                }

                if (reInfListSpan.Contains("sk"))
                {
                    if (recordDictionary.TryGetValue(firstReadingInHiragana, out JmdictRecord? primaryRecord))
                    {
                        recordDictionary.Add(key, primaryRecord);
                    }

                    continue;
                }

                string primarySpelling;
                string[]? primarySpellingOrthographyInfo = null;
                string[]? readings = null;
                string[]?[]? readingsOrthographyInfo = null;
                string[]? alternativeSpellings;
                string[]?[]? alternativeSpellingsOrthographyInfo = null;

                if (readingElement.ReRestrList.Count > 0 || spellingsWithoutSearchOnlyFormsExist)
                {
                    if (readingElement.ReRestrList.Count > 0)
                    {
                        primarySpelling = readingElement.ReRestrList[0];
                        alternativeSpellings = readingElement.ReRestrList.RemoveAtToArray(0);
                    }

                    else
                    {
                        Debug.Assert(firstPrimarySpelling is not null);
                        primarySpelling = firstPrimarySpelling;
                        alternativeSpellings = alternativeSpellingsForFirstPrimarySpelling;
                    }

                    if (recordDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(primarySpelling), out JmdictRecord? mainEntry))
                    {
                        readings = mainEntry.Readings;
                        primarySpellingOrthographyInfo = mainEntry.PrimarySpellingOrthographyInfo;
                        alternativeSpellingsOrthographyInfo = mainEntry.AlternativeSpellingsOrthographyInfo;
                        readingsOrthographyInfo = mainEntry.ReadingsOrthographyInfo;
                    }
                }

                else
                {
                    primarySpelling = readingElement.Reb;
                    primarySpellingOrthographyInfo = allROrthographyInfoWithoutSearchOnlyForms[index];

                    alternativeSpellings = allReadingsWithoutSearchOnlyForms.RemoveAt(index);
                    alternativeSpellingsOrthographyInfo = allROrthographyInfoWithoutSearchOnlyForms.RemoveAt(index);
                }

                List<string[]> definitionList = new(senseListSpanLength);
                List<string[]> wordClassList = new(senseListSpanLength);
                List<string[]?> readingRestrictionList = new(senseListSpanLength);
                List<string[]?> spellingRestrictionList = new(senseListSpanLength);
                List<string[]?> fieldList = new(senseListSpanLength);
                List<string[]?> miscList = new(senseListSpanLength);
                List<string[]?> dialectList = new(senseListSpanLength);
                List<string?> definitionInfoList = new(senseListSpanLength);
                List<string[]?> relatedTermList = new(senseListSpanLength);
                List<string[]?> antonymList = new(senseListSpanLength);
                List<LoanwordSource[]?> loanwordSourceList = new(senseListSpanLength);

                bool alternativeSpellingsExist = alternativeSpellings is not null;
                foreach (ref readonly Sense sense in senseListSpan)
                {
                    ReadOnlySpan<string> stagKListSpan = sense.StagKList.AsReadOnlySpan();
                    ReadOnlySpan<string> stagRListSpan = sense.StagRList.AsReadOnlySpan();

                    if ((stagKListSpan.Length is 0 && stagRListSpan.Length is 0)
                        || stagRListSpan.Contains(readingElement.Reb)
                        || stagKListSpan.Contains(primarySpelling)
                        || (alternativeSpellingsExist && stagKListSpan.ContainsAny(alternativeSpellings)))
                    {
                        definitionList.Add(sense.GlossList.ToArray());
                        wordClassList.Add(sense.PosList.ToArray());
                        readingRestrictionList.Add(sense.StagRList.TrimToArray());
                        spellingRestrictionList.Add(sense.StagKList.TrimToArray());
                        fieldList.Add(sense.FieldList.TrimToArray());
                        miscList.Add(sense.MiscList.TrimToArray());
                        dialectList.Add(sense.DialList.TrimToArray());
                        definitionInfoList.Add(sense.SInf);
                        relatedTermList.Add(sense.XRefList.TrimToArray());
                        antonymList.Add(sense.AntList.TrimToArray());
                        loanwordSourceList.Add(sense.LSourceList.TrimToArray());
                    }
                }

                (string[]?[]? exclusiveWordClasses, string[]? wordClassesSharedByAllSenses) = GetExclusiveAndSharedValuesForNonNullableSenseField(wordClassList);
                (string[]?[]? exclusiveMiscValues, string[]? miscValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(miscList);
                (string[]?[]? exclusiveFieldValues, string[]? fieldValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(fieldList);
                (string[]?[]? exclusiveDialectValues, string[]? dialectValuesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(dialectList);

                JmdictRecord record = new(entry.Id,
                    primarySpelling,
                    definitionList.ToArray(),
                    exclusiveWordClasses,
                    wordClassesSharedByAllSenses,
                    primarySpellingOrthographyInfo,
                    alternativeSpellings,
                    alternativeSpellingsOrthographyInfo,
                    readings,
                    readingsOrthographyInfo,
                    spellingRestrictionList.TrimListOfNullableElementsToArray(),
                    readingRestrictionList.TrimListOfNullableElementsToArray(),
                    exclusiveFieldValues,
                    fieldValuesSharedByAllSenses,
                    exclusiveMiscValues,
                    miscValuesSharedByAllSenses,
                    definitionInfoList.TrimListOfNullableElementsToArray(),
                    exclusiveDialectValues,
                    dialectValuesSharedByAllSenses,
                    loanwordSourceList.TrimListOfNullableElementsToArray(),
                    relatedTermList.TrimListOfNullableElementsToArray(),
                    antonymList.TrimListOfNullableElementsToArray());

                // record.Priorities = kanjiElement.KePriList

                recordDictionary.Add(key, record);

                ++index;

                if (i is 0 && allSpellingsWithoutSearchOnlyForms.Length is 0)
                {
                    foreach (ref readonly KanjiElement kanjiElement in kanjiElementsSpan)
                    {
                        _ = recordDictionary.TryAdd(JapaneseUtils.KatakanaToHiragana(kanjiElement.Keb.GetPooledString()), record);
                    }
                }
            }
        }

        foreach ((string key, JmdictRecord jmdictRecord) in recordDictionary)
        {
            if (jmdictDictionary.TryGetValue(key, out IList<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(jmdictRecord);
            }
            else
            {
                jmdictDictionary[key] = [jmdictRecord];
            }
        }
    }

    private static (string[]?[]? exclusiveSenseFieldValues, string[]? senseFieldValuesSharedByAllSenses) GetExclusiveAndSharedValuesForNonNullableSenseField(List<string[]> senseField)
    {
        if (senseField.Count is 0)
        {
            return (null, null);
        }

        if (senseField.Count is 1)
        {
            return (null, senseField[0]);
        }

        ReadOnlySpan<string[]> senseFieldSpan = senseField.AsReadOnlySpan();
        List<string>?[] exclusiveSenseFieldListArray = new List<string>?[senseFieldSpan.Length];
        List<string> senseFieldValuesSharedByAllSenses = [];

        for (int i = 0; i < senseFieldSpan.Length; i++)
        {
            foreach (string senseFieldValue in senseFieldSpan[i])
            {
                bool containsAll = true;
                foreach (ref readonly string[] senseItem in senseFieldSpan)
                {
                    if (!senseItem.AsSpan().Contains(senseFieldValue))
                    {
                        exclusiveSenseFieldListArray[i] ??= [];

                        List<string>? exclusiveSenseFieldList = exclusiveSenseFieldListArray[i];
                        Debug.Assert(exclusiveSenseFieldList is not null);

                        exclusiveSenseFieldList.Add(senseFieldValue);
                        containsAll = false;
                        break;
                    }
                }

                if (containsAll && !senseFieldValuesSharedByAllSenses.AsReadOnlySpan().Contains(senseFieldValue))
                {
                    senseFieldValuesSharedByAllSenses.Add(senseFieldValue);
                }
            }
        }

        if (senseFieldValuesSharedByAllSenses.Count is 0)
        {
            return (senseField.TrimToArray(), null);
        }

        bool allElementsAreNull = true;
        foreach (List<string>? item in exclusiveSenseFieldListArray)
        {
            if (item is not null)
            {
                allElementsAreNull = false;
                break;
            }
        }

        if (allElementsAreNull)
        {
            return (null, senseFieldValuesSharedByAllSenses.TrimToArray());
        }

        string[]?[] exclusiveSenseFieldValuesArray = new string[exclusiveSenseFieldListArray.Length][];
        for (int i = 0; i < exclusiveSenseFieldListArray.Length; i++)
        {
            exclusiveSenseFieldValuesArray[i] = exclusiveSenseFieldListArray[i]?.TrimToArray();
        }

        return (exclusiveSenseFieldValuesArray, senseFieldValuesSharedByAllSenses.TrimToArray());
    }

    private static (string[]?[]? exclusiveSenseFieldValues, string[]? senseFieldValuesSharedByAllSenses) GetExclusiveAndSharedValuesForNullableSenseField(List<string[]?> senseField)
    {
        if (senseField.Count is 0)
        {
            return (null, null);
        }

        if (senseField.Count is 1)
        {
            return (null, senseField[0]);
        }

        ReadOnlySpan<string[]?> senseFieldSpan = senseField.AsReadOnlySpan();
        List<string>?[] exclusiveSenseFieldListArray = new List<string>?[senseFieldSpan.Length];
        List<string> senseFieldValuesSharedByAllSenses = [];

        for (int i = 0; i < senseFieldSpan.Length; i++)
        {
            ref readonly string[]? senseFieldValue = ref senseFieldSpan[i];
            if (senseFieldValue is not null)
            {
                foreach (string value in senseFieldValue)
                {
                    bool containsAll = true;
                    foreach (ref readonly string[]? senseItem in senseFieldSpan)
                    {
                        if (!senseItem?.AsSpan().Contains(value) ?? true)
                        {
                            exclusiveSenseFieldListArray[i] ??= [];

                            List<string>? exclusiveSenseFieldList = exclusiveSenseFieldListArray[i];
                            Debug.Assert(exclusiveSenseFieldList is not null);

                            exclusiveSenseFieldList.Add(value);
                            containsAll = false;
                            break;
                        }
                    }

                    if (containsAll && !senseFieldValuesSharedByAllSenses.AsReadOnlySpan().Contains(value))
                    {
                        senseFieldValuesSharedByAllSenses.Add(value);
                    }
                }
            }
        }

        if (senseFieldValuesSharedByAllSenses.Count is 0)
        {
            return (senseField.TrimListOfNullableElementsToArray(), null);
        }

        bool allElementsAreNull = true;
        foreach (List<string>? item in exclusiveSenseFieldListArray)
        {
            if (item is not null)
            {
                allElementsAreNull = false;
                break;
            }
        }

        if (allElementsAreNull)
        {
            return (null, senseFieldValuesSharedByAllSenses.TrimToArray());
        }

        string[]?[] exclusiveSenseFieldValuesArray = new string[exclusiveSenseFieldListArray.Length][];
        for (int i = 0; i < exclusiveSenseFieldListArray.Length; i++)
        {
            exclusiveSenseFieldValuesArray[i] = exclusiveSenseFieldListArray[i]?.TrimToArray();
        }

        return (exclusiveSenseFieldValuesArray, senseFieldValuesSharedByAllSenses.TrimToArray());
    }
}
