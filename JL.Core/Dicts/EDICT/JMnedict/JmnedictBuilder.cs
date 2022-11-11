using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMnedict;

public static class JmnedictBuilder
{
    public static void BuildDictionary(JmnedictEntry entry, Dictionary<string, List<IDictRecord>> jmnedictDictionary)
    {
        Dictionary<string, JmnedictRecord> recordDictionary = new();

        if (entry.KebList.Any())
        {
            int kebListCount = entry.KebList.Count;
            for (int i = 0; i < kebListCount; i++)
            {
                string keb = entry.KebList[i];

                JmnedictRecord record = new();
                string key = Kana.KatakanaToHiraganaConverter(keb);

                record.PrimarySpelling = keb;
                record.Readings = entry.RebList;

                int transListCount = entry.TransList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Translation translation = entry.TransList[j];

                    record.Definitions!.AddRange(translation.TransDetList);
                    record.NameTypes!.AddRange(translation.NameTypeList);
                    // record.RelatedTerms.AddRange(translation.XRefList);
                }

                recordDictionary.TryAdd(key, record);
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
                string reb = entry.RebList[i];

                string key = Kana.KatakanaToHiraganaConverter(reb);

                if (recordDictionary.ContainsKey(key))
                    continue;

                JmnedictRecord record = new() { PrimarySpelling = reb };

                int transListCount = entry.TransList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Translation translation = entry.TransList[j];

                    record.Definitions!.AddRange(translation.TransDetList);

                    record.NameTypes!.AddRange(translation.NameTypeList);

                    //record.RelatedTerms.AddRange(translation.XRefList);
                }

                recordDictionary.Add(key, record);
            }
        }

        foreach (KeyValuePair<string, JmnedictRecord> recordKeyValuePair in recordDictionary)
        {
            recordKeyValuePair.Value.Id = entry.Id;
            string key = recordKeyValuePair.Key;

            recordKeyValuePair.Value.AlternativeSpellings = Utils.TrimStringList(recordKeyValuePair.Value.AlternativeSpellings!);
            recordKeyValuePair.Value.Definitions = Utils.TrimStringList(recordKeyValuePair.Value.Definitions!);
            recordKeyValuePair.Value.NameTypes = Utils.TrimStringList(recordKeyValuePair.Value.NameTypes!);
            recordKeyValuePair.Value.Readings = Utils.TrimStringList(recordKeyValuePair.Value.Readings!);

            if (jmnedictDictionary.TryGetValue(key, out List<IDictRecord>? tempRecordList))
                tempRecordList.Add(recordKeyValuePair.Value);
            else
                tempRecordList = new() { recordKeyValuePair.Value };

            jmnedictDictionary[key] = tempRecordList;
        }
    }
}
