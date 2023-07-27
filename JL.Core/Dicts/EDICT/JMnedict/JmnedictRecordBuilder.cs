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
                // List<string[]?> relatedTermList = new();

                for (int j = 0; j < entry.TranslationList.Count; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionList.Add(translation.TransDetList.ToArray());
                    nameTypeList.Add(translation.NameTypeList.ToArray());
                    //relatedTermList.Add(translation.XRefList > 0 ? translation.XRefList.ToArray() : null);
                }

                JmnedictRecord record = new(entry.Id, entry.KebList[i], entry.RebList.ToArray().TrimStringArray(), definitionList.ToArray(), nameTypeList.ToArray());
                //record.RelatedTerms = relatedTermList.TrimListToArray();

                recordDictionary.Add(record.PrimarySpelling, record);
            }

            List<string> allSpellings = recordDictionary.Keys.ToList();

            foreach (KeyValuePair<string, JmnedictRecord> record in recordDictionary)
            {
                List<string> alternativeSpellingList = new();

                for (int i = 0; i < allSpellings.Count; i++)
                {
                    if (record.Key != allSpellings[i])
                    {
                        alternativeSpellingList.Add(allSpellings[i]);
                    }
                }

                record.Value.AlternativeSpellings = alternativeSpellingList.ToArray().TrimStringArray();
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
                // List<string[]?> relatedTermList = new();

                for (int j = 0; j < entry.TranslationList.Count; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionList.Add(translation.TransDetList.ToArray());
                    nameTypeList.Add(translation.NameTypeList.ToArray());
                    // relatedTermList.Add(translation.XRefList > 0 ? translation.XRefList.ToArray() : null);
                }

                JmnedictRecord record = new(entry.Id, entry.RebList[i], null, definitionList.ToArray(), nameTypeList.ToArray())
                {
                    AlternativeSpellings = entry.RebList.RemoveAtToArray(i)
                    //RelatedTerms = relatedTermList.TrimListToArray()
                };

                recordDictionary.Add(key, record);
            }
        }

        foreach ((string dictKey, JmnedictRecord jmnedictRecord) in recordDictionary)
        {
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
