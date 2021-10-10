using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    internal static class JMDictBuilder
    {
        public static void BuildDictionary(JMdictEntry entry, Dictionary<string, List<IResult>> jMdictDictionary)
        {
            // entry (k_ele*, r_ele+, sense+)
            // k_ele (keb, ke_inf*, ke_pri*)
            // r_ele (reb, re_restr*, re_inf*, re_pri*)
            // sense (stagk*, stagr*, pos*, xref*, ant*, field*, misc*, s_inf*, dial*, gloss*)

            Dictionary<string, JMdictResult> resultList = new();
            List<string> alternativeSpellings;

            foreach (KEle kEle in entry.KEleList)
            {
                JMdictResult result = new();
                string key = kEle.Keb;

                result.PrimarySpelling = key;

                result.POrthographyInfoList = kEle.KeInfList;
                //result.PriorityList = kEle.KePriList;

                foreach (REle rEle in entry.REleList)
                {
                    if (!rEle.ReRestrList.Any() || rEle.ReRestrList.Contains(key))
                    {
                        result.Readings.Add(rEle.Reb);
                        result.ROrthographyInfoList.Add(rEle.ReInfList);
                    }
                }

                foreach (Sense sense in entry.SenseList)
                {
                    if ((!sense.StagKList.Any() && !sense.StagRList.Any())
                        || sense.StagKList.Contains(key)
                        || sense.StagRList.Intersect(result.Readings).Any())
                    {
                        result.Definitions.Add(sense.GlossList);
                        result.RRestrictions.Add(sense.StagRList);
                        result.KRestrictions.Add(sense.StagKList);
                        result.WordClasses.Add(sense.PosList);
                        result.TypeList.Add(sense.FieldList);
                        result.MiscList.Add(sense.MiscList);
                        result.Dialects.AddRange(sense.DialList);
                        result.SpellingInfo.Add(sense.SInf);
                        // result.RelatedTerms.AddRange(sense.XRefList);
                        // result.Antonyms.AddRange(sense.AntList);
                    }
                }

                resultList.Add(key, result);
            }

            alternativeSpellings = resultList.Keys.ToList();

            foreach (KeyValuePair<string, JMdictResult> item in resultList)
            {
                foreach (string spelling in alternativeSpellings)
                {
                    if (item.Key != spelling)
                    {
                        item.Value.AlternativeSpellings.Add(spelling);

                        resultList.TryGetValue(spelling, out var tempResult);
                        Debug.Assert(tempResult != null, nameof(tempResult) + " != null");

                        item.Value.AOrthographyInfoList.Add(tempResult.POrthographyInfoList);
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

                JMdictResult result = new();

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
                    {
                        result.Readings = mainEntry.Readings;
                        result.AOrthographyInfoList = mainEntry.AOrthographyInfoList;
                        result.ROrthographyInfoList = mainEntry.ROrthographyInfoList;
                    }
                }

                else
                    result.PrimarySpelling = rEle.Reb;

                foreach (Sense sense in entry.SenseList)
                {
                    if ((!sense.StagKList.Any() && !sense.StagRList.Any())
                        || sense.StagRList.Contains(rEle.Reb)
                        || sense.StagKList.Contains(result.PrimarySpelling)
                        || sense.StagKList.Intersect(result.AlternativeSpellings).Any())
                    {
                        result.Definitions.Add(sense.GlossList);
                        result.RRestrictions.Add(sense.StagRList);
                        result.KRestrictions.Add(sense.StagKList);
                        result.WordClasses.Add(sense.PosList);
                        result.TypeList.Add(sense.FieldList);
                        result.MiscList.Add(sense.MiscList);
                        result.Dialects.AddRange(sense.DialList);
                        result.SpellingInfo.Add(sense.SInf);
                        // result.RelatedTerms.AddRange(sense.XRefList);
                        // result.Antonyms.AddRange(sense.AntList);
                    }
                }

                resultList.Add(key, result);
            }

            //if(!alternativeSpellings.Any())
            //{
            //    foreach (KeyValuePair<string, EdictResult> item in resultList)
            //    {
            //        foreach (string spelling in resultList.Keys)
            //        {
            //            if (item.Key != spelling)
            //            {
            //                item.Value.AlternativeSpellings.Add(spelling);
            //            }
            //        }
            //    }
            //}

            foreach (KeyValuePair<string, JMdictResult> rl in resultList)
            {
                if (!rl.Value.AlternativeSpellings.Any())
                    rl.Value.AlternativeSpellings = null;

                if (!rl.Value.Definitions.Any())
                    rl.Value.Definitions = null;

                if (!rl.Value.RRestrictions.Any())
                    rl.Value.RRestrictions = null;

                if (!rl.Value.KRestrictions.Any())
                    rl.Value.KRestrictions = null;

                if (!rl.Value.Dialects.Any())
                    rl.Value.Dialects = null;

                if (!rl.Value.KanaSpellings.Any())
                    rl.Value.KanaSpellings = null;

                if (!rl.Value.MiscList.Any())
                    rl.Value.MiscList = null;

                if (!rl.Value.POrthographyInfoList.Any())
                    rl.Value.POrthographyInfoList = null;

                if (!rl.Value.AOrthographyInfoList.Any())
                    rl.Value.AOrthographyInfoList = null;

                if (!rl.Value.ROrthographyInfoList.Any())
                    rl.Value.ROrthographyInfoList = null;

                if (!rl.Value.SpellingInfo.Any())
                    rl.Value.SpellingInfo = null;

                if (!rl.Value.TypeList.Any())
                    rl.Value.TypeList = null;

                if (!rl.Value.WordClasses.Any())
                    rl.Value.WordClasses = null;

                rl.Value.Id = entry.Id;
                string key = rl.Key;

                if (jMdictDictionary.TryGetValue(key, out List<IResult> tempList))
                    tempList.Add(rl.Value);
                else
                    tempList = new() { rl.Value };

                jMdictDictionary[key] = tempList;
            }
        }
    }
}