using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class JMNeDictBuilder
    {
        public static void BuildDictionary(EdictEntry entry, Dictionary<string, List<EdictResult>> jMnedictDictionary)
        {
            Dictionary<string, EdictResult> resultList = new();
            List<string> alternativeSpellings;

            if (entry.KEleList.Any())
            {
                foreach (KEle kEle in entry.KEleList)
                {
                    EdictResult result = new();
                    string key = kEle.Keb;

                    result.PrimarySpelling = key;

                    foreach (REle rEle in entry.REleList)
                        result.Readings.Add(rEle.Reb);

                    foreach (Trans trans in entry.TransList)
                    {
                        result.DefinitionsList.Add((trans.TransDetList, new List<string>(), new List<string>()));
                        result.RelatedTerms.AddRange(trans.XRefList);
                        result.TypeList.Add(trans.NameTypeList);
                    }
                    resultList.Add(key, result);
                }

                alternativeSpellings = resultList.Keys.ToList();

                foreach (KeyValuePair<string, EdictResult> item in resultList)
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
                foreach (REle rEle in entry.REleList)
                {
                    string key = Kana.KatakanaToHiraganaConverter(rEle.Reb);

                    if (resultList.TryGetValue(key, out var previousResult))
                    {
                        previousResult.KanaSpellings.Add(rEle.Reb);
                        continue;
                    }

                    EdictResult result = new();

                    result.KanaSpellings.Add(rEle.Reb);
                    result.PrimarySpelling = rEle.Reb;

                    foreach (Trans trans in entry.TransList)
                    {
                        result.DefinitionsList.Add((trans.TransDetList, new List<string>(), new List<string>()));
                        result.RelatedTerms.AddRange(trans.XRefList);
                        result.TypeList.Add(trans.NameTypeList);
                    }
                    resultList.Add(key, result);
                }
            }


            foreach (KeyValuePair<string, EdictResult> rl in resultList)
            {
                rl.Value.Id = entry.Id;
                string key = rl.Key;

                if (jMnedictDictionary.TryGetValue(key, out List<EdictResult> tempList))
                    tempList.Add(rl.Value);
                else
                    tempList = new() { rl.Value };

                jMnedictDictionary[key] = tempList;
            }
        }
    }
}
