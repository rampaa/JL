using System.Collections.Generic;
using System.Linq;

namespace JL.Dicts.EDICT.JMnedict
{
    public static class JMnedictBuilder
    {
        public static void BuildDictionary(JMnedictEntry entry, Dictionary<string, List<IResult>> jMnedictDictionary)
        {
            Dictionary<string, JMnedictResult> resultList = new();
            List<string> alternativeSpellings;

            if (entry.KebList.Any())
            {
                for (int i = 0; i < entry.KebList.Count; i++)
                {
                    string keb = entry.KebList[i];

                    JMnedictResult result = new();
                    string key = Kana.KatakanaToHiraganaConverter(keb);

                    result.PrimarySpelling = keb;
                    result.Readings = entry.RebList;

                    for (int j = 0; j < entry.TransList.Count; j++)
                    {
                        Trans trans = entry.TransList[j];

                        result.Definitions.AddRange(trans.TransDetList);
                        result.NameTypes.AddRange(trans.NameTypeList);
                        // result.RelatedTerms.AddRange(trans.XRefList);
                    }

                    resultList.TryAdd(key, result);
                }

                alternativeSpellings = resultList.Keys.ToList();

                foreach (KeyValuePair<string, JMnedictResult> item in resultList)
                {
                    for (int i = 0; i < alternativeSpellings.Count; i++)
                    {
                        if (item.Key != alternativeSpellings[i])
                        {
                            item.Value.AlternativeSpellings.Add(alternativeSpellings[i]);
                        }
                    }
                }
            }

            else
            {
                for (int i = 0; i < entry.RebList.Count; i++)
                {
                    string reb = entry.RebList[i];

                    string key = Kana.KatakanaToHiraganaConverter(reb);

                    if (resultList.ContainsKey(key))
                        continue;

                    JMnedictResult result = new();

                    result.PrimarySpelling = reb;

                    for (int j = 0; j < entry.TransList.Count; j++)
                    {
                        Trans trans = entry.TransList[j];

                        result.Definitions.AddRange(trans.TransDetList);

                        result.NameTypes.AddRange(trans.NameTypeList);

                        //result.RelatedTerms.AddRange(trans.XRefList);
                    }

                    resultList.Add(key, result);
                }
            }

            foreach (KeyValuePair<string, JMnedictResult> rl in resultList)
            {
                rl.Value.Id = entry.Id;
                string key = rl.Key;

                if (!rl.Value.AlternativeSpellings.Any())
                    rl.Value.AlternativeSpellings = null;

                if (!rl.Value.Definitions.Any())
                    rl.Value.Definitions = null;

                if (!rl.Value.NameTypes.Any())
                    rl.Value.NameTypes = null;

                if (!rl.Value.PrimarySpelling.Any())
                    rl.Value.PrimarySpelling = null;

                if (!rl.Value.Readings.Any())
                    rl.Value.Readings = null;

                if (jMnedictDictionary.TryGetValue(key, out List<IResult> tempList))
                    tempList.Add(rl.Value);
                else
                    tempList = new() { rl.Value };

                jMnedictDictionary[key] = tempList;
            }
        }
    }
}
