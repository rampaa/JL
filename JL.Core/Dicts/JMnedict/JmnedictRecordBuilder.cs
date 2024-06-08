using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(JmnedictEntry entry, IDictionary<string, IList<IDictRecord>> jmnedictDictionary)
    {
        Dictionary<string, JmnedictRecord> recordDictionary = new(StringComparer.Ordinal);

        int translationListCount = entry.TranslationList.Count;
        int kebListCount = entry.KebList.Count;
        if (kebListCount > 0)
        {
            for (int i = 0; i < kebListCount; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.KebList[i]).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                List<string[]> definitionList = [];
                List<string[]> nameTypeList = [];
                // List<string[]?> relatedTermList = [];

                for (int j = 0; j < translationListCount; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionList.Add(translation.TransDetList.ToArray());
                    nameTypeList.Add(translation.NameTypeList.ToArray());
                    //relatedTermList.Add(translation.XRefList > 0 ? translation.XRefList.ToArray() : null);
                }

                JmnedictRecord record = new(entry.Id, entry.KebList[i], entry.KebList.RemoveAtToArray(i), entry.RebList.TrimListToArray(), definitionList.ToArray(), nameTypeList.ToArray());
                //record.RelatedTerms = relatedTermList.TrimListToArray();

                recordDictionary.Add(key, record);
            }
        }

        else
        {
            int rebListCount = entry.RebList.Count;
            for (int i = 0; i < rebListCount; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.RebList[i]).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                List<string[]> definitionList = [];
                List<string[]> nameTypeList = [];
                // List<string[]?> relatedTermList = [];

                for (int j = 0; j < translationListCount; j++)
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
                jmnedictDictionary[key] = [jmnedictRecord];
            }
        }
    }
}
