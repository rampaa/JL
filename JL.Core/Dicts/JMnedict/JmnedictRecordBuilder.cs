using System.Diagnostics;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Core.Utilities.Japanese;

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
        string[]?[] nameTypesArray = new string[translationListSpanLength][];
        // string[]?[] relatedTermsArray = new string[translationListCount][];

        for (int j = 0; j < translationListSpanLength; j++)
        {
            ref readonly Translation translation = ref translationListSpan[j];

            definitionsArray[j] = translation.TransDetArray;
            nameTypesArray[j] = translation.NameTypeArray;
            // relatedTermsArray[j] = translation.XRefList.TrimListToArray();
        }

        Dictionary<string, JmnedictRecord> recordDictionary;
        if (kebListSpanLength > 0)
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(kebListSpanLength, StringComparer.Ordinal);
            for (int i = 0; i < kebListSpan.Length; i++)
            {
                string keb = kebListSpan[i];
                string key = JapaneseUtils.NormalizeText(keb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.Id, keb, entry.KebList.RemoveAtToArray(i), entry.RebArray, definitionsArray, nameTypesArray.TrimNullableArray());
                // record.RelatedTerms = relatedTermsArray;

                recordDictionary.Add(key, record);
            }
        }
        else
        {
            recordDictionary = new Dictionary<string, JmnedictRecord>(entry.RebArray.Length, StringComparer.Ordinal);
            for (int i = 0; i < entry.RebArray.Length; i++)
            {
                string reb = entry.RebArray[i];
                string key = JapaneseUtils.NormalizeText(reb).GetPooledString();

                if (recordDictionary.ContainsKey(key))
                {
                    continue;
                }

                JmnedictRecord record = new(entry.Id, reb, entry.RebArray.RemoveAt(i), null, definitionsArray, nameTypesArray.TrimNullableArray());
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
