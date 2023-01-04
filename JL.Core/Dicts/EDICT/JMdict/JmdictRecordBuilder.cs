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

            JmdictRecord record = new(kanjiElement.Keb!)
            {
                PrimarySpellingOrthographyInfoList = kanjiElement.KeInfList
            };

            //record.PriorityList = kanjiElement.KePriList;

            int lREleListCount = entry.ReadingElements.Count;
            for (int j = 0; j < lREleListCount; j++)
            {
                ReadingElement readingElement = entry.ReadingElements[j];

                if (readingElement.ReRestrList.Count is 0 || readingElement.ReRestrList.Contains(record.PrimarySpelling))
                {
                    record.Readings?.Add(readingElement.Reb);
                    record.ReadingsOrthographyInfoList?.Add(readingElement.ReInfList);
                }
            }

            int senseListCount = entry.SenseList.Count;
            for (int j = 0; j < senseListCount; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                    || sense.StagKList.Contains(record.PrimarySpelling)
                    || sense.StagRList.Intersect(record.Readings!).Any())
                {
                    ProcessSense(record, sense);
                }
            }

            recordDictionary.Add(record.PrimarySpelling, record);
        }

        List<string> alternativeSpellings = recordDictionary.Keys.ToList();

        foreach ((string key, JmdictRecord result) in recordDictionary)
        {
            int alternativeSpellingsCount = alternativeSpellings.Count;
            for (int i = 0; i < alternativeSpellingsCount; i++)
            {
                string spelling = alternativeSpellings[i];

                if (key != spelling)
                {
                    result.AlternativeSpellings!.Add(spelling);

                    if (recordDictionary.TryGetValue(spelling, out JmdictRecord? tempResult))
                    {
                        result.AlternativeSpellingsOrthographyInfoList!.Add(tempResult.PrimarySpellingOrthographyInfoList);
                    }
                }
            }
        }

        List<string> allReadings = entry.ReadingElements.Select(rEle => rEle.Reb).ToList();
        List<List<string>> allROrthographyInfoLists = entry.ReadingElements.Select(rEle => rEle.ReInfList).ToList();

        int rEleListCount = entry.ReadingElements.Count;
        for (int i = 0; i < rEleListCount; i++)
        {
            ReadingElement readingElement = entry.ReadingElements[i];

            string key = Kana.KatakanaToHiragana(readingElement.Reb);

            if (recordDictionary.ContainsKey(key))
            {
                continue;
            }

            JmdictRecord record;

            if (readingElement.ReRestrList.Count > 0 || alternativeSpellings.Count > 0)
            {
                if (readingElement.ReRestrList.Count > 0)
                {
                    record = new(readingElement.ReRestrList[0]) { AlternativeSpellings = readingElement.ReRestrList };
                }

                else
                {
                    record = new(alternativeSpellings[0]) { AlternativeSpellings = alternativeSpellings.ToList() };
                }

                record.AlternativeSpellings.RemoveAt(0);

                if (recordDictionary.TryGetValue(record.PrimarySpelling, out JmdictRecord? mainEntry))
                {
                    record.Readings = mainEntry.Readings;
                    record.AlternativeSpellingsOrthographyInfoList = mainEntry.AlternativeSpellingsOrthographyInfoList;
                    record.ReadingsOrthographyInfoList = mainEntry.ReadingsOrthographyInfoList;
                }
            }

            else
            {
                record = new(readingElement.Reb)
                {
                    PrimarySpellingOrthographyInfoList = readingElement.ReInfList,
                    AlternativeSpellings = allReadings.ToList(),
                    AlternativeSpellingsOrthographyInfoList = allROrthographyInfoLists.ToList()!
                };

                record.AlternativeSpellings.RemoveAt(i);
                record.AlternativeSpellingsOrthographyInfoList.RemoveAt(i);
            }

            int senseListCount = entry.SenseList.Count;
            for (int j = 0; j < senseListCount; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((sense.StagKList.Count is 0 && sense.StagRList.Count is 0)
                    || sense.StagRList.Contains(readingElement.Reb)
                    || sense.StagKList.Contains(record.PrimarySpelling)
                    || sense.StagKList.Intersect(record.AlternativeSpellings).Any())
                {
                    ProcessSense(record, sense);
                }
            }

            recordDictionary.Add(key, record);
        }

        foreach (KeyValuePair<string, JmdictRecord> recordKeyValuePair in recordDictionary)
        {
            recordKeyValuePair.Value.Readings = Utils.TrimStringList(recordKeyValuePair.Value.Readings!);
            recordKeyValuePair.Value.AlternativeSpellings = Utils.TrimStringList(recordKeyValuePair.Value.AlternativeSpellings!);
            recordKeyValuePair.Value.PrimarySpellingOrthographyInfoList = Utils.TrimStringList(recordKeyValuePair.Value.PrimarySpellingOrthographyInfoList!);
            recordKeyValuePair.Value.DefinitionInfo = Utils.TrimStringList(recordKeyValuePair.Value.DefinitionInfo!)!;
            recordKeyValuePair.Value.Definitions = TrimListOfLists(recordKeyValuePair.Value.Definitions!)!;
            recordKeyValuePair.Value.ReadingRestrictions = TrimListOfLists(recordKeyValuePair.Value.ReadingRestrictions);
            recordKeyValuePair.Value.SpellingRestrictions = TrimListOfLists(recordKeyValuePair.Value.SpellingRestrictions);
            recordKeyValuePair.Value.Dialects = TrimListOfLists(recordKeyValuePair.Value.Dialects);
            recordKeyValuePair.Value.MiscList = TrimListOfLists(recordKeyValuePair.Value.MiscList);
            recordKeyValuePair.Value.AlternativeSpellingsOrthographyInfoList = TrimListOfLists(recordKeyValuePair.Value.AlternativeSpellingsOrthographyInfoList);
            recordKeyValuePair.Value.ReadingsOrthographyInfoList = TrimListOfLists(recordKeyValuePair.Value.ReadingsOrthographyInfoList);
            recordKeyValuePair.Value.FieldList = TrimListOfLists(recordKeyValuePair.Value.FieldList);
            recordKeyValuePair.Value.WordClasses = TrimListOfLists(recordKeyValuePair.Value.WordClasses);
            recordKeyValuePair.Value.RelatedTerms = TrimListOfLists(recordKeyValuePair.Value.RelatedTerms);
            recordKeyValuePair.Value.Antonyms = TrimListOfLists(recordKeyValuePair.Value.Antonyms);
            recordKeyValuePair.Value.LoanwordEtymology = TrimListOfLists(recordKeyValuePair.Value.LoanwordEtymology);

            recordKeyValuePair.Value.Id = entry.Id;
            string key = Kana.KatakanaToHiragana(recordKeyValuePair.Key);

            if (jmdictDictionary.TryGetValue(key, out List<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(recordKeyValuePair.Value);
            }
            else
            {
                jmdictDictionary.Add(key, new List<IDictRecord>() { recordKeyValuePair.Value });
            }
        }
    }

    private static void ProcessSense(JmdictRecord jmdictRecord, Sense sense)
    {
        jmdictRecord.Definitions.Add(sense.GlossList);
        jmdictRecord.ReadingRestrictions!.Add(sense.StagRList.Count > 0 ? sense.StagRList : null);
        jmdictRecord.SpellingRestrictions!.Add(sense.StagKList.Count > 0 ? sense.StagKList : null);
        jmdictRecord.WordClasses!.Add(sense.PosList.Count > 0 ? sense.PosList : null);
        jmdictRecord.FieldList!.Add(sense.FieldList.Count > 0 ? sense.FieldList : null);
        jmdictRecord.MiscList!.Add(sense.MiscList.Count > 0 ? sense.MiscList : null);
        jmdictRecord.Dialects!.Add(sense.DialList.Count > 0 ? sense.DialList : null);
        jmdictRecord.DefinitionInfo!.Add(sense.SInf);
        jmdictRecord.RelatedTerms!.Add(sense.XRefList.Count > 0 ? sense.XRefList : null);
        jmdictRecord.Antonyms!.Add(sense.AntList.Count > 0 ? sense.AntList : null);
        jmdictRecord.LoanwordEtymology!.Add(sense.LSourceList.Count > 0 ? sense.LSourceList : null);
    }

    private static List<List<T>?>? TrimListOfLists<T>(List<List<T>?>? listOfLists)
    {
        List<List<T>?>? listOfListClone = listOfLists;

        if (listOfListClone!.Count is 0 || listOfListClone.All(l => l is null || l.Count is 0))
        {
            listOfListClone = null;
        }
        else
        {
            listOfListClone.TrimExcess();

            int counter = listOfListClone.Count;
            for (int i = 0; i < counter; i++)
            {
                listOfListClone[i]?.TrimExcess();
            }
        }

        return listOfListClone;
    }
}
