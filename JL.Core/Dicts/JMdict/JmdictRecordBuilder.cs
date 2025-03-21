using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictRecordBuilder
{
    public static void AddToDictionary(in JmdictEntry entry, IDictionary<string, IList<IDictRecord>> jmdictDictionary)
    {
        List<KanjiElement> kanjiElementsWithoutSearchOnlyForms = entry.KanjiElements.Where(static ke => !ke.KeInfList.Contains("sK")).ToList();
        string[] allSpellingsWithoutSearchOnlyForms = kanjiElementsWithoutSearchOnlyForms.Select(static ke => ke.Keb).ToArray();
        string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms = kanjiElementsWithoutSearchOnlyForms.Select(static ke => ke.KeInfList.TrimToArray()).ToArray();

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
        int kanjiElementCount = entry.KanjiElements.Count;
        int readingElementCount = entry.ReadingElements.Count;
        int senseListCount = entry.SenseList.Count;

        string? firstPrimarySpellingInHiragana = firstPrimarySpelling is not null
            ? JapaneseUtils.KatakanaToHiragana(firstPrimarySpelling)
            : null;

        Dictionary<string, JmdictRecord> recordDictionary = new(kanjiElementCount + readingElementCount, StringComparer.Ordinal);
        if (spellingsWithoutSearchOnlyFormsExist)
        {
            for (int i = 0; i < kanjiElementCount; i++)
            {
                KanjiElement kanjiElement = entry.KanjiElements[i];

                string key = JapaneseUtils.KatakanaToHiragana(kanjiElement.Keb).GetPooledString();
                if (recordDictionary.ContainsKey(key))
                {
                    if (!kanjiElement.KeInfList.Contains("sK"))
                    {
                        ++index;
                    }

                    continue;
                }

                if (kanjiElement.KeInfList.Contains("sK"))
                {
                    if (recordDictionary.TryGetValue(firstPrimarySpellingInHiragana!, out JmdictRecord? primaryRecord))
                    {
                        recordDictionary.Add(key, primaryRecord);
                    }

                    continue;
                }

                List<string> readingList = new(readingElementCount);
                List<string[]?> readingsOrthographyInfoList = new(readingElementCount);
                for (int j = 0; j < readingElementCount; j++)
                {
                    ReadingElement readingElement = entry.ReadingElements[j];

                    if (!readingElement.ReInfList.Contains("sk")
                        && (readingElement.ReRestrList.Count is 0 || readingElement.ReRestrList.Contains(kanjiElement.Keb)))
                    {
                        readingList.Add(readingElement.Reb);
                        readingsOrthographyInfoList.Add(readingElement.ReInfList.TrimToArray());
                    }
                }

                List<string[]> definitionList = new(senseListCount);
                List<string[]> wordClassList = new(senseListCount);
                List<string[]?> readingRestrictionList = new(senseListCount);
                List<string[]?> spellingRestrictionList = new(senseListCount);
                List<string[]?> fieldList = new(senseListCount);
                List<string[]?> miscList = new(senseListCount);
                List<string[]?> dialectList = new(senseListCount);
                List<string?> definitionInfoList = new(senseListCount);
                List<string[]?> relatedTermList = new(senseListCount);
                List<string[]?> antonymList = new(senseListCount);
                List<LoanwordSource[]?> loanwordSourceList = new(senseListCount);
                for (int j = 0; j < senseListCount; j++)
                {
                    Sense sense = entry.SenseList[j];

                    if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                        || sense.StagKList.Contains(kanjiElement.Keb)
                        || sense.StagRList.Any(readingList.Contains))
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

        List<ReadingElement> readingElementsWithoutSearchOnlyForms = entry.ReadingElements.Where(static ke => !ke.ReInfList.Contains("sk")).ToList();
        bool readingElementsWithoutSearchOnlyFormsExist = readingElementsWithoutSearchOnlyForms.Count > 0;
        if (readingElementsWithoutSearchOnlyFormsExist)
        {
            string[] allReadingsWithoutSearchOnlyForms = readingElementsWithoutSearchOnlyForms.Select(static rEle => rEle.Reb).ToArray();
            string[]?[] allROrthographyInfoWithoutSearchOnlyForms = readingElementsWithoutSearchOnlyForms.Select(static rEle => rEle.ReInfList.TrimToArray()).ToArray();
            string firstReadingInHiragana = JapaneseUtils.KatakanaToHiragana(allReadingsWithoutSearchOnlyForms[0]);

            index = 0;
            for (int i = 0; i < readingElementCount; i++)
            {
                ReadingElement readingElement = entry.ReadingElements[i];

                string key = JapaneseUtils.KatakanaToHiragana(readingElement.Reb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    if (!readingElement.ReInfList.Contains("sk"))
                    {
                        ++index;
                    }

                    continue;
                }

                if (readingElement.ReInfList.Contains("sk"))
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
                        primarySpelling = firstPrimarySpelling!;
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

                List<string[]> definitionList = new(senseListCount);
                List<string[]> wordClassList = new(senseListCount);
                List<string[]?> readingRestrictionList = new(senseListCount);
                List<string[]?> spellingRestrictionList = new(senseListCount);
                List<string[]?> fieldList = new(senseListCount);
                List<string[]?> miscList = new(senseListCount);
                List<string[]?> dialectList = new(senseListCount);
                List<string?> definitionInfoList = new(senseListCount);
                List<string[]?> relatedTermList = new(senseListCount);
                List<string[]?> antonymList = new(senseListCount);
                List<LoanwordSource[]?> loanwordSourceList = new(senseListCount);
                for (int j = 0; j < senseListCount; j++)
                {
                    Sense sense = entry.SenseList[j];
                    if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                        || sense.StagRList.Contains(readingElement.Reb)
                        || sense.StagKList.Contains(primarySpelling)
                        || (alternativeSpellings is not null && sense.StagKList.Any(alternativeSpellings.Contains)))
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
                    for (int j = 0; j < kanjiElementCount; j++)
                    {
                        _ = recordDictionary.TryAdd(JapaneseUtils.KatakanaToHiragana(entry.KanjiElements[j].Keb.GetPooledString()), record);
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

        List<string>?[] exclusiveSenseFieldValues = new List<string>?[senseField.Count];
        List<string> senseFieldValuesSharedByAllSenses = [];

        for (int i = 0; i < senseField.Count; i++)
        {
            string[] senseFieldValue = senseField[i];
            for (int j = 0; j < senseFieldValue.Length; j++)
            {
                string value = senseFieldValue[j];
                if (senseField.All(wc => wc.Contains(value)))
                {
                    if (!senseFieldValuesSharedByAllSenses.Contains(value))
                    {
                        senseFieldValuesSharedByAllSenses.Add(value);
                    }
                }
                else
                {
                    exclusiveSenseFieldValues[i] ??= [];
                    exclusiveSenseFieldValues[i]!.Add(value);
                }
            }
        }

        return senseFieldValuesSharedByAllSenses.Count is 0
            ? (senseField.TrimToArray(), null)
            : exclusiveSenseFieldValues.All(static ewc => ewc is null)
                ? (null, senseFieldValuesSharedByAllSenses.TrimToArray())
                : (exclusiveSenseFieldValues.Select(static ewc => ewc?.TrimToArray()).ToArray(), senseFieldValuesSharedByAllSenses.TrimToArray());
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

        List<string>?[] exclusiveSenseFieldValues = new List<string>?[senseField.Count];
        List<string> senseFieldValuesSharedByAllSenses = [];

        for (int i = 0; i < senseField.Count; i++)
        {
            string[]? senseFieldValue = senseField[i];
            if (senseFieldValue is not null)
            {
                for (int j = 0; j < senseFieldValue.Length; j++)
                {
                    string value = senseFieldValue[j];
                    if (senseField.All(wc => wc?.Contains(value) ?? false))
                    {
                        if (!senseFieldValuesSharedByAllSenses.Contains(value))
                        {
                            senseFieldValuesSharedByAllSenses.Add(value);
                        }
                    }
                    else
                    {
                        exclusiveSenseFieldValues[i] ??= [];
                        exclusiveSenseFieldValues[i]!.Add(value);
                    }
                }
            }
        }

        return senseFieldValuesSharedByAllSenses.Count is 0
            ? (senseField.TrimListOfNullableElementsToArray(), null)
            : exclusiveSenseFieldValues.All(static ewc => ewc is null)
                ? (null, senseFieldValuesSharedByAllSenses.TrimToArray())
                : (exclusiveSenseFieldValues.Select(static ewc => ewc?.TrimToArray()).ToArray(), senseFieldValuesSharedByAllSenses.TrimToArray());
    }
}
