using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(JmnedictEntry entry, Dictionary<string, IList<IDictRecord>> jmnedictDictionary)
    {
        Dictionary<string, JmnedictRecord> recordDictionary = new();

        if (entry.KebList.Count > 0)
        {
            for (int i = 0; i < entry.KebList.Count; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.KebList[i]).GetPooledString();

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
                    //relatedTermList.Add(translation.XRefList > 0 ? translation.XRefList.ToArray() : null);
                }

                JmnedictRecord record = new(entry.Id, entry.KebList[i], entry.KebList.RemoveAtToArray(i), entry.RebList.TrimStringListToStringArray(), definitionList.ToArray(), nameTypeList.ToArray());
                //record.RelatedTerms = relatedTermList.TrimListToArray();

                recordDictionary.Add(key, record);
            }
        }

        else
        {
            for (int i = 0; i < entry.RebList.Count; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.RebList[i]).GetPooledString();

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

                JmnedictRecord record = new(entry.Id, entry.RebList[i], entry.RebList.RemoveAtToArray(i), null, definitionList.ToArray(), nameTypeList.ToArray());
                // record.RelatedTerms = relatedTermList.TrimListToArray()

                recordDictionary.Add(key, record);
            }
        }

        foreach ((string key, JmnedictRecord jmnedictRecord) in recordDictionary)
        {
            if (jmnedictDictionary.TryGetValue(key, out IList<IDictRecord>? tempRecordList))
            {
                tempRecordList.Add(jmnedictRecord);
            }
            else
            {
                jmnedictDictionary[key] = new List<IDictRecord> { jmnedictRecord };
            }
        }
    }
}
