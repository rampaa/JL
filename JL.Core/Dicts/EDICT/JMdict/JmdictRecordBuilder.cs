using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMdict;

internal static class JmdictRecordBuilder
{
    public static void AddToDictionary(JmdictEntry entry, Dictionary<string, List<IDictRecord>> jmdictDictionary)
    {
        // entry (k_ele*, r_ele+, sense+)
        // k_ele (keb, ke_inf*, ke_pri*)
        // r_ele (reb, re_restr*, re_inf*, re_pri*)
        // sense (stagk*, stagr*, pos*, xref*, ant*, field*, misc*, s_inf*, dial*, gloss*)

        Dictionary<string, JmdictRecord> recordDictionary = new();

        int kEleListCount = entry.KanjiElements.Count;
        for (int i = 0; i < kEleListCount; i++)
        {
            KanjiElement kanjiElement = entry.KanjiElements[i];

            List<string> readingList = new();
            List<string[]?> readingsOrthographyInfoList = new();
            for (int j = 0; j < entry.ReadingElements.Count; j++)
            {
                ReadingElement readingElement = entry.ReadingElements[j];

                if (readingElement.ReRestrList.Count is 0 || readingElement.ReRestrList.Contains(kanjiElement.Keb))
                {
                    readingList.Add(readingElement.Reb);
                    readingsOrthographyInfoList.Add(readingElement.ReInfList.ToArray().TrimStringArray());
                }
            }

            List<string[]> definitionList = new();
            List<string[]> wordClassList = new();
            List<string[]?> readingRestrictionList = new();
            List<string[]?> spellingRestrictionList = new();
            List<string[]?> fieldList = new();
            List<string[]?> miscList = new();
            List<string[]?> dialectList = new();
            List<string?> definitionInfoList = new();
            List<string[]?> relatedTermList = new();
            List<string[]?> antonymList = new();
            List<LoanwordSource[]?> loanwordSourceList = new();
            for (int j = 0; j < entry.SenseList.Count; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                    || sense.StagKList.Contains(kanjiElement.Keb)
                    || sense.StagRList.Intersect(readingList).Any())
                {
                    definitionList.Add(sense.GlossList.ToArray());
                    wordClassList.Add(sense.PosList.ToArray());
                    readingRestrictionList.Add(sense.StagRList.ToArray().TrimStringArray());
                    spellingRestrictionList.Add(sense.StagKList.ToArray().TrimStringArray());
                    fieldList.Add(sense.FieldList.ToArray().TrimStringArray());
                    miscList.Add(sense.MiscList.ToArray().TrimStringArray());
                    dialectList.Add(sense.DialList.ToArray().TrimStringArray());
                    definitionInfoList.Add(sense.SInf);
                    relatedTermList.Add(sense.XRefList.ToArray().TrimStringArray());
                    antonymList.Add(sense.AntList.ToArray().TrimStringArray());
                    loanwordSourceList.Add(sense.LSourceList.TrimListToArray());
                }
            }

            JmdictRecord record = new(entry.Id,
                kanjiElement.Keb,
                kanjiElement.KeInfList.ToArray().TrimStringArray(),
                readingList.ToArray().TrimStringArray(),
                readingsOrthographyInfoList.TrimListOfArraysToArrayOfArrays(),
                definitionList.ToArray(),
                wordClassList.ToArray(),
                spellingRestrictionList.TrimListOfArraysToArrayOfArrays(),
                readingRestrictionList.TrimListOfArraysToArrayOfArrays(),
                fieldList.TrimListOfArraysToArrayOfArrays(),
                miscList.TrimListOfArraysToArrayOfArrays(),
                definitionInfoList.TrimListToArray(),
                dialectList.TrimListOfArraysToArrayOfArrays(),
                loanwordSourceList.TrimListOfArraysToArrayOfArrays(),
                relatedTermList.TrimListOfArraysToArrayOfArrays(),
                antonymList.TrimListOfArraysToArrayOfArrays());

            recordDictionary.Add(record.PrimarySpelling, record);
        }

        List<string> allSpellings = recordDictionary.Keys.ToList();

        foreach ((string key, JmdictRecord result) in recordDictionary)
        {
            List<string> alternativeSpellingList = new();
            List<string[]?> alternativeSpellingsOrthographyInfoList = new();
            for (int i = 0; i < allSpellings.Count; i++)
            {
                string spelling = allSpellings[i];

                if (key != spelling)
                {
                    alternativeSpellingList.Add(spelling);

                    if (recordDictionary.TryGetValue(spelling, out JmdictRecord? tempResult))
                    {
                        alternativeSpellingsOrthographyInfoList.Add(tempResult.PrimarySpellingOrthographyInfo);
                    }
                }
            }

            result.AlternativeSpellings = alternativeSpellingList.Count > 0
                ? alternativeSpellingList.ToArray()
                : null;

            result.AlternativeSpellingsOrthographyInfo = alternativeSpellingsOrthographyInfoList.TrimListOfArraysToArrayOfArrays();
        }

        List<string> allReadings = entry.ReadingElements.Select(static rEle => rEle.Reb).ToList();
        List<string[]?> allROrthographyInfoLists = entry.ReadingElements.Select(static rEle => rEle.ReInfList.ToArray().TrimStringArray()).ToList();

        int rEleListCount = entry.ReadingElements.Count;
        for (int i = 0; i < rEleListCount; i++)
        {
            ReadingElement readingElement = entry.ReadingElements[i];

            string key = JapaneseUtils.KatakanaToHiragana(readingElement.Reb);

            if (recordDictionary.ContainsKey(key))
            {
                continue;
            }

            string primarySpelling;
            string[]? primarySpellingOrthographyInfo = null;
            string[]? readings = null;
            string[]?[]? readingsOrthographyInfo = null;
            string[]? alternativeSpellings;
            string[]?[]? alternativeSpellingsOrthographyInfo = null;

            if (readingElement.ReRestrList.Count > 0 || allSpellings.Count > 0)
            {
                if (readingElement.ReRestrList.Count > 0)
                {
                    primarySpelling = readingElement.ReRestrList[0];
                    alternativeSpellings = readingElement.ReRestrList.RemoveAtToArray(0);
                }

                else
                {
                    primarySpelling = allSpellings[0];
                    alternativeSpellings = allSpellings.RemoveAtToArray(0);
                }

                if (recordDictionary.TryGetValue(primarySpelling, out JmdictRecord? mainEntry))
                {
                    readings = mainEntry.Readings;
                    alternativeSpellingsOrthographyInfo = mainEntry.AlternativeSpellingsOrthographyInfo;
                    readingsOrthographyInfo = mainEntry.ReadingsOrthographyInfo;
                }
            }

            else
            {
                primarySpelling = readingElement.Reb;
                primarySpellingOrthographyInfo = readingElement.ReInfList.ToArray().TrimStringArray();

                alternativeSpellings = allReadings.RemoveAtToArray(i);
                alternativeSpellingsOrthographyInfo = allROrthographyInfoLists.RemoveAtToArray(i);
            }

            List<string[]> definitionList = new();
            List<string[]> wordClassList = new();
            List<string[]?> readingRestrictionList = new();
            List<string[]?> spellingRestrictionList = new();
            List<string[]?> fieldList = new();
            List<string[]?> miscList = new();
            List<string[]?> dialectList = new();
            List<string?> definitionInfoList = new();
            List<string[]?> relatedTermList = new();
            List<string[]?> antonymList = new();
            List<LoanwordSource[]?> loanwordSourceList = new();
            for (int j = 0; j < entry.SenseList.Count; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                    || sense.StagRList.Contains(readingElement.Reb)
                    || sense.StagKList.Contains(primarySpelling)
                    || (alternativeSpellings is not null && sense.StagKList.Intersect(alternativeSpellings).Any()))
                {
                    definitionList.Add(sense.GlossList.ToArray());
                    wordClassList.Add(sense.PosList.ToArray());
                    readingRestrictionList.Add(sense.StagRList.ToArray().TrimStringArray());
                    spellingRestrictionList.Add(sense.StagKList.ToArray().TrimStringArray());
                    fieldList.Add(sense.FieldList.ToArray().TrimStringArray());
                    miscList.Add(sense.MiscList.ToArray().TrimStringArray());
                    dialectList.Add(sense.DialList.ToArray().TrimStringArray());
                    definitionInfoList.Add(sense.SInf);
                    relatedTermList.Add(sense.XRefList.ToArray().TrimStringArray());
                    antonymList.Add(sense.AntList.ToArray().TrimStringArray());
                    loanwordSourceList.Add(sense.LSourceList.TrimListToArray());
                }
            }

            JmdictRecord record = new(entry.Id,
                primarySpelling,
                primarySpellingOrthographyInfo,
                readings,
                readingsOrthographyInfo,
                definitionList.ToArray(),
                wordClassList.ToArray(),
                spellingRestrictionList.TrimListOfArraysToArrayOfArrays(),
                readingRestrictionList.TrimListOfArraysToArrayOfArrays(),
                fieldList.TrimListOfArraysToArrayOfArrays(),
                miscList.TrimListOfArraysToArrayOfArrays(),
                definitionInfoList.TrimListToArray(),
                dialectList.TrimListOfArraysToArrayOfArrays(),
                loanwordSourceList.TrimListOfArraysToArrayOfArrays(),
                relatedTermList.TrimListOfArraysToArrayOfArrays(),
                antonymList.TrimListOfArraysToArrayOfArrays())
            {
                AlternativeSpellings = alternativeSpellings,
                AlternativeSpellingsOrthographyInfo = alternativeSpellingsOrthographyInfo
                // Priorities = kanjiElement.KePriList
            };

            recordDictionary.Add(key, record);
        }

        foreach ((string dictKey, JmdictRecord jmdictRecord) in recordDictionary)
        {
            string key = JapaneseUtils.KatakanaToHiragana(dictKey);
            if (jmdictDictionary.TryGetValue(key, out List<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(jmdictRecord);
            }
            else
            {
                jmdictDictionary.Add(key, new List<IDictRecord> { jmdictRecord });
            }
        }
    }
}
