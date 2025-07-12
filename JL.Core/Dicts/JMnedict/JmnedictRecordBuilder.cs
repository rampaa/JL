using System.Diagnostics;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.JMnedict;

internal static class JmnedictRecordBuilder
{
    public static void AddToDictionary(in JmnedictEntry entry, IDictionary<string, IList<IDictRecord>> jmnedictDictionary)
    {
        ReadOnlySpan<string> kebListSpan = entry.KebList.AsReadOnlySpan();
        ReadOnlySpan<Translation> translationListSpan = entry.TranslationList.AsReadOnlySpan();

        int kebListSpanLength = kebListSpan.Length;
        int translationListSpanLength = translationListSpan.Length;

        Debug.Assert(translationListSpanLength > 0);

        string[][] definitionsArray = new string[translationListSpanLength][];
        string[][] nameTypesArray = new string[translationListSpanLength][];
        // string[]?[] relatedTermsArray = new string[translationListCount][];

        for (int j = 0; j < translationListSpanLength; j++)
        {
            ref readonly Translation translation = ref translationListSpan[j];

            definitionsArray[j] = translation.TransDetList.ToArray();
            nameTypesArray[j] = translation.NameTypeList.ToArray();
            // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
        }

        Dictionary<string, JmnedictRecord> recordDictionary;
        if (kebListSpanLength > 0)
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(kebListSpanLength, StringComparer.Ordinal);
            for (int i = 0; i < kebListSpan.Length; i++)
            {
                ref readonly string keb = ref kebListSpan[i];
                string key = JapaneseUtils.KatakanaToHiragana(keb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.Id, keb, entry.KebList.RemoveAtToArray(i), entry.RebList.TrimToArray(), definitionsArray, nameTypesArray);
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }

        else
        {
            ReadOnlySpan<string> rebListSpan = entry.RebList.AsReadOnlySpan();
            recordDictionary = new Dictionary<string, JmnedictRecord>(rebListSpan.Length, StringComparer.Ordinal);
            for (int i = 0; i < rebListSpan.Length; i++)
            {
                ref readonly string reb = ref rebListSpan[i];
                string key = JapaneseUtils.KatakanaToHiragana(reb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
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
