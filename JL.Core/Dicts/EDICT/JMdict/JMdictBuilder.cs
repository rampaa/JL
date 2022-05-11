namespace JL.Core.Dicts.EDICT.JMdict;

internal static class JMdictBuilder
{
    public static void BuildDictionary(JMdictEntry entry, Dictionary<string, List<IResult>> jMdictDictionary)
    {
        // entry (k_ele*, r_ele+, sense+)
        // k_ele (keb, ke_inf*, ke_pri*)
        // r_ele (reb, re_restr*, re_inf*, re_pri*)
        // sense (stagk*, stagr*, pos*, xref*, ant*, field*, misc*, s_inf*, dial*, gloss*)

        Dictionary<string, JMdictResult> resultList = new();

        int kEleListCount = entry.KanjiElements.Count;
        for (int i = 0; i < kEleListCount; i++)
        {
            KanjiElement kanjiElement = entry.KanjiElements[i];

            JMdictResult result = new();
            string key = kanjiElement.Keb!;

            result.PrimarySpelling = key;

            result.POrthographyInfoList = kanjiElement.KeInfList;
            //result.PriorityList = kanjiElement.KePriList;

            int lREleListCount = entry.ReadingElements.Count;
            for (int j = 0; j < lREleListCount; j++)
            {
                ReadingElement readingElement = entry.ReadingElements[j];

                if (!readingElement.ReRestrList.Any() || readingElement.ReRestrList.Contains(key))
                {
                    result.Readings?.Add(readingElement.Reb);
                    result.ROrthographyInfoList?.Add(readingElement.ReInfList);
                }
            }

            int senseListCount = entry.SenseList.Count;
            for (int j = 0; j < senseListCount; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((!sense.StagKList.Any() && !sense.StagRList.Any())
                    || sense.StagKList.Contains(key)
                    || sense.StagRList.Intersect(result.Readings!).Any())
                {
                    result.Definitions!.Add(sense.GlossList);
                    result.RRestrictions!.Add(sense.StagRList);
                    result.KRestrictions!.Add(sense.StagKList);
                    result.WordClasses!.Add(sense.PosList);
                    result.FieldList!.Add(sense.FieldList);
                    result.MiscList!.Add(sense.MiscList);
                    result.Dialects!.Add(sense.DialList);
                    result.DefinitionInfo!.Add(sense.SInf);
                    // result.RelatedTerms.AddRange(sense.XRefList);
                    // result.Antonyms.AddRange(sense.AntList);
                }
            }

            resultList.Add(key, result);
        }

        List<string> alternativeSpellings = resultList.Keys.ToList();

        foreach ((string key, JMdictResult result) in resultList)
        {
            int alternativeSpellingsCount = alternativeSpellings.Count;
            for (int i = 0; i < alternativeSpellingsCount; i++)
            {
                string spelling = alternativeSpellings[i];

                if (key != spelling)
                {
                    result.AlternativeSpellings!.Add(spelling);

                    if (resultList.TryGetValue(spelling, out JMdictResult? tempResult))
                    {
                        result.AOrthographyInfoList!.Add(tempResult.POrthographyInfoList);
                    }
                }
            }
        }

        List<string> allReadings = entry.ReadingElements.Select(rEle => rEle.Reb).ToList();
        List<List<string>> allROrthographyInfoLists = entry.ReadingElements.Select(rEle => rEle.ReInfList).ToList();

        int rEleListCount = entry.ReadingElements.Count;
        for (int i = 0; i < rEleListCount; i++)
        {
            ReadingElement readingElement = entry.ReadingElements[i];

            string key = Kana.KatakanaToHiraganaConverter(readingElement.Reb);

            if (resultList.ContainsKey(key))
            {
                continue;
            }

            JMdictResult result = new()
            {
                AlternativeSpellings = readingElement.ReRestrList.Any()
                    ? readingElement.ReRestrList
                    : new List<string>(alternativeSpellings)
            };

            if (result.AlternativeSpellings.Any())
            {
                result.PrimarySpelling = result.AlternativeSpellings[0];

                result.AlternativeSpellings.RemoveAt(0);

                if (resultList.TryGetValue(result.PrimarySpelling, out JMdictResult? mainEntry))
                {
                    result.Readings = mainEntry.Readings;
                    result.AOrthographyInfoList = mainEntry.AOrthographyInfoList;
                    result.ROrthographyInfoList = mainEntry.ROrthographyInfoList;
                }
            }

            else
            {
                result.PrimarySpelling = readingElement.Reb;
                result.POrthographyInfoList = readingElement.ReInfList;

                result.AlternativeSpellings = allReadings.ToList();
                result.AlternativeSpellings.RemoveAt(i);

                result.AOrthographyInfoList = allROrthographyInfoLists.ToList()!;
                result.AOrthographyInfoList.RemoveAt(i);
            }

            int senseListCount = entry.SenseList.Count;
            for (int j = 0; j < senseListCount; j++)
            {
                Sense sense = entry.SenseList[j];

                if ((!sense.StagKList.Any() && !sense.StagRList.Any())
                    || sense.StagRList.Contains(readingElement.Reb)
                    || sense.StagKList.Contains(result.PrimarySpelling)
                    || sense.StagKList.Intersect(result.AlternativeSpellings).Any())
                {
                    result.Definitions!.Add(sense.GlossList);
                    result.RRestrictions!.Add(sense.StagRList);
                    result.KRestrictions!.Add(sense.StagKList);
                    result.WordClasses!.Add(sense.PosList);
                    result.FieldList!.Add(sense.FieldList);
                    result.MiscList!.Add(sense.MiscList);
                    result.Dialects!.Add(sense.DialList);
                    result.DefinitionInfo!.Add(sense.SInf);
                    // result.RelatedTerms.AddRange(sense.XRefList);
                    // result.Antonyms.AddRange(sense.AntList);
                }
            }

            resultList.Add(key, result);
        }

        foreach (KeyValuePair<string, JMdictResult> rl in resultList)
        {
            if (!rl.Value.Readings!.Any() || rl.Value.Readings!.All(string.IsNullOrEmpty))
                rl.Value.Readings = null;
            else
                rl.Value.Readings!.TrimExcess();

            if (!rl.Value.AlternativeSpellings!.Any())
                rl.Value.AlternativeSpellings = null;
            else
                rl.Value.AlternativeSpellings!.TrimExcess();

            if (!rl.Value.RRestrictions!.Any() || rl.Value.RRestrictions!.All(l => l == null || !l.Any()))
                rl.Value.RRestrictions = null;
            else
            {
                rl.Value.RRestrictions!.TrimExcess();

                int counter = rl.Value.RRestrictions.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.RRestrictions[i]?.TrimExcess();
                }
            }

            if (!rl.Value.KRestrictions!.Any() || rl.Value.KRestrictions!.All(l => l == null || !l.Any()))
                rl.Value.KRestrictions = null;
            else
            {
                rl.Value.KRestrictions!.TrimExcess();

                int counter = rl.Value.KRestrictions.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.KRestrictions[i]?.TrimExcess();
                }
            }

            if (!rl.Value.Dialects!.Any() || !rl.Value.Dialects!.All(l => l == null || !l.Any()))
                rl.Value.Dialects = null;
            else
            {
                rl.Value.Dialects!.TrimExcess();

                int counter = rl.Value.Dialects.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.Dialects[i]?.TrimExcess();
                }
            }

            if (!rl.Value.MiscList!.Any() || rl.Value.MiscList!.All(l => l == null || !l.Any()))
                rl.Value.MiscList = null;
            else
            {
                rl.Value.MiscList!.TrimExcess();

                int counter = rl.Value.MiscList.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.MiscList[i]?.TrimExcess();
                }
            }

            if (!rl.Value.POrthographyInfoList!.Any() || rl.Value.POrthographyInfoList!.All(string.IsNullOrEmpty))
                rl.Value.POrthographyInfoList = null;
            else
                rl.Value.POrthographyInfoList!.TrimExcess();

            if (!rl.Value.AOrthographyInfoList!.Any() || rl.Value.AOrthographyInfoList!.All(l => l == null || !l.Any()))
                rl.Value.AOrthographyInfoList = null;
            else
            {
                rl.Value.AOrthographyInfoList!.TrimExcess();

                int counter = rl.Value.AOrthographyInfoList.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.AOrthographyInfoList[i]?.TrimExcess();
                }
            }

            if (!rl.Value.ROrthographyInfoList!.Any() || rl.Value.ROrthographyInfoList!.All(l => l == null || !l.Any()))
                rl.Value.ROrthographyInfoList = null;
            else
            {
                rl.Value.ROrthographyInfoList!.TrimExcess();

                int counter = rl.Value.ROrthographyInfoList.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.ROrthographyInfoList[i]?.TrimExcess();
                }
            }

            if (!rl.Value.Definitions!.Any() || rl.Value.Definitions!.All(l => !l.Any()))
                rl.Value.Definitions = null;
            else
            {
                rl.Value.Definitions!.TrimExcess();

                int counter = rl.Value.Definitions.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.Definitions[i].TrimExcess();
                }
            }

            if (!rl.Value.DefinitionInfo!.Any() || rl.Value.DefinitionInfo!.All(s => s == null || string.IsNullOrEmpty(s)))
                rl.Value.DefinitionInfo = null;
            else
                rl.Value.DefinitionInfo!.TrimExcess();

            if (!rl.Value.FieldList!.Any() || rl.Value.FieldList!.All(l => l == null || !l.Any()))
                rl.Value.FieldList = null;
            else
            {
                rl.Value.FieldList!.TrimExcess();

                int counter = rl.Value.FieldList.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.FieldList[i]?.TrimExcess();
                }
            }

            if (!rl.Value.WordClasses!.Any() || rl.Value.WordClasses!.All(l => l == null || !l.Any()))
                rl.Value.WordClasses = null;
            else
            {
                rl.Value.WordClasses!.TrimExcess();

                int counter = rl.Value.WordClasses.Count;
                for (int i = 0; i < counter; i++)
                {
                    rl.Value.WordClasses[i]?.TrimExcess();
                }
            }

            rl.Value.Id = entry.Id;
            string key = Kana.KatakanaToHiraganaConverter(rl.Key);

            if (jMdictDictionary.TryGetValue(key, out List<IResult>? tempResultList))
                tempResultList.Add(rl.Value);
            else
                tempResultList = new() { rl.Value };

            jMdictDictionary[key] = tempResultList;
        }
    }
}
