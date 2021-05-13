using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class JMNeDictBuilder
    {
        public static void BuildDictionary(JMnedictEntry entry, Dictionary<string, List<JMnedictResult>> jMnedictDictionary)
        {
            Dictionary<string, JMnedictResult> resultList = new();
            List<string> alternativeSpellings;

            if (entry.KebList.Any())
            {
                foreach (string keb in entry.KebList)
                {
                    JMnedictResult result = new();
                    string key = Kana.KatakanaToHiraganaConverter(keb);

                    result.PrimarySpelling = keb;
                    result.Readings = entry.RebList;

                    foreach (Trans trans in entry.TransList)
                    {
                        result.Definitions.AddRange(trans.TransDetList);
                        result.NameTypes.AddRange(trans.NameTypeList);
                        // result.RelatedTerms.AddRange(trans.XRefList);
                    }
                    resultList.TryAdd(key, result);
                }

                alternativeSpellings = resultList.Keys.ToList();

                foreach (KeyValuePair<string, JMnedictResult> item in resultList)
                {
                    foreach (string s in alternativeSpellings)
                    {
                        if (item.Key != s)
                        {
                            item.Value.AlternativeSpellings.Add(s);
                        }
                    }
                }
            }

            else
            {
                foreach (string reb in entry.RebList)
                {
                    string key = Kana.KatakanaToHiraganaConverter(reb);

                    if (resultList.ContainsKey(key))
                        continue;

                    JMnedictResult result = new();

                    result.PrimarySpelling = reb;

                    foreach (Trans trans in entry.TransList)
                    {
                        result.Definitions.AddRange(trans.TransDetList);

                        result.Definitions.AddRange(trans.NameTypeList);

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

                if (jMnedictDictionary.TryGetValue(key, out List<JMnedictResult> tempList))
                    tempList.Add(rl.Value);
                else
                    tempList = new() { rl.Value };

                jMnedictDictionary[key] = tempList;
            }
        }
    }
}
