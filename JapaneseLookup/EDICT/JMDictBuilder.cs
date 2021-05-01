using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class JMDictBuilder
    {
        public static void DictionaryBuilder(JMdictEntry entry, Dictionary<string, List<Results>> jMdictDictionary)
        {
            // entry (k_ele*, r_ele+, sense+)
            // k_ele (keb, ke_inf*, ke_pri*)
            // r_ele (reb, re_restr*, re_inf*, re_pri*)
            // sense (stagk*, stagr*, pos*, xref*, ant*, field*, misc*, s_inf*, dial*, gloss*)

            Dictionary<string, Results> resultList = new();
            List<string> alternativeSpellings;

            foreach (KEle kEle in entry.KEleList)
            {
                Results result = new();
                string key = kEle.Keb;

                result.PrimarySpelling = key;

                result.OrthographyInfoList = kEle.KeInfList;
                result.PriorityList = kEle.KePriList;

                foreach (REle rEle in entry.REleList)
                {
                    if (!rEle.ReRestrList.Any() || rEle.ReRestrList.Contains(key))
                        result.Readings.Add(rEle.Reb);
                }

                foreach (Sense sense in entry.SenseList)
                {
                    if ((!sense.StagKList.Any() && !sense.StagRList.Any())
                        || sense.StagKList.Contains(key)
                        || sense.StagRList.Intersect(result.Readings).Any())
                    {
                        result.DefinitionsList.Add((sense.GlossList, sense.StagRList, sense.StagKList));
                        result.WordClasses.Add(sense.PosList);
                        result.RelatedTerms.AddRange(sense.XRefList);
                        result.Antonyms.AddRange(sense.AntList);
                        result.FieldInfoList.AddRange(sense.FieldList);
                        result.MiscList.Add(sense.MiscList);
                        result.Dialects.AddRange(sense.DialList);
                        result.SpellingInfo.Add(sense.SInf);
                    }
                }
                resultList.Add(key, result);
            }

            alternativeSpellings = resultList.Keys.ToList();

            foreach (KeyValuePair<string, Results> item in resultList)
            {
                foreach (string s in alternativeSpellings)
                {
                    if (item.Key != s)
                    {
                        item.Value.AlternativeSpellings.Add(s);
                    }
                }
            }

            foreach (REle rEle in entry.REleList)
            {
                string key = Kana.KatakanaToHiraganaConverter(rEle.Reb);

                if (resultList.TryGetValue(key, out var previousResult))
                {
                    previousResult.KanaSpellings.Add(rEle.Reb);
                    continue;
                }

                Results result = new();

                result.KanaSpellings.Add(rEle.Reb);

                if (rEle.ReRestrList.Any())
                    result.AlternativeSpellings = rEle.ReRestrList;
                else
                    result.AlternativeSpellings = new List<string>(alternativeSpellings);

                if (result.AlternativeSpellings.Any())
                {
                    result.PrimarySpelling = result.AlternativeSpellings[0];

                    result.AlternativeSpellings.Remove(result.PrimarySpelling);

                    if (resultList.TryGetValue(result.PrimarySpelling, out var mainEntry))
                        result.Readings = mainEntry.Readings;
                }

                foreach (Sense sense in entry.SenseList)
                {
                    if((!sense.StagKList.Any() && !sense.StagRList.Any())
                        || sense.StagRList.Contains(rEle.Reb) 
                        || sense.StagKList.Contains(result.PrimarySpelling)
                        || sense.StagKList.Intersect(result.AlternativeSpellings).Any())
                    {
                        result.DefinitionsList.Add((sense.GlossList, sense.StagRList, sense.StagKList));
                        result.WordClasses.Add(sense.PosList);
                        result.RelatedTerms.AddRange(sense.XRefList);
                        result.Antonyms.AddRange(sense.AntList);
                        result.FieldInfoList.AddRange(sense.FieldList);
                        result.MiscList.Add(sense.MiscList);
                        result.Dialects.AddRange(sense.DialList);
                        result.SpellingInfo.Add(sense.SInf);
                    }
                }
                resultList.Add(key, result);
            }

            foreach (KeyValuePair<string, Results> rl in resultList)
            {
                rl.Value.Id = entry.Id;
                string key = rl.Key;

                if (jMdictDictionary.TryGetValue(key, out List<Results> tempList))
                    tempList.Add(rl.Value);
                else
                    tempList = new() { rl.Value };

                jMdictDictionary[key] = tempList;
            }
        }
    }
}
