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
                    ProcessSense(result, sense);
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
                    ProcessSense(result, sense);
                }
            }

            resultList.Add(key, result);
        }

        foreach (KeyValuePair<string, JMdictResult> rl in resultList)
        {
            rl.Value.Readings = TrimList(rl.Value.Readings);
            rl.Value.AlternativeSpellings = TrimList(rl.Value.AlternativeSpellings);
            rl.Value.POrthographyInfoList = TrimList(rl.Value.POrthographyInfoList);
            rl.Value.DefinitionInfo = TrimList(rl.Value.DefinitionInfo!)!;
            rl.Value.Definitions = TrimListOfLists(rl.Value.Definitions!)!;
            rl.Value.RRestrictions = TrimListOfLists(rl.Value.RRestrictions);
            rl.Value.KRestrictions = TrimListOfLists(rl.Value.KRestrictions);
            rl.Value.Dialects = TrimListOfLists(rl.Value.Dialects);
            rl.Value.MiscList = TrimListOfLists(rl.Value.MiscList);
            rl.Value.AOrthographyInfoList = TrimListOfLists(rl.Value.AOrthographyInfoList);
            rl.Value.ROrthographyInfoList = TrimListOfLists(rl.Value.ROrthographyInfoList);
            rl.Value.FieldList = TrimListOfLists(rl.Value.FieldList);
            rl.Value.WordClasses = TrimListOfLists(rl.Value.WordClasses);
            rl.Value.RelatedTerms = TrimListOfLists(rl.Value.RelatedTerms);
            rl.Value.Antonyms = TrimListOfLists(rl.Value.Antonyms);
            rl.Value.LoanwordEtymology = TrimListOfLists(rl.Value.LoanwordEtymology);

            rl.Value.Id = entry.Id;
            string key = Kana.KatakanaToHiraganaConverter(rl.Key);

            if (jMdictDictionary.TryGetValue(key, out List<IResult>? tempResultList))
                tempResultList.Add(rl.Value);
            else
                tempResultList = new() { rl.Value };

            jMdictDictionary[key] = tempResultList;
        }
    }

    private static void ProcessSense(JMdictResult jmdictResult, Sense sense)
    {
        jmdictResult.Definitions.Add(sense.GlossList);
        jmdictResult.RRestrictions!.Add(sense.StagRList.Any() ? sense.StagRList : null);
        jmdictResult.KRestrictions!.Add(sense.StagKList.Any() ? sense.StagKList : null);
        jmdictResult.WordClasses!.Add(sense.PosList.Any() ? sense.PosList : null);
        jmdictResult.FieldList!.Add(sense.FieldList.Any() ? sense.FieldList : null);
        jmdictResult.MiscList!.Add(sense.MiscList.Any() ? sense.MiscList : null);
        jmdictResult.Dialects!.Add(sense.DialList.Any() ? sense.DialList : null);
        jmdictResult.DefinitionInfo!.Add(sense.SInf);
        jmdictResult.RelatedTerms!.Add(sense.XRefList.Any() ? sense.XRefList : null);
        jmdictResult.Antonyms!.Add(sense.AntList.Any() ? sense.AntList : null);
        jmdictResult.LoanwordEtymology!.Add(sense.LSourceList.Any() ? sense.LSourceList : null);
    }

    private static List<List<T>?>? TrimListOfLists<T>(List<List<T>?>? listOfLists)
    {
        List<List<T>?>? listOfListClone = listOfLists;

        if (!listOfListClone!.Any() || listOfListClone!.All(l => l == null || !l.Any()))
            listOfListClone = null;
        else
        {
            listOfListClone!.TrimExcess();

            int counter = listOfListClone.Count;
            for (int i = 0; i < counter; i++)
            {
                listOfListClone[i]?.TrimExcess();
            }
        }

        return listOfListClone;
    }

    private static List<string>? TrimList(List<string>? list)
    {
        List<string>? listClone = list;

        if (!listClone!.Any() || listClone!.All(string.IsNullOrEmpty))
            listClone = null;
        else
            listClone!.TrimExcess();

        return listClone;
    }
}
