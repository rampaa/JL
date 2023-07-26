using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(JmnedictEntry entry, Dictionary<string, List<IDictRecord>> jmnedictDictionary)
    {
        Dictionary<string, JmnedictRecord> recordDictionary = new();

        if (entry.KebList.Count > 0)
        {
            for (int i = 0; i < entry.KebList.Count; i++)
            {
                List<string[]> definitionList = new();
                List<string[]> nameTypeList = new();

                for (int j = 0; j < entry.TranslationList.Count; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionList.Add(translation.TransDetList.ToArray());
                    nameTypeList.Add(translation.NameTypeList.ToArray());
                    //record.RelatedTerms!.Add(translation.XRefList > 0 ? translation.XRefList : null);
                }

                JmnedictRecord record = new(entry.KebList[i], entry.RebList.ToArray(), definitionList.ToArray(), nameTypeList.ToArray());

                recordDictionary.Add(record.PrimarySpelling, record);
            }

            List<string> alternativeSpellings = recordDictionary.Keys.ToList();

            foreach (KeyValuePair<string, JmnedictRecord> record in recordDictionary)
            {
                List<string> tempAlternativeSpellingList = new();

                for (int i = 0; i < alternativeSpellings.Count; i++)
                {
                    if (record.Key != alternativeSpellings[i])
                    {
                        tempAlternativeSpellingList.Add(alternativeSpellings[i]);
                    }
                }

                record.Value.AlternativeSpellings = tempAlternativeSpellingList.ToArray();
            }
        }

        else
        {
            for (int i = 0; i < entry.RebList.Count; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.RebList[i]);

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                List<string[]> definitionList = new();
                List<string[]> nameTypeList = new();

                for (int j = 0; j < entry.TranslationList.Count; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionList.Add(translation.TransDetList.ToArray());
                    nameTypeList.Add(translation.NameTypeList.ToArray());
                    //record.RelatedTerms!.Add(translation.XRefList > 0 ? translation.XRefList : null);
                }

                JmnedictRecord record = new(entry.RebList[i], null, definitionList.ToArray(), nameTypeList.ToArray())
                {
                    AlternativeSpellings = entry.RebList.RemoveAtToArray(i)
                };

                recordDictionary.Add(key, record);
            }
        }

        foreach ((string dictKey, JmnedictRecord jmnedictRecord) in recordDictionary)
        {
            //jmnedictRecord.Definitions = Utils.TrimListOfLists(jmnedictRecord.Definitions);
            //jmnedictRecord.NameTypes = Utils.TrimListOfLists(jmnedictRecord.NameTypes);
            //jmnedictRecord.RelatedTerms = Utils.TrimListOfLists(jmnedictRecord.RelatedTerms!);
            jmnedictRecord.AlternativeSpellings = jmnedictRecord.AlternativeSpellings.TrimStringArray()!;
            jmnedictRecord.Readings = jmnedictRecord.Readings.TrimStringArray()!;
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
