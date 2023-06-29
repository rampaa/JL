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

                    record.Definitions.Add(translation.TransDetList);
                    record.NameTypes.Add(translation.NameTypeList);
                    //record.RelatedTerms!.Add(translation.XRefList > 0 ? translation.XRefList : null);
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

                    record.Definitions.Add(translation.TransDetList);
                    record.NameTypes.Add(translation.NameTypeList);
                    //record.RelatedTerms!.Add(translation.XRefList > 0 ? translation.XRefList : null);
                }

                record.AlternativeSpellings = entry.RebList.ToList();
                record.AlternativeSpellings.RemoveAt(i);

                recordDictionary.Add(key, record);
            }
        }

        foreach ((string dictKey, JmnedictRecord jmnedictRecord) in recordDictionary)
        {
            jmnedictRecord.Definitions = Utils.TrimListOfLists(jmnedictRecord.Definitions);
            jmnedictRecord.NameTypes = Utils.TrimListOfLists(jmnedictRecord.NameTypes);
            //jmnedictRecord.RelatedTerms = Utils.TrimListOfLists(jmnedictRecord.RelatedTerms!);
            jmnedictRecord.AlternativeSpellings = Utils.TrimStringList(jmnedictRecord.AlternativeSpellings!);
            jmnedictRecord.Readings = Utils.TrimStringList(jmnedictRecord.Readings!);
            jmnedictRecord.Id = entry.Id;

            string key = JapaneseUtils.KatakanaToHiragana(dictKey);
            if (jmnedictDictionary.TryGetValue(key, out List<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(jmnedictRecord);
            }
            else
            {
                jmnedictDictionary.Add(key, new List<IDictRecord> { jmnedictRecord });
            }
        }
    }
}
