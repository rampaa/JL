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

        ReadOnlySpan<Sense> senseSpan = entry.SenseList.AsReadOnlySpan();
        string[]?[] stagKArraysInHiragana;
        string[]?[] stagRArraysInHiragana;
        bool hasStagK = false;
        bool hasStagR = false;
        if (senseSpan.Length is 1)
        {
            Debug.Assert(senseSpan[0].StagKArray is null && senseSpan[0].StagRArray is null);
            stagKArraysInHiragana = [];
            stagRArraysInHiragana = [];
        }
        else
        {
            stagKArraysInHiragana = new string[]?[senseSpan.Length];
            stagRArraysInHiragana = new string[]?[senseSpan.Length];
            for (int i = 0; i < senseSpan.Length; i++)
            {
                Sense sense = senseSpan[i];
                if (sense.StagKArray is not null)
                {
                    hasStagK = true;
                    string[] stagKArrayInHiragana = new string[sense.StagKArray.Length];
                    stagKArraysInHiragana[i] = stagKArrayInHiragana;
                    for (int j = 0; j < sense.StagKArray.Length; j++)
                    {
                        stagKArrayInHiragana[j] = JapaneseUtils.NormalizeText(sense.StagKArray[j]);
                    }
                }

                if (sense.StagRArray is not null)
                {
                    hasStagR = true;
                    string[] stagRArrayInHiragana = new string[sense.StagRArray.Length];
                    stagRArraysInHiragana[i] = stagRArrayInHiragana;
                    for (int j = 0; j < sense.StagRArray.Length; j++)
                    {
                        stagRArrayInHiragana[j] = JapaneseUtils.NormalizeText(sense.StagRArray[j]);
                    }
                }
            }
        }

        string? firstPrimarySpelling;
        string[]? alternativeSpellingsForFirstPrimarySpelling;
        if (spellingsWithoutSearchOnlyFormsExist)
        {
            string[] allSpellingsWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length];
            string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms = new string[kanjiElementsWithoutSearchOnlyForms.Length][];

            for (int i = 0; i < kanjiElementsWithoutSearchOnlyForms.Length; i++)
            {
                ref readonly KanjiElement kanjiElement = ref kanjiElementsWithoutSearchOnlyForms[i];
                allSpellingsWithoutSearchOnlyForms[i] = kanjiElement.Keb;
                allKanjiOrthographyInfoWithoutSearchOnlyForms[i] = kanjiElement.KeInfArray;
            }

            firstPrimarySpelling = allSpellingsWithoutSearchOnlyForms[0];
            alternativeSpellingsForFirstPrimarySpelling = allSpellingsWithoutSearchOnlyForms.RemoveAt(0);
            ProcessKanjiElements(in entry, recordDictionary, allSpellingsWithoutSearchOnlyForms, allKanjiOrthographyInfoWithoutSearchOnlyForms, stagKArraysInHiragana, stagRArraysInHiragana, firstPrimarySpelling, hasStagR);
        }
        else
        {
            firstPrimarySpelling = null;
            alternativeSpellingsForFirstPrimarySpelling = null;
        }

        ProcessReadingElements(in entry, recordDictionary, firstPrimarySpelling, alternativeSpellingsForFirstPrimarySpelling, stagKArraysInHiragana, stagRArraysInHiragana, spellingsWithoutSearchOnlyFormsExist, hasStagK);
        return recordDictionary;
    }

    private static void ProcessKanjiElements(in JmdictEntry entry, Dictionary<string, JmdictRecord> recordDictionary, string[] allSpellingsWithoutSearchOnlyForms, string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms, string[]?[] stagKArraysInHiragana, string[]?[] stagRArraysInHiragana, string firstPrimarySpelling, bool hasStagR)
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

                Debug.Assert(recordForFirstPrimarySpellingInHiragana is not null);
                continue;
            }

            List<string> readingList = new(readingElementsLength);
            List<string>? readingListInHiragana = hasStagR ? new List<string>(readingElementsLength) : null;
            List<string[]?> readingsOrthographyInfoList = new(readingElementsLength);

            foreach (ref readonly ReadingElement readingElement in readingElementsSpan)
            {
                if (readingElement.ReInfArray is null || !readingElement.ReInfArray.Contains("sk"))
                {
                    ReadOnlySpan<string> reRestrListSpan = readingElement.ReRestrList.AsReadOnlySpan();
                    if (reRestrListSpan.Length is 0 || reRestrListSpan.Contains(kanjiElement.Keb))
                    {
                        readingList.Add(readingElement.Reb);
                        readingListInHiragana?.Add(JapaneseUtils.NormalizeText(readingElement.Reb));
                        readingsOrthographyInfoList.Add(readingElement.ReInfArray);
                    }
                }
            }

            if (senseListSpanLength is 1)
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

            ReadOnlySpan<string> readingListInHiraganaSpan = readingListInHiragana.AsReadOnlySpan();
            for (int i = 0; i < senseListSpan.Length; i++)
            {
                Sense sense = senseListSpan[i];
                string[]? stagKArrayInHiragana = stagKArraysInHiragana[i];
                string[]? stagRArrayInHiragana = stagRArraysInHiragana[i];

                if ((stagKArrayInHiragana is null && stagRArrayInHiragana is null)
                    || (stagKArrayInHiragana is not null && stagKArrayInHiragana.Contains(key))
                    || (stagRArrayInHiragana is not null && stagRArrayInHiragana.ContainsAny(readingListInHiraganaSpan)))
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
    }

    private static void ProcessReadingElements(in JmdictEntry entry, Dictionary<string, JmdictRecord> recordDictionary, string? firstPrimarySpelling, string[]? alternativeSpellingsForFirstPrimarySpelling, string[]?[] stagKArraysInHiragana, string[]?[] stagRArraysInHiragana, bool spellingsWithoutSearchOnlyFormsExist, bool hasStagK)
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

                Debug.Assert(recordForFirstReadingInHiragana is not null);
                continue;
            }

            string primarySpelling;
            string[]? primarySpellingOrthographyInfo = null;
            string[]? readings = null;
            string[]?[]? readingsOrthographyInfo = null;
            string[]? alternativeSpellings;
            string[]?[]? alternativeSpellingsOrthographyInfo = null;
            string? normalizedPrimarySpelling = null;

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

                normalizedPrimarySpelling = JapaneseUtils.NormalizeText(primarySpelling);
                if (recordDictionary.TryGetValue(normalizedPrimarySpelling, out JmdictRecord? mainEntry))
                {
                    readings = mainEntry.Readings;
                    primarySpellingOrthographyInfo = mainEntry.PrimarySpellingOrthographyInfo;
                    alternativeSpellingsOrthographyInfo = mainEntry.AlternativeSpellingsOrthographyInfo;
                    readingsOrthographyInfo = mainEntry.ReadingsOrthographyInfo;
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

            if (senseListSpanLength is 1)
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
                ++index;

                if (i is 0 && spellingsWithoutSearchOnlyFormsExist)
                {
                    foreach (ref readonly KanjiElement kanjiElement in kanjiElementsSpan)
                    {
                        _ = recordDictionary.TryAdd(JapaneseUtils.NormalizeText(kanjiElement.Keb), singleSenseRecord);
                    }
                }

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

            string[]? alternativeSpellingsInHiragana;
            if (hasStagK && alternativeSpellings is not null)
            {
                alternativeSpellingsInHiragana = new string[alternativeSpellings.Length];
                for (int j = 0; j < alternativeSpellings.Length; j++)
                {
                    alternativeSpellingsInHiragana[j] = JapaneseUtils.NormalizeText(alternativeSpellings[j]);
                }
            }
            else
            {
                alternativeSpellingsInHiragana = null;
            }

            string primarySpellingInHiragana = "";
            if (hasStagK)
            {
                primarySpellingInHiragana = normalizedPrimarySpelling ?? JapaneseUtils.NormalizeText(primarySpelling);
            }

            for (int j = 0; j < senseListSpan.Length; j++)
            {
                Sense sense = senseListSpan[j];
                string[]? stagKArrayInHiragana = stagKArraysInHiragana[j];
                string[]? stagRArrayInHiragana = stagRArraysInHiragana[j];

                if ((stagKArrayInHiragana is null && stagRArrayInHiragana is null)
                    || (stagRArrayInHiragana is not null && stagRArrayInHiragana.Contains(key))
                    || (stagKArrayInHiragana is not null
                        && (stagKArrayInHiragana.Contains(primarySpellingInHiragana)
                            || (alternativeSpellingsInHiragana is not null && stagKArrayInHiragana.ContainsAny(alternativeSpellingsInHiragana)))))
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

            ++index;

            if (i is 0 && spellingsWithoutSearchOnlyFormsExist)
            {
                foreach (ref readonly KanjiElement kanjiElement in kanjiElementsSpan)
                {
                    _ = recordDictionary.TryAdd(JapaneseUtils.NormalizeText(kanjiElement.Keb), record);
                }
            }
        }
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
