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

            JmdictRecord record = new(kanjiElement.Keb)
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

        List<string> allReadings = entry.ReadingElements.Select(static rEle => rEle.Reb).ToList();
        List<List<string>> allROrthographyInfoLists = entry.ReadingElements.Select(static rEle => rEle.ReInfList).ToList();

        int rEleListCount = entry.ReadingElements.Count;
        for (int i = 0; i < rEleListCount; i++)
        {
            ReadingElement readingElement = entry.ReadingElements[i];

            string key = JapaneseUtils.KatakanaToHiragana(readingElement.Reb);

            if (recordDictionary.ContainsKey(key))
            {
                continue;
            }

            JmdictRecord record;

            if (readingElement.ReRestrList.Count > 0 || alternativeSpellings.Count > 0)
            {
                if (readingElement.ReRestrList.Count > 0)
                {
                    record = new JmdictRecord(readingElement.ReRestrList[0]) { AlternativeSpellings = readingElement.ReRestrList };
                }

                else
                {
                    record = new JmdictRecord(alternativeSpellings[0]) { AlternativeSpellings = alternativeSpellings.ToList() };
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
                record = new JmdictRecord(readingElement.Reb)
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

        foreach ((string dictKey, JmdictRecord jmdictRecord) in recordDictionary)
        {
            jmdictRecord.Definitions = Utils.TrimListOfLists(jmdictRecord.Definitions);
            jmdictRecord.WordClasses = Utils.TrimListOfLists(jmdictRecord.WordClasses);
            jmdictRecord.Readings = Utils.TrimStringList(jmdictRecord.Readings!);
            jmdictRecord.AlternativeSpellings = Utils.TrimStringList(jmdictRecord.AlternativeSpellings!);
            jmdictRecord.PrimarySpellingOrthographyInfoList = Utils.TrimStringList(jmdictRecord.PrimarySpellingOrthographyInfoList!);
            jmdictRecord.DefinitionInfo = Utils.TrimStringList(jmdictRecord.DefinitionInfo!)!;
            jmdictRecord.ReadingRestrictions = Utils.TrimNullableListOfLists(jmdictRecord.ReadingRestrictions!);
            jmdictRecord.SpellingRestrictions = Utils.TrimNullableListOfLists(jmdictRecord.SpellingRestrictions!);
            jmdictRecord.Dialects = Utils.TrimNullableListOfLists(jmdictRecord.Dialects!);
            jmdictRecord.MiscList = Utils.TrimNullableListOfLists(jmdictRecord.MiscList!);
            jmdictRecord.AlternativeSpellingsOrthographyInfoList = Utils.TrimNullableListOfLists(jmdictRecord.AlternativeSpellingsOrthographyInfoList!);
            jmdictRecord.ReadingsOrthographyInfoList = Utils.TrimNullableListOfLists(jmdictRecord.ReadingsOrthographyInfoList!);
            jmdictRecord.FieldList = Utils.TrimNullableListOfLists(jmdictRecord.FieldList!);
            jmdictRecord.RelatedTerms = Utils.TrimNullableListOfLists(jmdictRecord.RelatedTerms!);
            jmdictRecord.Antonyms = Utils.TrimNullableListOfLists(jmdictRecord.Antonyms!);
            jmdictRecord.LoanwordEtymology = Utils.TrimNullableListOfLists(jmdictRecord.LoanwordEtymology!);
            jmdictRecord.Id = entry.Id;

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

    private static void ProcessSense(JmdictRecord jmdictRecord, Sense sense)
    {
        jmdictRecord.Definitions.Add(sense.GlossList);
        jmdictRecord.WordClasses.Add(sense.PosList);
        jmdictRecord.ReadingRestrictions!.Add(sense.StagRList);
        jmdictRecord.SpellingRestrictions!.Add(sense.StagKList);
        jmdictRecord.FieldList!.Add(sense.FieldList);
        jmdictRecord.MiscList!.Add(sense.MiscList);
        jmdictRecord.Dialects!.Add(sense.DialList);
        jmdictRecord.DefinitionInfo!.Add(sense.SInf);
        jmdictRecord.RelatedTerms!.Add(sense.XRefList);
        jmdictRecord.Antonyms!.Add(sense.AntList);
        jmdictRecord.LoanwordEtymology!.Add(sense.LSourceList);
    }
}
