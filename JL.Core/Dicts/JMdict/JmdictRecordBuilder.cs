using System.Diagnostics;
using JL.Core.Japanese;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictRecordBuilder
{
    public static Dictionary<string, JmdictRecord>? GetRecordsFromEntry(in JmdictEntry entry, bool includeProperNames)
    {
        // https://github.com/JMdictProject/JMdictIssues/issues/94#issuecomment-1535592300
        if (!includeProperNames && entry.Id is >= 5000000 and <= 5999999)
        {
            return null;
        }

        ReadOnlySpan<KanjiElement> kanjiElementsSpan = entry.KanjiElements.AsReadOnlySpan();
        ReadOnlySpan<ReadingElement> readingElementsSpan = entry.ReadingElements.AsReadOnlySpan();
        Dictionary<string, JmdictRecord> recordDictionary = new(kanjiElementsSpan.Length + readingElementsSpan.Length, StringComparer.Ordinal);

        List<KanjiElement>? kanjiElementsWithoutSearchOnlyFormsList = null;
        for (int i = 0; i < kanjiElementsSpan.Length; i++)
        {
            ref readonly KanjiElement kanjiElement = ref kanjiElementsSpan[i];
            if (kanjiElement.KeInfArray is not null && kanjiElement.KeInfArray.Contains("sK"))
            {
                if (kanjiElementsWithoutSearchOnlyFormsList is null)
                {
                    kanjiElementsWithoutSearchOnlyFormsList = new List<KanjiElement>(kanjiElementsSpan.Length - 1);
                    for (int j = 0; j < i; j++)
                    {
                        kanjiElementsWithoutSearchOnlyFormsList.Add(kanjiElementsSpan[j]);
                    }
                }
            }
            else
            {
                kanjiElementsWithoutSearchOnlyFormsList?.Add(kanjiElement);
            }
        }

        ReadOnlySpan<KanjiElement> kanjiElementsWithoutSearchOnlyForms = kanjiElementsWithoutSearchOnlyFormsList is not null
            ? kanjiElementsWithoutSearchOnlyFormsList.AsReadOnlySpan()
            : kanjiElementsSpan;
        bool spellingsWithoutSearchOnlyFormsExist = kanjiElementsWithoutSearchOnlyForms.Length > 0;

        string[]?[]? allKanjiOrthographyInfoWithoutSearchOnlyForms = null;
        string? firstPrimarySpelling;
        string[]? alternativeSpellingsForFirstPrimarySpelling;
        JmdictRecord? displacedPrimarySpellingRecord;
        if (spellingsWithoutSearchOnlyFormsExist)
        {
            string[] allSpellingsWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length];
            allKanjiOrthographyInfoWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length][];

            for (int i = 0; i < kanjiElementsWithoutSearchOnlyForms.Length; i++)
            {
                ref readonly KanjiElement kanjiElement = ref kanjiElementsWithoutSearchOnlyForms[i];
                allSpellingsWithoutSearchOnlyForms[i] = kanjiElement.Keb;
                allKanjiOrthographyInfoWithoutSearchOnlyForms[i] = kanjiElement.KeInfArray;
            }

            firstPrimarySpelling = allSpellingsWithoutSearchOnlyForms[0];
            alternativeSpellingsForFirstPrimarySpelling = allSpellingsWithoutSearchOnlyForms.RemoveAt(0);
            displacedPrimarySpellingRecord = ProcessKanjiElements(in entry, recordDictionary, allSpellingsWithoutSearchOnlyForms, allKanjiOrthographyInfoWithoutSearchOnlyForms, firstPrimarySpelling);
        }
        else
        {
            firstPrimarySpelling = null;
            alternativeSpellingsForFirstPrimarySpelling = null;
            displacedPrimarySpellingRecord = null;
        }

        ProcessReadingElements(in entry, recordDictionary, firstPrimarySpelling, alternativeSpellingsForFirstPrimarySpelling, spellingsWithoutSearchOnlyFormsExist, displacedPrimarySpellingRecord, allKanjiOrthographyInfoWithoutSearchOnlyForms);
        return recordDictionary;
    }

    private static JmdictRecord? ProcessKanjiElements(in JmdictEntry entry, Dictionary<string, JmdictRecord> recordDictionary, string[] allSpellingsWithoutSearchOnlyForms, string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms, string firstPrimarySpelling)
    {
        int index = 0;
        ReadOnlySpan<KanjiElement> kanjiElementsSpan = entry.KanjiElements.AsReadOnlySpan();
        ReadOnlySpan<ReadingElement> readingElementsSpan = entry.ReadingElements.AsReadOnlySpan();
        ReadOnlySpan<Sense> senseListSpan = entry.SenseList.AsReadOnlySpan();
        int readingElementsLength = readingElementsSpan.Length;
        int senseListSpanLength = senseListSpan.Length;
        Debug.Assert(senseListSpanLength > 0);

        string? firstPrimarySpellingInHiragana = null;

        JmdictRecord? recordForFirstPrimarySpellingInHiragana = null;

        foreach (ref readonly KanjiElement kanjiElement in kanjiElementsSpan)
        {
            string key = JapaneseUtils.NormalizeText(kanjiElement.Keb).GetPooledString();
            if (recordDictionary.ContainsKey(key))
            {
                if (kanjiElement.KeInfArray is null || !kanjiElement.KeInfArray.Contains("sK"))
                {
                    ++index;
                }

                continue;
            }

            if (kanjiElement.KeInfArray is not null && kanjiElement.KeInfArray.Contains("sK"))
            {
                firstPrimarySpellingInHiragana ??= JapaneseUtils.NormalizeText(firstPrimarySpelling);
                if (JapaneseUtils.NormalizeLongVowelMark(key).AsReadOnlySpan().Contains(firstPrimarySpellingInHiragana))
                {
                    continue;
                }

                if (JapaneseUtils.NormalizeLongVowelMark(firstPrimarySpellingInHiragana).AsReadOnlySpan().Contains(key))
                {
                    if (recordForFirstPrimarySpellingInHiragana is not null)
                    {
                        _ = recordDictionary.Remove(firstPrimarySpellingInHiragana);
                        recordDictionary.Add(key, recordForFirstPrimarySpellingInHiragana);
                    }
                    else if (recordDictionary.Remove(firstPrimarySpellingInHiragana, out recordForFirstPrimarySpellingInHiragana))
                    {
                        recordDictionary.Add(key, recordForFirstPrimarySpellingInHiragana);
                    }
                }
                else if (recordForFirstPrimarySpellingInHiragana is not null
                    || recordDictionary.TryGetValue(firstPrimarySpellingInHiragana, out recordForFirstPrimarySpellingInHiragana))
                {
                    recordDictionary.Add(key, recordForFirstPrimarySpellingInHiragana);
                }

                continue;
            }

            List<string> readingList = new(readingElementsLength);
            List<string[]?> readingsOrthographyInfoList = new(readingElementsLength);

            foreach (ref readonly ReadingElement readingElement in readingElementsSpan)
            {
                if (readingElement.ReInfArray is null || !readingElement.ReInfArray.Contains("sk"))
                {
                    ReadOnlySpan<string> reRestrListSpan = readingElement.ReRestrList.AsReadOnlySpan();
                    if (reRestrListSpan.Length is 0 || reRestrListSpan.Contains(kanjiElement.Keb))
                    {
                        readingList.Add(readingElement.Reb);
                        readingsOrthographyInfoList.Add(readingElement.ReInfArray);
                    }
                }
            }

            if (senseListSpanLength is 1 && senseListSpan[0] is { StagKArray: null, StagRArray: null })
            {
                Sense sense = senseListSpan[0];
                JmdictRecord singleSenseRecord = new(entry.Id,
                    kanjiElement.Keb,
                    [sense.GlossArray],
                    null,
                    sense.PosArray,
                    allKanjiOrthographyInfoWithoutSearchOnlyForms[index],
                    allSpellingsWithoutSearchOnlyForms.RemoveAt(index),
                    allKanjiOrthographyInfoWithoutSearchOnlyForms.RemoveAtNullable(index),
                    readingList.TrimToArray(),
                    readingsOrthographyInfoList.TrimListOfNullableElementsToArray(),
                    null,
                    null,
                    null,
                    sense.FieldArray,
                    null,
                    sense.MiscArray,
                    sense.SInf is not null ? [sense.SInf] : null,
                    null,
                    sense.DialArray,
                    entry.LSourceArray,
                    sense.XRefArray is not null ? [sense.XRefArray] : null,
                    entry.Info);

                recordDictionary.Add(key, singleSenseRecord);
                ++index;
                continue;
            }

            List<string[]> definitionList = new(senseListSpanLength);
            List<string[]?> wordClassList = new(senseListSpanLength);
            List<string[]?> readingRestrictionList = new(senseListSpanLength);
            List<string[]?> spellingRestrictionList = new(senseListSpanLength);
            List<string[]?> fieldList = new(senseListSpanLength);
            List<string[]?> miscList = new(senseListSpanLength);
            List<string[]?> dialectList = new(senseListSpanLength);
            List<string?> definitionInfoList = new(senseListSpanLength);
            List<string[]?> crossReferencesList = new(senseListSpanLength);

            foreach (Sense sense in senseListSpan)
            {
                if ((sense.StagKArray is null && sense.StagRArray is null)
                    || SenseAppliesToSpelling(sense, key, kanjiElementsSpan, readingElementsSpan)
                    || SenseAppliesToReading(sense, key, kanjiElementsSpan, readingElementsSpan))
                {
                    definitionList.Add(sense.GlossArray);
                    wordClassList.Add(sense.PosArray);
                    readingRestrictionList.Add(sense.StagRArray);
                    spellingRestrictionList.Add(sense.StagKArray);
                    fieldList.Add(sense.FieldArray);
                    miscList.Add(sense.MiscArray);
                    dialectList.Add(sense.DialArray);
                    definitionInfoList.Add(sense.SInf);
                    crossReferencesList.Add(sense.XRefArray);
                }
            }

            Debug.Assert(definitionList.Count > 0);

            (string[]?[]? exclusiveWordClasses, string[]? wordClassesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(wordClassList);
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
                allKanjiOrthographyInfoWithoutSearchOnlyForms.RemoveAtNullable(index),
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
                entry.LSourceArray,
                crossReferencesList.TrimListOfNullableElementsToArray(),
                entry.Info);

            recordDictionary.Add(key, record);

            ++index;
        }

        return recordForFirstPrimarySpellingInHiragana;
    }

    private static void ProcessReadingElements(in JmdictEntry entry, Dictionary<string, JmdictRecord> recordDictionary, string? firstPrimarySpelling, string[]? alternativeSpellingsForFirstPrimarySpelling, bool spellingsWithoutSearchOnlyFormsExist, JmdictRecord? displacedPrimarySpellingRecord, string[]?[]? allKanjiOrthographyInfoWithoutSearchOnlyForms)
    {
        ReadOnlySpan<ReadingElement> readingElementsSpan = entry.ReadingElements.AsReadOnlySpan();
        List<ReadingElement>? readingElementsWithoutSearchOnlyFormsList = null;
        for (int i = 0; i < readingElementsSpan.Length; i++)
        {
            ref readonly ReadingElement readingElement = ref readingElementsSpan[i];
            if (readingElement.ReInfArray is not null && readingElement.ReInfArray.Contains("sk"))
            {
                if (readingElementsWithoutSearchOnlyFormsList is null)
                {
                    readingElementsWithoutSearchOnlyFormsList = new List<ReadingElement>(readingElementsSpan.Length - 1);
                    for (int j = 0; j < i; j++)
                    {
                        readingElementsWithoutSearchOnlyFormsList.Add(readingElementsSpan[j]);
                    }
                }
            }
            else
            {
                readingElementsWithoutSearchOnlyFormsList?.Add(readingElement);
            }
        }

        ReadOnlySpan<ReadingElement> readingElementsWithoutSearchOnlyForms = readingElementsWithoutSearchOnlyFormsList is not null
            ? readingElementsWithoutSearchOnlyFormsList.AsReadOnlySpan()
            : readingElementsSpan;
        Debug.Assert(readingElementsWithoutSearchOnlyForms.Length > 0);

        string[]? allReadingsWithoutSearchOnlyForms = null;
        string[]?[]? allROrthographyInfoWithoutSearchOnlyForms = null;
        if (!spellingsWithoutSearchOnlyFormsExist)
        {
            allReadingsWithoutSearchOnlyForms = new string[readingElementsWithoutSearchOnlyForms.Length];
            allROrthographyInfoWithoutSearchOnlyForms = new string[readingElementsWithoutSearchOnlyForms.Length][];
            for (int i = 0; i < readingElementsWithoutSearchOnlyForms.Length; i++)
            {
                ref readonly ReadingElement readingElement = ref readingElementsWithoutSearchOnlyForms[i];
                allReadingsWithoutSearchOnlyForms[i] = readingElement.Reb;
                allROrthographyInfoWithoutSearchOnlyForms[i] = readingElement.ReInfArray;
            }
        }

        string? firstReadingInHiragana = null;
        JmdictRecord? recordForFirstReadingInHiragana = null;

        int index = 0;
        ReadOnlySpan<Sense> senseListSpan = entry.SenseList.AsReadOnlySpan();
        ReadOnlySpan<KanjiElement> kanjiElementsSpan = entry.KanjiElements.AsReadOnlySpan();
        int senseListSpanLength = senseListSpan.Length;
        Debug.Assert(senseListSpanLength > 0);

        for (int i = 0; i < readingElementsSpan.Length; i++)
        {
            ref readonly ReadingElement readingElement = ref readingElementsSpan[i];

            string key = JapaneseUtils.NormalizeText(readingElement.Reb).GetPooledString();
            if (recordDictionary.ContainsKey(key))
            {
                if (readingElement.ReInfArray is null || !readingElement.ReInfArray.Contains("sk"))
                {
                    ++index;
                }

                continue;
            }

            if (readingElement.ReInfArray is not null && readingElement.ReInfArray.Contains("sk"))
            {
                firstReadingInHiragana ??= JapaneseUtils.NormalizeText(readingElementsWithoutSearchOnlyForms[0].Reb);
                if (JapaneseUtils.NormalizeLongVowelMark(key).AsReadOnlySpan().Contains(firstReadingInHiragana))
                {
                    continue;
                }

                if (JapaneseUtils.NormalizeLongVowelMark(firstReadingInHiragana).AsReadOnlySpan().Contains(key))
                {
                    if (recordForFirstReadingInHiragana is not null)
                    {
                        _ = recordDictionary.Remove(firstReadingInHiragana);
                        recordDictionary.Add(key, recordForFirstReadingInHiragana);
                    }
                    else if (recordDictionary.Remove(firstReadingInHiragana, out recordForFirstReadingInHiragana))
                    {
                        recordDictionary.Add(key, recordForFirstReadingInHiragana);
                    }
                }
                else if (recordForFirstReadingInHiragana is not null
                    || recordDictionary.TryGetValue(firstReadingInHiragana, out recordForFirstReadingInHiragana))
                {
                    recordDictionary.Add(key, recordForFirstReadingInHiragana);
                }

                continue;
            }

            string primarySpelling;
            string[]? primarySpellingOrthographyInfo = null;
            string[]? readings = null;
            string[]?[]? readingsOrthographyInfo = null;
            string[]? alternativeSpellings;
            string[]?[]? alternativeSpellingsOrthographyInfo = null;

            if (readingElement.ReRestrList is not null || spellingsWithoutSearchOnlyFormsExist)
            {
                if (readingElement.ReRestrList is not null)
                {
                    Debug.Assert(readingElement.ReRestrList.Count > 0);
                    primarySpelling = readingElement.ReRestrList[0];
                    alternativeSpellings = readingElement.ReRestrList.RemoveAtToArray(0);
                }
                else
                {
                    Debug.Assert(firstPrimarySpelling is not null);
                    primarySpelling = firstPrimarySpelling;
                    alternativeSpellings = alternativeSpellingsForFirstPrimarySpelling;
                }

                JmdictRecord? mainEntry = displacedPrimarySpellingRecord;
                if (mainEntry is null || mainEntry.PrimarySpelling != primarySpelling)
                {
                    _ = recordDictionary.TryGetValue(JapaneseUtils.NormalizeText(primarySpelling), out mainEntry);
                }

                if (mainEntry is not null)
                {
                    readings = mainEntry.Readings;
                    readingsOrthographyInfo = mainEntry.ReadingsOrthographyInfo;
                    if (readingElement.ReRestrList is null)
                    {
                        primarySpellingOrthographyInfo = mainEntry.PrimarySpellingOrthographyInfo;
                        alternativeSpellingsOrthographyInfo = mainEntry.AlternativeSpellingsOrthographyInfo;
                    }
                }
                else
                {
                    List<string> readingList = [];
                    List<string[]?> readingInfoList = [];
                    foreach (ref readonly ReadingElement candidateReading in readingElementsSpan)
                    {
                        if ((candidateReading.ReInfArray is null || !candidateReading.ReInfArray.Contains("sk"))
                            && (candidateReading.ReRestrList is null || candidateReading.ReRestrList.Contains(primarySpelling)))
                        {
                            readingList.Add(candidateReading.Reb);
                            readingInfoList.Add(candidateReading.ReInfArray);
                        }
                    }

                    readings = readingList.TrimToArray();
                    readingsOrthographyInfo = readingInfoList.TrimListOfNullableElementsToArray();

                    if (readingElement.ReRestrList is null)
                    {
                        Debug.Assert(allKanjiOrthographyInfoWithoutSearchOnlyForms is not null);
                        primarySpellingOrthographyInfo = allKanjiOrthographyInfoWithoutSearchOnlyForms[0];
                        alternativeSpellingsOrthographyInfo = allKanjiOrthographyInfoWithoutSearchOnlyForms.RemoveAtNullable(0);
                    }
                }

                if (readingElement.ReRestrList is not null)
                {
                    foreach (ref readonly KanjiElement spelling in kanjiElementsSpan)
                    {
                        if (spelling.KeInfArray is null)
                        {
                            continue;
                        }

                        if (spelling.Keb == primarySpelling)
                        {
                            primarySpellingOrthographyInfo = spelling.KeInfArray;
                        }
                        else if (alternativeSpellings is not null)
                        {
                            int spellingIndex = Array.IndexOf(alternativeSpellings, spelling.Keb);
                            if (spellingIndex >= 0)
                            {
                                alternativeSpellingsOrthographyInfo ??= new string[alternativeSpellings.Length][];
                                alternativeSpellingsOrthographyInfo[spellingIndex] = spelling.KeInfArray;
                            }
                        }
                    }
                }
            }

            else
            {
                Debug.Assert(allReadingsWithoutSearchOnlyForms is not null);
                Debug.Assert(allROrthographyInfoWithoutSearchOnlyForms is not null);
                primarySpelling = readingElement.Reb;
                primarySpellingOrthographyInfo = allROrthographyInfoWithoutSearchOnlyForms[index];

                alternativeSpellings = allReadingsWithoutSearchOnlyForms.RemoveAt(index);
                alternativeSpellingsOrthographyInfo = allROrthographyInfoWithoutSearchOnlyForms.RemoveAtNullable(index);
            }

            if (senseListSpanLength is 1 && senseListSpan[0] is { StagKArray: null, StagRArray: null })
            {
                Sense sense = senseListSpan[0];
                JmdictRecord singleSenseRecord = new(entry.Id,
                    primarySpelling,
                    [sense.GlossArray],
                    null,
                    sense.PosArray,
                    primarySpellingOrthographyInfo,
                    alternativeSpellings,
                    alternativeSpellingsOrthographyInfo,
                    readings,
                    readingsOrthographyInfo,
                    null,
                    null,
                    null,
                    sense.FieldArray,
                    null,
                    sense.MiscArray,
                    sense.SInf is not null ? [sense.SInf] : null,
                    null,
                    sense.DialArray,
                    entry.LSourceArray,
                    sense.XRefArray is not null ? [sense.XRefArray] : null,
                    entry.Info);

                recordDictionary.Add(key, singleSenseRecord);
                if (i is 0)
                {
                    RestoreSpellingSearchKeys(recordDictionary, kanjiElementsSpan, firstPrimarySpelling, displacedPrimarySpellingRecord);
                }

                ++index;

                continue;
            }

            List<string[]> definitionList = new(senseListSpanLength);
            List<string[]?> wordClassList = new(senseListSpanLength);
            List<string[]?> readingRestrictionList = new(senseListSpanLength);
            List<string[]?> spellingRestrictionList = new(senseListSpanLength);
            List<string[]?> fieldList = new(senseListSpanLength);
            List<string[]?> miscList = new(senseListSpanLength);
            List<string[]?> dialectList = new(senseListSpanLength);
            List<string?> definitionInfoList = new(senseListSpanLength);
            List<string[]?> crossReferencesList = new(senseListSpanLength);

            foreach (Sense sense in senseListSpan)
            {
                if ((sense.StagKArray is null && sense.StagRArray is null)
                    || SenseAppliesToReading(sense, key, kanjiElementsSpan, readingElementsSpan))
                {
                    definitionList.Add(sense.GlossArray);
                    wordClassList.Add(sense.PosArray);
                    readingRestrictionList.Add(sense.StagRArray);
                    spellingRestrictionList.Add(sense.StagKArray);
                    fieldList.Add(sense.FieldArray);
                    miscList.Add(sense.MiscArray);
                    dialectList.Add(sense.DialArray);
                    definitionInfoList.Add(sense.SInf);
                    crossReferencesList.Add(sense.XRefArray);
                }
            }

            Debug.Assert(definitionList.Count > 0);

            (string[]?[]? exclusiveWordClasses, string[]? wordClassesSharedByAllSenses) = GetExclusiveAndSharedValuesForNullableSenseField(wordClassList);
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
                entry.LSourceArray,
                crossReferencesList.TrimListOfNullableElementsToArray(),
                entry.Info);

            // record.Priorities = kanjiElement.KePriList

            recordDictionary.Add(key, record);
            if (i is 0)
            {
                RestoreSpellingSearchKeys(recordDictionary, kanjiElementsSpan, firstPrimarySpelling, displacedPrimarySpellingRecord);
            }

            ++index;
        }
    }

    private static void RestoreSpellingSearchKeys(Dictionary<string, JmdictRecord> records, ReadOnlySpan<KanjiElement> spellings, string? firstPrimarySpelling, JmdictRecord? displacedRecord)
    {
        if (firstPrimarySpelling is null)
        {
            return;
        }

        JmdictRecord? record = displacedRecord;
        if (record is null && !records.TryGetValue(JapaneseUtils.NormalizeText(firstPrimarySpelling), out record))
        {
            return;
        }

        // Long-vowel handling can skip or replace these keys. Restore their filtered spelling record,
        // without reintroducing ordinary spellings excluded by their sense restrictions.
        foreach (ref readonly KanjiElement spelling in spellings)
        {
            if (spelling.Keb == firstPrimarySpelling
                || (spelling.KeInfArray is not null && spelling.KeInfArray.Contains("sK")))
            {
                _ = records.TryAdd(JapaneseUtils.NormalizeText(spelling.Keb), record);
            }
        }
    }

    private static bool SenseAppliesToSpelling(Sense sense, string key, ReadOnlySpan<KanjiElement> spellings, ReadOnlySpan<ReadingElement> readings)
    {
        foreach (ref readonly KanjiElement spelling in spellings)
        {
            if ((spelling.KeInfArray is not null && spelling.KeInfArray.Contains("sK"))
                || (sense.StagKArray is not null && !sense.StagKArray.Contains(spelling.Keb))
                || JapaneseUtils.NormalizeText(spelling.Keb) != key)
            {
                continue;
            }

            if (sense.StagRArray is null)
            {
                return true;
            }

            foreach (ref readonly ReadingElement reading in readings)
            {
                if ((reading.ReInfArray is null || !reading.ReInfArray.Contains("sk"))
                    && sense.StagRArray.Contains(reading.Reb)
                    && (reading.ReRestrList is null || reading.ReRestrList.Contains(spelling.Keb)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SenseAppliesToReading(Sense sense, string key, ReadOnlySpan<KanjiElement> spellings, ReadOnlySpan<ReadingElement> readings)
    {
        foreach (ref readonly ReadingElement reading in readings)
        {
            if ((reading.ReInfArray is not null && reading.ReInfArray.Contains("sk"))
                || (sense.StagRArray is not null && !sense.StagRArray.Contains(reading.Reb))
                || JapaneseUtils.NormalizeText(reading.Reb) != key)
            {
                continue;
            }

            if (sense.StagKArray is null)
            {
                return true;
            }

            foreach (ref readonly KanjiElement spelling in spellings)
            {
                if ((spelling.KeInfArray is null || !spelling.KeInfArray.Contains("sK"))
                    && sense.StagKArray.Contains(spelling.Keb)
                    && (reading.ReRestrList is null || reading.ReRestrList.Contains(spelling.Keb)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (string[]?[]? exclusiveSenseFieldValues, string[]? senseFieldValuesSharedByAllSenses) GetExclusiveAndSharedValuesForNullableSenseField(List<string[]?> senseField)
    {
        int senseCount = senseField.Count;
        if (senseCount is 0)
        {
            return (null, null);
        }

        if (senseCount is 1)
        {
            return (null, senseField[0]);
        }

        ReadOnlySpan<string[]?> senseFieldSpan = senseField.AsReadOnlySpan();
        foreach (ref readonly string[]? senses in senseFieldSpan)
        {
            if (senses is null)
            {
                return (senseField.TrimListOfNullableElementsToArray(), null);
            }
        }

        string[]? firstSensesArray = senseFieldSpan[0];
        Debug.Assert(firstSensesArray is not null);
        List<string> sharedSenseCandidates = firstSensesArray.ToList();
        return GetExclusiveAndSharedValuesForSenseField(
            // ReSharper disable once NullableWarningSuppressionIsUsed
            senseField!,
            sharedSenseCandidates);
    }

    private static (string[]?[]? exclusiveSenseFieldValues, string[]? senseFieldValuesSharedByAllSenses) GetExclusiveAndSharedValuesForSenseField(List<string[]> senseField, List<string> sharedSenseCandidates)
    {
        ReadOnlySpan<string[]> senseFieldSpan = senseField.AsReadOnlySpan();
        for (int i = 1; i < senseFieldSpan.Length; i++)
        {
            ReadOnlySpan<string> sensesSpan = senseFieldSpan[i];
            for (int j = sharedSenseCandidates.Count - 1; j >= 0; j--)
            {
                string sharedSenseCandidate = sharedSenseCandidates[j];
                if (!sensesSpan.Contains(sharedSenseCandidate))
                {
                    if (sharedSenseCandidates.Count is 1)
                    {
                        return (senseField.ToArray(), null);
                    }

                    sharedSenseCandidates.RemoveAt(j);
                }
            }
        }

        ReadOnlySpan<string> sharedSenseCandidatesSpan = sharedSenseCandidates.AsReadOnlySpan();
        string[]?[]? exclusiveSenseFieldValues = null;
        for (int i = 0; i < senseFieldSpan.Length; i++)
        {
            ReadOnlySpan<string> senseSpan = senseFieldSpan[i];
            List<string>? currentExclusiveList = null;
            foreach (string sense in senseSpan)
            {
                if (!sharedSenseCandidatesSpan.Contains(sense))
                {
                    currentExclusiveList ??= [];
                    currentExclusiveList.Add(sense);
                }
            }

            if (currentExclusiveList is not null)
            {
                exclusiveSenseFieldValues ??= new string[senseFieldSpan.Length][];
                exclusiveSenseFieldValues[i] = currentExclusiveList.ToArray();
            }
        }

        return (exclusiveSenseFieldValues, sharedSenseCandidates.ToArray());
    }
}
