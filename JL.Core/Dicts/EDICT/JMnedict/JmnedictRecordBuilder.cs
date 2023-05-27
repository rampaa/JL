using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(JmnedictEntry entry, Dictionary<string, List<IDictRecord>> jmnedictDictionary)
    {
        Dictionary<string, JmnedictRecord> recordDictionary = new();

        if (entry.KebList.Count > 0)
        {
            int kebListCount = entry.KebList.Count;
            for (int i = 0; i < kebListCount; i++)
            {
                JmnedictRecord record = new(entry.KebList[i])
                {
                    Readings = entry.RebList
                };

                int transListCount = entry.TranslationList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    record.Definitions!.Add(translation.TransDetList);
                    record.NameTypes!.Add(translation.NameTypeList);
                    // record.RelatedTerms.Add(translation.XRefList);
                }

                recordDictionary.Add(record.PrimarySpelling, record);
            }

            List<string> alternativeSpellings = recordDictionary.Keys.ToList();

            foreach (KeyValuePair<string, JmnedictRecord> record in recordDictionary)
            {
                int alternativeSpellingsCount = alternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (record.Key != alternativeSpellings[i])
                    {
                        record.Value.AlternativeSpellings!.Add(alternativeSpellings[i]);
                    }
                }
            }
        }

        else
        {
            int rebListCount = entry.RebList.Count;
            for (int i = 0; i < rebListCount; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.RebList[i]);

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.RebList[i]);

                int transListCount = entry.TranslationList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    record.Definitions!.Add(translation.TransDetList);
                    record.NameTypes!.Add(translation.NameTypeList);
                    //record.RelatedTerms.Add(translation.XRefList);
                }

                record.AlternativeSpellings = entry.RebList.ToList();
                record.AlternativeSpellings.RemoveAt(i);

                recordDictionary.Add(key, record);
            }
        }

        foreach (KeyValuePair<string, JmnedictRecord> recordKeyValuePair in recordDictionary)
        {
            recordKeyValuePair.Value.Definitions = Utils.TrimListOfLists(recordKeyValuePair.Value.Definitions!);
            recordKeyValuePair.Value.NameTypes = Utils.TrimListOfLists(recordKeyValuePair.Value.NameTypes!);
            recordKeyValuePair.Value.AlternativeSpellings = Utils.TrimStringList(recordKeyValuePair.Value.AlternativeSpellings!);
            recordKeyValuePair.Value.Readings = Utils.TrimStringList(recordKeyValuePair.Value.Readings!);
            recordKeyValuePair.Value.Id = entry.Id;

            string key = JapaneseUtils.KatakanaToHiragana(recordKeyValuePair.Key);
            if (jmnedictDictionary.TryGetValue(key, out List<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(recordKeyValuePair.Value);
            }
            else
            {
                jmnedictDictionary.Add(key, new List<IDictRecord> { recordKeyValuePair.Value });
            }
        }
    }
}
