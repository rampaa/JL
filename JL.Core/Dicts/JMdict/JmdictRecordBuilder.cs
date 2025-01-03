using JL.Core.Utilities;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictRecordBuilder
{
    public static void AddToDictionary(JmdictEntry entry, IDictionary<string, IList<IDictRecord>> jmdictDictionary)
    {
        List<KanjiElement> kanjiElementsWithoutSearchOnlyForms = entry.KanjiElements.Where(static ke => !ke.KeInfList.Contains("sK")).ToList();
        string[] allSpellingsWithoutSearchOnlyForms = kanjiElementsWithoutSearchOnlyForms.Select(static ke => ke.Keb).ToArray();
        string[]?[] allKanjiOrthographyInfoWithoutSearchOnlyForms = kanjiElementsWithoutSearchOnlyForms.Select(static ke => ke.KeInfList.TrimListToArray()).ToArray();

        int index = 0;
        int kanjiElementCount = entry.KanjiElements.Count;
        int readingElementCount = entry.ReadingElements.Count;
        int senseListCount = entry.SenseList.Count;
        Dictionary<string, JmdictRecord> recordDictionary = new(kanjiElementCount + readingElementCount, StringComparer.Ordinal);
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
                if (recordDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(entry.KanjiElements[0].Keb), out JmdictRecord? primaryRecord))
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
                    readingsOrthographyInfoList.Add(readingElement.ReInfList.TrimListToArray());
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
                    || sense.StagRList.Intersect(readingList).Any())
                {
                    definitionList.Add(sense.GlossList.ToArray());
                    wordClassList.Add(sense.PosList.ToArray());
                    readingRestrictionList.Add(sense.StagRList.TrimListToArray());
                    spellingRestrictionList.Add(sense.StagKList.TrimListToArray());
                    fieldList.Add(sense.FieldList.TrimListToArray());
                    miscList.Add(sense.MiscList.TrimListToArray());
                    dialectList.Add(sense.DialList.TrimListToArray());
                    definitionInfoList.Add(sense.SInf);
                    relatedTermList.Add(sense.XRefList.TrimListToArray());
                    antonymList.Add(sense.AntList.TrimListToArray());
                    loanwordSourceList.Add(sense.LSourceList.TrimListToArray());
                }
            }

            JmdictRecord record = new(entry.Id,
                kanjiElement.Keb,
                allKanjiOrthographyInfoWithoutSearchOnlyForms[index],
                allSpellingsWithoutSearchOnlyForms.RemoveAt(index),
                allKanjiOrthographyInfoWithoutSearchOnlyForms.RemoveAt(index),
                readingList.TrimListToArray(),
                readingsOrthographyInfoList.TrimListOfNullableArraysToArrayOfArrays(),
                definitionList.ToArray(),
                wordClassList.ToArray(),
                spellingRestrictionList.TrimListOfNullableArraysToArrayOfArrays(),
                readingRestrictionList.TrimListOfNullableArraysToArrayOfArrays(),
                fieldList.TrimListOfNullableArraysToArrayOfArrays(),
                miscList.TrimListOfNullableArraysToArrayOfArrays(),
                definitionInfoList.TrimListWithNullableElementsToArray(),
                dialectList.TrimListOfNullableArraysToArrayOfArrays(),
                loanwordSourceList.TrimListOfNullableArraysToArrayOfArrays(),
                relatedTermList.TrimListOfNullableArraysToArrayOfArrays(),
                antonymList.TrimListOfNullableArraysToArrayOfArrays());

            recordDictionary.Add(key, record);

            ++index;
        }

        List<ReadingElement> readingElementsWithoutSearchOnlyForms = entry.ReadingElements.Where(static ke => !ke.ReInfList.Contains("sk")).ToList();
        string[] allReadingsWithoutSearchOnlyForms = readingElementsWithoutSearchOnlyForms.Select(static rEle => rEle.Reb).ToArray();
        string[]?[] allROrthographyInfoWithoutSearchOnlyForms = readingElementsWithoutSearchOnlyForms.Select(static rEle => rEle.ReInfList.TrimListToArray()).ToArray();

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
                if (recordDictionary.TryGetValue(JapaneseUtils.KatakanaToHiragana(entry.ReadingElements[0].Reb), out JmdictRecord? primaryRecord))
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

            if (readingElement.ReRestrList.Count > 0 || allSpellingsWithoutSearchOnlyForms.Length > 0)
            {
                if (readingElement.ReRestrList.Count > 0)
                {
                    primarySpelling = readingElement.ReRestrList[0];
                    alternativeSpellings = readingElement.ReRestrList.RemoveAtToArray(0);
                }

                else
                {
                    primarySpelling = allSpellingsWithoutSearchOnlyForms[0];
                    alternativeSpellings = allSpellingsWithoutSearchOnlyForms.RemoveAt(0);
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
                    || (alternativeSpellings is not null && sense.StagKList.Intersect(alternativeSpellings).Any()))
                {
                    definitionList.Add(sense.GlossList.ToArray());
                    wordClassList.Add(sense.PosList.ToArray());
                    readingRestrictionList.Add(sense.StagRList.TrimListToArray());
                    spellingRestrictionList.Add(sense.StagKList.TrimListToArray());
                    fieldList.Add(sense.FieldList.TrimListToArray());
                    miscList.Add(sense.MiscList.TrimListToArray());
                    dialectList.Add(sense.DialList.TrimListToArray());
                    definitionInfoList.Add(sense.SInf);
                    relatedTermList.Add(sense.XRefList.TrimListToArray());
                    antonymList.Add(sense.AntList.TrimListToArray());
                    loanwordSourceList.Add(sense.LSourceList.TrimListToArray());
                }
            }

            JmdictRecord record = new(entry.Id,
                primarySpelling,
                primarySpellingOrthographyInfo,
                alternativeSpellings,
                alternativeSpellingsOrthographyInfo,
                readings,
                readingsOrthographyInfo,
                definitionList.ToArray(),
                wordClassList.ToArray(),
                spellingRestrictionList.TrimListOfNullableArraysToArrayOfArrays(),
                readingRestrictionList.TrimListOfNullableArraysToArrayOfArrays(),
                fieldList.TrimListOfNullableArraysToArrayOfArrays(),
                miscList.TrimListOfNullableArraysToArrayOfArrays(),
                definitionInfoList.TrimListWithNullableElementsToArray(),
                dialectList.TrimListOfNullableArraysToArrayOfArrays(),
                loanwordSourceList.TrimListOfNullableArraysToArrayOfArrays(),
                relatedTermList.TrimListOfNullableArraysToArrayOfArrays(),
                antonymList.TrimListOfNullableArraysToArrayOfArrays());

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
}
