using System.Runtime.InteropServices;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(in JmnedictEntry entry, IDictionary<string, IList<IDictRecord>> jmnedictDictionary)
    {
        ReadOnlySpan<string> kebListSpan = CollectionsMarshal.AsSpan(entry.KebList);
        ReadOnlySpan<Translation> translationListSpan = CollectionsMarshal.AsSpan(entry.TranslationList);

        int kebListSpanLength = kebListSpan.Length;
        int translationListSpanLength = translationListSpan.Length;

        Dictionary<string, JmnedictRecord> recordDictionary;
        if (kebListSpanLength > 0)
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(kebListSpanLength, StringComparer.Ordinal);
            for (int i = 0; i < kebListSpan.Length; i++)
            {
                string keb = kebListSpan[i];
                string key = JapaneseUtils.KatakanaToHiragana(keb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                string[][] definitionsArray = new string[translationListSpanLength][];
                string[][] nameTypesArray = new string[translationListSpanLength][];
                // string[]?[] relatedTermsArray = new string[translationListCount][];

                for (int j = 0; j < translationListSpan.Length; j++)
                {
                    Translation translation = translationListSpan[j];

                    definitionsArray[j] = translation.TransDetList.ToArray();
                    nameTypesArray[j] = translation.NameTypeList.ToArray();
                    // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
                }

                JmnedictRecord record = new(entry.Id, keb, entry.KebList.RemoveAtToArray(i), entry.RebList.TrimToArray(), definitionsArray, nameTypesArray);
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }

        else
        {
            ReadOnlySpan<string> rebListSpan = CollectionsMarshal.AsSpan(entry.RebList);
            recordDictionary = new Dictionary<string, JmnedictRecord>(rebListSpan.Length, StringComparer.Ordinal);
            for (int i = 0; i < rebListSpan.Length; i++)
            {
                string reb = rebListSpan[i];
                string key = JapaneseUtils.KatakanaToHiragana(reb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                string[][] definitionsArray = new string[translationListSpanLength][];
                string[][] nameTypesArray = new string[translationListSpanLength][];
                // string[]?[] relatedTermsArray = new string[translationListCount][];

                for (int j = 0; j < translationListSpan.Length; j++)
                {
                    Translation translation = translationListSpan[j];

                    definitionsArray[j] = translation.TransDetList.ToArray();
                    nameTypesArray[j] = translation.NameTypeList.ToArray();
                    // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
                }

                JmnedictRecord record = new(entry.Id, reb, entry.RebList.RemoveAtToArray(i), null, definitionsArray, nameTypesArray);
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
