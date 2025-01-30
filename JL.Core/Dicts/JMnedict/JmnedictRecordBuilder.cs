using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(JmnedictEntry entry, IDictionary<string, IList<IDictRecord>> jmnedictDictionary)
    {
        int translationListCount = entry.TranslationList.Count;
        int kebListCount = entry.KebList.Count;
        Dictionary<string, JmnedictRecord> recordDictionary;
        if (kebListCount > 0)
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(kebListCount, StringComparer.Ordinal);
            for (int i = 0; i < kebListCount; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.KebList[i]).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                string[][] definitionsArray = new string[translationListCount][];
                string[][] nameTypesArray = new string[translationListCount][];
                // string[]?[] relatedTermsArray = new string[translationListCount][];

                for (int j = 0; j < translationListCount; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionsArray[j] = translation.TransDetList.ToArray();
                    nameTypesArray[j] = translation.NameTypeList.ToArray();
                    // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
                }

                JmnedictRecord record = new(entry.Id, entry.KebList[i], entry.KebList.RemoveAtToArray(i), entry.RebList.TrimListToArray(), definitionsArray, nameTypesArray);
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }

        else
        {
            int rebListCount = entry.RebList.Count;
            recordDictionary = new Dictionary<string, JmnedictRecord>(rebListCount, StringComparer.Ordinal);
            for (int i = 0; i < rebListCount; i++)
            {
                string key = JapaneseUtils.KatakanaToHiragana(entry.RebList[i]).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                string[][] definitionsArray = new string[translationListCount][];
                string[][] nameTypesArray = new string[translationListCount][];
                // string[]?[] relatedTermsArray = new string[translationListCount][];

                for (int j = 0; j < translationListCount; j++)
                {
                    Translation translation = entry.TranslationList[j];

                    definitionsArray[j] = translation.TransDetList.ToArray();
                    nameTypesArray[j] = translation.NameTypeList.ToArray();
                    // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
                }

                JmnedictRecord record = new(entry.Id, entry.RebList[i], entry.RebList.RemoveAtToArray(i), null, definitionsArray, nameTypesArray);
                // record.RelatedTerms = relatedTermsArray;

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
