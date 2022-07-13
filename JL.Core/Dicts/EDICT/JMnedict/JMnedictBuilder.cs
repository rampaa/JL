using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.JMnedict;

public static class JMnedictBuilder
{
    public static void BuildDictionary(JMnedictEntry entry, Dictionary<string, List<IResult>> jMnedictDictionary)
    {
        Dictionary<string, JMnedictResult> resultList = new();

        if (entry.KebList.Any())
        {
            int kebListCount = entry.KebList.Count;
            for (int i = 0; i < kebListCount; i++)
            {
                string keb = entry.KebList[i];

                JMnedictResult result = new();
                string key = Kana.KatakanaToHiraganaConverter(keb);

                result.PrimarySpelling = keb;
                result.Readings = entry.RebList;

                int transListCount = entry.TransList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Trans trans = entry.TransList[j];

                    result.Definitions!.AddRange(trans.TransDetList);
                    result.NameTypes!.AddRange(trans.NameTypeList);
                    // result.RelatedTerms.AddRange(trans.XRefList);
                }

                resultList.TryAdd(key, result);
            }

            List<string> alternativeSpellings = resultList.Keys.ToList();

            foreach (KeyValuePair<string, JMnedictResult> item in resultList)
            {
                int alternativeSpellingsCount = alternativeSpellings.Count;
                for (int i = 0; i < alternativeSpellingsCount; i++)
                {
                    if (item.Key != alternativeSpellings[i])
                    {
                        item.Value.AlternativeSpellings!.Add(alternativeSpellings[i]);
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

                if (resultList.ContainsKey(key))
                    continue;

                JMnedictResult result = new() { PrimarySpelling = reb };

                int transListCount = entry.TransList.Count;
                for (int j = 0; j < transListCount; j++)
                {
                    Trans trans = entry.TransList[j];

                    result.Definitions!.AddRange(trans.TransDetList);

                    result.NameTypes!.AddRange(trans.NameTypeList);

                    //result.RelatedTerms.AddRange(trans.XRefList);
                }

                resultList.Add(key, result);
            }
        }

        foreach (KeyValuePair<string, JMnedictResult> rl in resultList)
        {
            rl.Value.Id = entry.Id;
            string key = rl.Key;

            rl.Value.AlternativeSpellings = Utils.TrimStringList(rl.Value.AlternativeSpellings!);
            rl.Value.Definitions = Utils.TrimStringList(rl.Value.Definitions!);
            rl.Value.NameTypes = Utils.TrimStringList(rl.Value.NameTypes!);
            rl.Value.Readings = Utils.TrimStringList(rl.Value.Readings!);

            if (jMnedictDictionary.TryGetValue(key, out List<IResult>? tempList))
                tempList.Add(rl.Value);
            else
                tempList = new() { rl.Value };

            jMnedictDictionary[key] = tempList;
        }
    }
}
