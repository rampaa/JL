using System.Diagnostics;
using System.Text;
using JL.Core.Deconjugation;
using JL.Core.Dicts;
using JL.Core.Dicts.CustomDict;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Dicts.EPWING;
using JL.Core.Dicts.EPWING.EpwingNazeka;
using JL.Core.Frequency;
using JL.Core.PoS;
using JL.Core.Utilities;

namespace JL.Core.Lookup
{
    public static class Lookup
    {
        private static DateTime s_lastLookupTime;

        public static List<Dictionary<LookupResult, List<string>>> LookupText(string text)
        {
            DateTime preciseTimeNow = new(Stopwatch.GetTimestamp());
            if ((preciseTimeNow - s_lastLookupTime).TotalMilliseconds < Storage.Frontend.CoreConfig.LookupRate) return null;
            s_lastLookupTime = preciseTimeNow;

            if (Storage.Frontend.CoreConfig.KanjiMode)
            {
                return KanjiResultBuilder(GetKanjidicResults(text, DictType.Kanjidic));
            }

            Dictionary<string, IntermediaryResult> jMdictResults = new();
            Dictionary<string, IntermediaryResult> jMnedictResults = new();
            List<Dictionary<string, IntermediaryResult>> epwingWordResultsList = new();
            List<Dictionary<string, IntermediaryResult>> epwingNazekaWordResultsList = new();
            Dictionary<string, IntermediaryResult> kanjiResult = new();
            Dictionary<string, IntermediaryResult> customWordResults = new();
            Dictionary<string, IntermediaryResult> customNameResults = new();

            List<string> textInHiraganaList = new();
            List<HashSet<Form>> deconjugationResultsList = new();

            for (int i = 0; i < text.Length; i++)
            {
                string textInHiragana = Kana.KatakanaToHiraganaConverter(text[..^i]);
                textInHiraganaList.Add(textInHiragana);

                deconjugationResultsList.Add(Deconjugator.Deconjugate(textInHiragana));
            }

            foreach ((DictType dictType, Dict dict) in Storage.Dicts.ToList())
            {
                if (dict.Active)
                {
                    switch (dictType)
                    {
                        case DictType.JMdict:
                            jMdictResults = GetWordResults(text, textInHiraganaList, deconjugationResultsList, dictType);
                            break;
                        case DictType.JMnedict:
                            jMnedictResults = GetNameResults(text, textInHiraganaList, dictType);
                            break;
                        case DictType.Kanjidic:
                            kanjiResult = GetKanjidicResults(text, DictType.Kanjidic);
                            break;
                        case DictType.Kenkyuusha:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Daijirin:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Daijisen:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Koujien:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Meikyou:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Gakken:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Kotowaza:
                            epwingWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.CustomWordDictionary:
                            customWordResults = GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType);
                            break;
                        case DictType.CustomNameDictionary:
                            customNameResults = GetNameResults(text, textInHiraganaList, dictType);
                            break;
                        case DictType.DaijirinNazeka:
                            epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.KenkyuushaNazeka:
                            epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.ShinmeikaiNazeka:
                            epwingNazekaWordResultsList.Add(GetWordResults(text, textInHiraganaList,
                                deconjugationResultsList, dictType));
                            break;
                        case DictType.Kanjium:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                    }
                }
            }

            List<Dictionary<LookupResult, List<string>>> lookupResults = new();

            if (jMdictResults.Any())
                lookupResults.AddRange(JmdictResultBuilder(jMdictResults));

            if (epwingWordResultsList.Any())
            {
                for (int i = 0; i < epwingWordResultsList.Count; i++)
                {
                    lookupResults.AddRange(EpwingResultBuilder(epwingWordResultsList[i]));
                }
            }

            if (jMnedictResults.Any())
                lookupResults.AddRange(JmnedictResultBuilder(jMnedictResults));

            if (kanjiResult.Any())
                lookupResults.AddRange(KanjiResultBuilder(kanjiResult));

            if (customWordResults.Any())
                lookupResults.AddRange(CustomWordResultBuilder(customWordResults));

            if (customNameResults.Any())
                lookupResults.AddRange(CustomNameResultBuilder(customNameResults));

            if (epwingNazekaWordResultsList.Any())
            {
                for (int i = 0; i < epwingNazekaWordResultsList.Count; i++)
                {
                    lookupResults.AddRange(EpwingNazekaResultBuilder(epwingNazekaWordResultsList[i]));
                }
            }

            if (lookupResults.Any())
                lookupResults = SortLookupResults(lookupResults);

            return lookupResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> SortLookupResults(
            List<Dictionary<LookupResult, List<string>>> lookupResults)
        {
            List<Dictionary<LookupResult, List<string>>> sortedLookupResults = lookupResults
                .OrderByDescending(dict => dict[LookupResult.FoundForm][0].Length)
                .ThenBy(dict => Enum.TryParse(dict[LookupResult.DictType][0], out DictType dictType)
                    ? Storage.Dicts[dictType].Priority
                    : int.MaxValue)
                .ThenBy(dict => Convert.ToInt32(dict[LookupResult.Frequency][0])).ToList();

            string longestFoundForm = sortedLookupResults.First()[LookupResult.FoundForm][0];

            sortedLookupResults = sortedLookupResults
                .OrderByDescending(dict => longestFoundForm == dict[LookupResult.FoundSpelling][0])
                .ThenByDescending(dict => dict[LookupResult.Readings].Contains(longestFoundForm))
                .ToList();

            return sortedLookupResults.ToList();
        }

        private static (bool tryLongVowelConversion, int succAttempt) GetWordResultsHelper(DictType dictType,
            Dictionary<string, IntermediaryResult> results,
            HashSet<Form> deconjugationList,
            string foundForm,
            string textInHiragana,
            int succAttempt)
        {
            Dictionary<string, List<IResult>> dictionary = Storage.Dicts[dictType].Contents;

            bool tryLongVowelConversion = true;

            if (dictionary.TryGetValue(textInHiragana, out List<IResult> tempResult))
            {
                results.TryAdd(textInHiragana,
                    new IntermediaryResult(tempResult, new List<List<string>> { new() }, foundForm,
                        dictType));
                tryLongVowelConversion = false;
            }

            if (succAttempt < 3)
            {
                foreach (Form deconjugationResult in deconjugationList)
                {
                    string lastTag = "";
                    if (deconjugationResult.Tags.Count > 0)
                        lastTag = deconjugationResult.Tags.Last();

                    if (dictionary.TryGetValue(deconjugationResult.Text, out List<IResult> dictResults))
                    {
                        List<IResult> resultsList = new();

                        switch (dictType)
                        {
                            case DictType.JMdict:
                                {
                                    int dictResultsCount = dictResults.Count;
                                    for (int i = 0; i < dictResultsCount; i++)
                                    {
                                        var dictResult = (JMdictResult)dictResults[i];

                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.SelectMany(pos => pos).Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                    }
                                }
                                break;

                            case DictType.CustomWordDictionary:
                                {
                                    int dictResultsCount = dictResults.Count;
                                    for (int i = 0; i < dictResultsCount; i++)
                                    {
                                        var dictResult = (CustomWordEntry)dictResults[i];

                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }
                                    }
                                }
                                break;

                            case DictType.Daijirin:
                            case DictType.Daijisen:
                            case DictType.Gakken:
                            case DictType.Kenkyuusha:
                            case DictType.Kotowaza:
                            case DictType.Koujien:
                            case DictType.Meikyou:
                                {
                                    int dictResultsCount = dictResults.Count;
                                    for (int i = 0; i < dictResultsCount; i++)
                                    {
                                        var dictResult = (EpwingResult)dictResults[i];

                                        if (deconjugationResult.Tags.Count == 0 || dictResult.WordClasses.Contains(lastTag))
                                        {
                                            resultsList.Add(dictResult);
                                        }

                                        else if (Storage.WcDict.TryGetValue(deconjugationResult.Text, out List<JmdictWc> jmdictWcResults))
                                        {
                                            for (int j = 0; j < jmdictWcResults.Count; j++)
                                            {
                                                JmdictWc jmdictWcResult = jmdictWcResults[j];

                                                if (dictResult.PrimarySpelling == jmdictWcResult.Spelling
                                                    && (jmdictWcResult.Readings?.Contains(dictResult.Reading)
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                                {
                                                    if (jmdictWcResult.WordClasses.Contains(lastTag))
                                                    {
                                                        resultsList.Add(dictResult);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case DictType.DaijirinNazeka:
                            case DictType.KenkyuushaNazeka:
                            case DictType.ShinmeikaiNazeka:
                                {
                                    int dictResultsCount = dictResults.Count;
                                    for (int i = 0; i < dictResultsCount; i++)
                                    {
                                        var dictResult = (EpwingNazekaResult)dictResults[i];

                                        if (deconjugationResult.Tags.Count == 0)
                                        {
                                            resultsList.Add(dictResult);
                                        }

                                        else if (Storage.WcDict.TryGetValue(deconjugationResult.Text, out List<JmdictWc> jmdictWcResults))
                                        {
                                            for (int j = 0; j < jmdictWcResults.Count; j++)
                                            {
                                                JmdictWc jmdictWcResult = jmdictWcResults[j];

                                                if (dictResult.PrimarySpelling == jmdictWcResult.Spelling
                                                    && (jmdictWcResult.Readings?.Contains(dictResult.Reading)
                                                    ?? string.IsNullOrEmpty(dictResult.Reading)))
                                                {
                                                    if (jmdictWcResult.WordClasses.Contains(lastTag))
                                                    {
                                                        resultsList.Add(dictResult);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case DictType.Kanjium:
                                break;

                            default:
                                throw new ArgumentOutOfRangeException(null, "Invalid DictType");
                        }

                        if (resultsList.Any())
                        {
                            if (results.TryGetValue(deconjugationResult.Text, out IntermediaryResult r))
                            {
                                if (r.FoundForm == deconjugationResult.OriginalText)
                                    r.ProcessListList.Add(deconjugationResult.Process);
                            }
                            else
                            {
                                results.Add(deconjugationResult.Text,
                                    new IntermediaryResult(resultsList, new List<List<string>> { deconjugationResult.Process },
                                        foundForm,
                                        dictType)
                                );
                            }

                            ++succAttempt;
                            tryLongVowelConversion = false;
                        }
                    }
                }
            }

            return (tryLongVowelConversion, succAttempt);
        }

        private static Dictionary<string, IntermediaryResult> GetWordResults(string text,
            List<string> textInHiraganaList, List<HashSet<Form>> deconjugationResultsList, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> results = new();

            int succAttempt = 0;

            for (int i = 0; i < text.Length; i++)
            {
                (bool tryLongVowelConversion, succAttempt) = GetWordResultsHelper(dictType, results,
                    deconjugationResultsList[i], text[..^i], textInHiraganaList[i], succAttempt);

                if (tryLongVowelConversion && textInHiraganaList[i].Contains('ー') &&
                    textInHiraganaList[i][0] != 'ー')
                {
                    List<string> textWithoutLongVowelMarkList = Kana.LongVowelMarkConverter(textInHiraganaList[i]);

                    for (int j = 0; j < textWithoutLongVowelMarkList.Count; j++)
                    {
                        succAttempt = GetWordResultsHelper(dictType, results, deconjugationResultsList[i],
                            text[..^i], textWithoutLongVowelMarkList[j], succAttempt).succAttempt;
                    }
                }
            }

            return results;
        }

        private static Dictionary<string, IntermediaryResult> GetNameResults(string text,
            List<string> textInHiraganaList, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> nameResults = new();

            for (int i = 0; i < text.Length; i++)
            {
                if (Storage.Dicts[dictType].Contents
                    .TryGetValue(textInHiraganaList[i], out List<IResult> result))
                {
                    nameResults.TryAdd(textInHiraganaList[i],
                        new IntermediaryResult(result, new List<List<string>>(), text[..^i], dictType));
                }
            }

            return nameResults;
        }

        private static Dictionary<string, IntermediaryResult> GetKanjidicResults(string text, DictType dictType)
        {
            Dictionary<string, IntermediaryResult> kanjiResults = new();

            if (Storage.Dicts[DictType.Kanjidic].Contents.TryGetValue(
                text.UnicodeIterator().DefaultIfEmpty(string.Empty).First(), out List<IResult> result))
            {
                kanjiResults.Add(text.UnicodeIterator().First(),
                    new IntermediaryResult(result, new List<List<string>>(), text.UnicodeIterator().First(),
                        dictType));
            }

            return kanjiResults;
        }

        private static List<Dictionary<LookupResult, List<string>>> JmdictResultBuilder(
            Dictionary<string, IntermediaryResult> jmdictResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (IntermediaryResult wordResult in jmdictResults.Values.ToList())
            {
                int resultListCount = wordResult.ResultsList.Count;
                for (int i = 0; i < resultListCount; i++)
                {
                    var jMDictResult = (JMdictResult)wordResult.ResultsList[i];

                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { jMDictResult.PrimarySpelling };

                    //var kanaSpellings = jMDictResult.KanaSpellings ?? new List<string>();

                    List<string> readings = jMDictResult.Readings ?? new();

                    var foundForm = new List<string> { wordResult.FoundForm };

                    var edictID = new List<string> { jMDictResult.Id };

                    List<string> alternativeSpellings = jMDictResult.AlternativeSpellings ?? new();

                    List<string> process = ProcessProcess(wordResult);

                    List<string> frequency = GetJMDictFreq(jMDictResult);

                    var dictType = new List<string> { wordResult.DictType.ToString() };

                    var definitions = new List<string> { BuildJmdictDefinition(jMDictResult) };

                    List<string> pOrthographyInfoList = jMDictResult.POrthographyInfoList ?? new();

                    List<List<string>> rLists = jMDictResult.ROrthographyInfoList ?? new();
                    List<List<string>> aLists = jMDictResult.AOrthographyInfoList ?? new();
                    List<string> rOrthographyInfoList = new();
                    List<string> aOrthographyInfoList = new();

                    for (int j = 0; j < rLists.Count; j++)
                    {
                        StringBuilder formattedROrthographyInfo = new();

                        for (int k = 0; k < rLists[j].Count; k++)
                        {
                            formattedROrthographyInfo.Append(rLists[j][k]);
                            formattedROrthographyInfo.Append(", ");
                        }

                        rOrthographyInfoList.Add(formattedROrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    for (int j = 0; j < aLists.Count; j++)
                    {
                        StringBuilder formattedAOrthographyInfo = new();

                        for (int k = 0; k < aLists[j].Count; k++)
                        {
                            formattedAOrthographyInfo.Append(aLists[j][k]);
                            formattedAOrthographyInfo.Append(", ");
                        }

                        aOrthographyInfoList.Add(formattedAOrthographyInfo.ToString().TrimEnd(", ".ToCharArray()));
                    }

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    //result.Add(LookupResult.KanaSpellings, kanaSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.POrthographyInfoList, pOrthographyInfoList);
                    result.Add(LookupResult.ROrthographyInfoList, rOrthographyInfoList);
                    result.Add(LookupResult.AOrthographyInfoList, aOrthographyInfoList);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> JmnedictResultBuilder(
            Dictionary<string, IntermediaryResult> jmnedictResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (IntermediaryResult nameResult in jmnedictResults.Values.ToList())
            {
                int resultListCount = nameResult.ResultsList.Count;
                for (int i = 0; i < resultListCount; i++)
                {
                    var jMnedictResult = (JMnedictResult)nameResult.ResultsList[i];

                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { jMnedictResult.PrimarySpelling };

                    List<string> readings = jMnedictResult.Readings ?? new List<string>();

                    var foundForm = new List<string> { nameResult.FoundForm };

                    var edictID = new List<string> { jMnedictResult.Id };

                    var dictType = new List<string> { nameResult.DictType.ToString() };

                    List<string> alternativeSpellings = jMnedictResult.AlternativeSpellings ?? new List<string>();

                    var definitions = new List<string> { BuildJmnedictDefinition(jMnedictResult) };

                    result.Add(LookupResult.EdictID, edictID);
                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { Storage.FakeFrequency });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> KanjiResultBuilder(
            Dictionary<string, IntermediaryResult> kanjiResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();
            Dictionary<LookupResult, List<string>> result = new();

            if (!kanjiResults.Any())
                return results;

            List<IResult> iResult = kanjiResults.First().Value.ResultsList;
            KanjiResult kanjiResult = (KanjiResult)iResult.First();

            var dictType = new List<string> { kanjiResults.First().Value.DictType.ToString() };

            result.Add(LookupResult.FoundSpelling, new List<string> { kanjiResults.First().Key });
            result.Add(LookupResult.Definitions, kanjiResult.Meanings);
            result.Add(LookupResult.OnReadings, kanjiResult.OnReadings);
            result.Add(LookupResult.KunReadings, kanjiResult.KunReadings);
            result.Add(LookupResult.Nanori, kanjiResult.Nanori);
            result.Add(LookupResult.StrokeCount, new List<string> { kanjiResult.StrokeCount.ToString() });
            result.Add(LookupResult.Grade, new List<string> { kanjiResult.Grade.ToString() });
            result.Add(LookupResult.Composition, new List<string> { kanjiResult.Composition });
            result.Add(LookupResult.Frequency, new List<string> { kanjiResult.Frequency.ToString() });

            var foundForm = new List<string> { kanjiResults.First().Value.FoundForm };
            result.Add(LookupResult.FoundForm, foundForm);
            result.Add(LookupResult.DictType, dictType);

            List<string> allReadings = new();

            if (kanjiResult.OnReadings != null)
                allReadings.AddRange(kanjiResult.OnReadings);

            if (kanjiResult.KunReadings != null)
                allReadings.AddRange(kanjiResult.KunReadings);

            if (kanjiResult.Nanori != null)
                allReadings.AddRange(kanjiResult.Nanori);

            result.Add(LookupResult.Readings, allReadings);

            results.Add(result);
            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> EpwingResultBuilder(
            Dictionary<string, IntermediaryResult> epwingResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (IntermediaryResult wordResult in epwingResults.Values.ToList())
            {
                int resultListCount = wordResult.ResultsList.Count;
                for (int i = 0; i < resultListCount; i++)
                {
                    var epwingResult = (EpwingResult)wordResult.ResultsList[i];

                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { epwingResult.PrimarySpelling };

                    var reading = new List<string> { epwingResult.Reading };

                    var foundForm = new List<string> { wordResult.FoundForm };

                    List<string> process = ProcessProcess(wordResult);

                    List<string> frequency = GetEpwingFreq(epwingResult);

                    var dictType = new List<string> { wordResult.DictType.ToString() };

                    var definitions = new List<string> { BuildEpwingDefinition(epwingResult.Definitions) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, reading);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> EpwingNazekaResultBuilder(
            Dictionary<string, IntermediaryResult> epwingNazekaResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (IntermediaryResult wordResult in epwingNazekaResults.Values.ToList())
            {
                int resultListCount = wordResult.ResultsList.Count;
                for (int i = 0; i < resultListCount; i++)
                {
                    var epwingResult = (EpwingNazekaResult)wordResult.ResultsList[i];

                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { epwingResult.PrimarySpelling };

                    var reading = new List<string> { epwingResult.Reading };

                    var foundForm = new List<string> { wordResult.FoundForm };

                    List<string> process = ProcessProcess(wordResult);

                    List<string> frequency = GetEpwingNazekaFreq(epwingResult);

                    List<string> alternativeSpellings = epwingResult.AlternativeSpellings ?? new List<string>();

                    var dictType = new List<string> { wordResult.DictType.ToString() };

                    var definitions = new List<string> { BuildEpwingDefinition(epwingResult.Definitions) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Readings, reading);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> CustomWordResultBuilder(
            Dictionary<string, IntermediaryResult> customWordResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (IntermediaryResult wordResult in customWordResults.Values.ToList())
            {
                int wordResultCount = wordResult.ResultsList.Count;
                for (int i = 0; i < wordResultCount; i++)
                {
                    var customWordDictResult = (CustomWordEntry)wordResult.ResultsList[i];
                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { customWordDictResult.PrimarySpelling };

                    List<string> readings = customWordDictResult.Readings != null
                        ? customWordDictResult.Readings.ToList()
                        : new List<string>();

                    var foundForm = new List<string> { wordResult.FoundForm };

                    List<string> alternativeSpellings;

                    if (customWordDictResult.AlternativeSpellings != null)
                        alternativeSpellings = customWordDictResult.AlternativeSpellings.ToList();
                    else
                        alternativeSpellings = new();

                    List<string> process = ProcessProcess(wordResult);

                    List<string> frequency = GetCustomWordFreq(customWordDictResult);
                    if (frequency.First() == Storage.FakeFrequency)
                        frequency = new List<string> { (wordResultCount - i).ToString() };

                    var dictType = new List<string> { wordResult.DictType.ToString() };

                    var definitions = new List<string> { BuildCustomWordDefinition(customWordDictResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);
                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.AlternativeSpellings, alternativeSpellings);
                    result.Add(LookupResult.Process, process);
                    result.Add(LookupResult.Frequency, frequency);
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<Dictionary<LookupResult, List<string>>> CustomNameResultBuilder(
            Dictionary<string, IntermediaryResult> customNameResults)
        {
            List<Dictionary<LookupResult, List<string>>> results = new();

            foreach (KeyValuePair<string, IntermediaryResult> customNameResult in customNameResults.ToList())
            {
                int resultCount = customNameResult.Value.ResultsList.Count;
                for (int i = 0; i < resultCount; i++)
                {
                    var customNameDictResult = (CustomNameEntry)customNameResult.Value.ResultsList[i];
                    Dictionary<LookupResult, List<string>> result = new();

                    var foundSpelling = new List<string> { customNameDictResult.PrimarySpelling };

                    var readings = new List<string> { customNameDictResult.Reading };

                    var foundForm = new List<string> { customNameResult.Value.FoundForm };

                    var dictType = new List<string> { customNameResult.Value.DictType.ToString() };

                    var definitions = new List<string> { BuildCustomNameDefinition(customNameDictResult) };

                    result.Add(LookupResult.FoundSpelling, foundSpelling);
                    result.Add(LookupResult.Readings, readings);
                    result.Add(LookupResult.Definitions, definitions);

                    result.Add(LookupResult.FoundForm, foundForm);
                    result.Add(LookupResult.Frequency, new List<string> { (resultCount - i).ToString() });
                    result.Add(LookupResult.DictType, dictType);

                    results.Add(result);
                }
            }

            return results;
        }

        private static List<string> GetJMDictFreq(JMdictResult jMDictResult)
        {
            List<string> frequency = new() { Storage.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(jMDictResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                int freqResultsCount = freqResults.Count;
                for (int i = 0; i < freqResultsCount; i++)
                {
                    FrequencyEntry freqResult = freqResults[i];

                    if ((jMDictResult.Readings != null && jMDictResult.Readings.Contains(freqResult.Spelling))
                        || (jMDictResult.Readings == null && jMDictResult.PrimarySpelling == freqResult.Spelling))
                    //|| (jMnedictResult.KanaSpellings != null && jMnedictResult.KanaSpellings.Contains(freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }

                if (freqValue == int.MaxValue && jMDictResult.AlternativeSpellings != null)
                {
                    int alternativeSpellingsCount = jMDictResult.AlternativeSpellings.Count;
                    for (int i = 0; i < alternativeSpellingsCount; i++)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(jMDictResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry> alternativeSpellingFreqResults))
                        {
                            int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                            for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                            {
                                FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                                if (jMDictResult.Readings != null
                                    && jMDictResult.Readings.Contains(alternativeSpellingFreqResult.Spelling))
                                {
                                    if (freqValue > alternativeSpellingFreqResult.Frequency)
                                    {
                                        freqValue = alternativeSpellingFreqResult.Frequency;
                                        frequency = new List<string>
                                        {
                                            alternativeSpellingFreqResult.Frequency.ToString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            else if (jMDictResult.Readings != null)
            {
                int readingCount = jMDictResult.Readings.Count;
                for (int i = 0; i < readingCount; i++)
                {
                    string reading = jMDictResult.Readings[i];

                    if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading), out List<FrequencyEntry> readingFreqResults))
                    {
                        int readingFreqResultsCount = readingFreqResults.Count;
                        for (int j = 0; j < readingFreqResultsCount; j++)
                        {
                            FrequencyEntry readingFreqResult = readingFreqResults[j];

                            if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                                || (jMDictResult.AlternativeSpellings != null
                                    && jMDictResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                            //|| (jMDictResult.KanaSpellings != null && jMDictResult.KanaSpellings.Contains(readingFreqResults.Spelling))
                            {
                                if (freqValue > readingFreqResult.Frequency)
                                {
                                    freqValue = readingFreqResult.Frequency;
                                    frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                                }
                            }
                        }
                    }
                }
            }

            return frequency;
        }

        private static List<string> GetEpwingNazekaFreq(EpwingNazekaResult epwingNazekaResult)
        {
            List<string> frequency = new() { Storage.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingNazekaResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                int freqResultsCount = freqResults.Count;
                for (int i = 0; i < freqResultsCount; i++)
                {
                    FrequencyEntry freqResult = freqResults[i];

                    if ((epwingNazekaResult.Reading == freqResult.Spelling)
                        || (epwingNazekaResult.Reading == null && epwingNazekaResult.PrimarySpelling == freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }

                if (freqValue == int.MaxValue && epwingNazekaResult.AlternativeSpellings != null)
                {
                    int alternativeSpellingsCount = epwingNazekaResult.AlternativeSpellings.Count;
                    for (int i = 0; i < alternativeSpellingsCount; i++)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingNazekaResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry> alternativeSpellingFreqResults))
                        {
                            int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                            for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                            {
                                FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                                if (epwingNazekaResult.Reading == alternativeSpellingFreqResult.Spelling)
                                {
                                    if (freqValue > alternativeSpellingFreqResult.Frequency)
                                    {
                                        freqValue = alternativeSpellingFreqResult.Frequency;
                                        frequency = new List<string>
                                        {
                                            alternativeSpellingFreqResult.Frequency.ToString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            else if (epwingNazekaResult.Reading != null)
            {
                string reading = epwingNazekaResult.Reading;

                if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading), out List<FrequencyEntry> readingFreqResults))
                {
                    int readingFreqResultsCount = readingFreqResults.Count;
                    for (int j = 0; j < readingFreqResultsCount; j++)
                    {
                        FrequencyEntry readingFreqResult = readingFreqResults[j];

                        if (reading == readingFreqResult.Spelling && Kana.IsKatakana(reading)
                            || (epwingNazekaResult.AlternativeSpellings != null
                                && epwingNazekaResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                        {
                            if (freqValue > readingFreqResult.Frequency)
                            {
                                freqValue = readingFreqResult.Frequency;
                                frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                            }
                        }
                    }
                }
            }

            return frequency;
        }

        private static List<string> GetEpwingFreq(EpwingResult epwingResult)
        {
            List<string> frequency = new() { Storage.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                int freqResultsCount = freqResults.Count;
                for (int i = 0; i < freqResultsCount; i++)
                {
                    FrequencyEntry freqResult = freqResults[i];

                    if (epwingResult.Reading == freqResult.Spelling
                        || (string.IsNullOrEmpty(epwingResult.Reading)
                            && epwingResult.PrimarySpelling == freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }
            }

            else if (!string.IsNullOrEmpty(epwingResult.Reading)
                     && freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(epwingResult.Reading),
                         out List<FrequencyEntry> readingFreqResults))
            {
                int readingFreqResultsCount = readingFreqResults.Count;
                for (int i = 0; i < readingFreqResultsCount; i++)
                {
                    FrequencyEntry readingFreqResult = readingFreqResults[i];

                    if (epwingResult.Reading == readingFreqResult.Spelling && Kana.IsKatakana(epwingResult.Reading))
                    {
                        if (freqValue > readingFreqResult.Frequency)
                        {
                            freqValue = readingFreqResult.Frequency;
                            frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                        }
                    }
                }
            }

            return frequency;
        }

        private static List<string> GetCustomWordFreq(CustomWordEntry customWordResult)
        {
            List<string> frequency = new() { Storage.FakeFrequency };

            int freqValue = int.MaxValue;

            Storage.FreqDicts.TryGetValue(Storage.Frontend.CoreConfig.FrequencyListName, out Dictionary<string, List<FrequencyEntry>> freqDict);

            if (freqDict == null)
                return frequency;

            if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(customWordResult.PrimarySpelling),
                out List<FrequencyEntry> freqResults))
            {
                int freqResultsCount = freqResults.Count;
                for (int i = 0; i < freqResultsCount; i++)
                {
                    FrequencyEntry freqResult = freqResults[i];

                    if (customWordResult.Readings != null && customWordResult.Readings.Contains(freqResult.Spelling)
                        || (customWordResult.Readings == null
                            && customWordResult.PrimarySpelling == freqResult.Spelling))
                    {
                        if (freqValue > freqResult.Frequency)
                        {
                            freqValue = freqResult.Frequency;
                            frequency = new List<string> { freqResult.Frequency.ToString() };
                        }
                    }
                }

                if (freqValue == int.MaxValue && customWordResult.AlternativeSpellings != null)
                {
                    int alternativeSpellingsCount = customWordResult.AlternativeSpellings.Count;
                    for (int i = 0; i < alternativeSpellingsCount; i++)
                    {
                        if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(customWordResult.AlternativeSpellings[i]),
                            out List<FrequencyEntry> alternativeSpellingFreqResults))
                        {
                            int alternativeSpellingFreqResultsCount = alternativeSpellingFreqResults.Count;
                            for (int j = 0; j < alternativeSpellingFreqResultsCount; j++)
                            {
                                FrequencyEntry alternativeSpellingFreqResult = alternativeSpellingFreqResults[j];

                                if (customWordResult.Readings != null
                                    && customWordResult.Readings.Contains(alternativeSpellingFreqResult.Spelling)
)
                                {
                                    if (freqValue > alternativeSpellingFreqResult.Frequency)
                                    {
                                        freqValue = alternativeSpellingFreqResult.Frequency;
                                        frequency = new List<string>
                                        {
                                            alternativeSpellingFreqResult.Frequency.ToString()
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }

            else if (customWordResult.Readings != null)
            {
                int readingCount = customWordResult.Readings.Count;
                for (int i = 0; i < readingCount; i++)
                {
                    string reading = customWordResult.Readings[i];

                    if (freqDict.TryGetValue(Kana.KatakanaToHiraganaConverter(reading), out List<FrequencyEntry> readingFreqResults))
                    {
                        int readingFreqResultsCount = readingFreqResults.Count;
                        for (int j = 0; j < readingFreqResultsCount; j++)
                        {
                            FrequencyEntry readingFreqResult = readingFreqResults[j];

                            if ((reading == readingFreqResult.Spelling && Kana.IsKatakana(reading))
                                || (customWordResult.AlternativeSpellings != null
                                    && customWordResult.AlternativeSpellings.Contains(readingFreqResult.Spelling)))
                            //|| (customWordResult.KanaSpellings != null && customWordResult.KanaSpellings.Contains(readingFreqResults.Spelling))
                            {
                                if (freqValue > readingFreqResult.Frequency)
                                {
                                    freqValue = readingFreqResult.Frequency;
                                    frequency = new List<string> { readingFreqResult.Frequency.ToString() };
                                }
                            }
                        }
                    }
                }
            }

            return frequency;
        }


        private static string BuildJmdictDefinition(JMdictResult jMDictResult)
        {
            string separator = Storage.Frontend.CoreConfig.NewlineBetweenDefinitions ? "\n" : "";
            int count = 1;
            StringBuilder defResult = new();

            for (int i = 0; i < jMDictResult.Definitions.Count; i++)
            {
                if (jMDictResult.WordClasses.Any() && jMDictResult.WordClasses[i].Any())
                {
                    defResult.Append('(');
                    defResult.Append(string.Join(", ", jMDictResult.WordClasses[i]));
                    defResult.Append(") ");
                }

                if (jMDictResult.Definitions.Any())
                {
                    defResult.Append($"({count}) ");

                    if (jMDictResult.SpellingInfo.Any() && jMDictResult.SpellingInfo[i] != null)
                    {
                        defResult.Append('(');
                        defResult.Append(jMDictResult.SpellingInfo[i]);
                        defResult.Append(") ");
                    }

                    if (jMDictResult.MiscList.Any() && jMDictResult.MiscList[i].Any())
                    {
                        defResult.Append('(');
                        defResult.Append(string.Join(", ", jMDictResult.MiscList[i]));
                        defResult.Append(") ");
                    }

                    defResult.Append(string.Join("; ", jMDictResult.Definitions[i]) + " ");

                    if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any()
                        || jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                    {
                        defResult.Append("(only applies to ");

                        if (jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                        {
                            defResult.Append(string.Join("; ", jMDictResult.KRestrictions[i]));
                        }

                        if (jMDictResult.RRestrictions != null && jMDictResult.RRestrictions[i].Any())
                        {
                            if (jMDictResult.KRestrictions != null && jMDictResult.KRestrictions[i].Any())
                                defResult.Append("; ");

                            defResult.Append(string.Join("; ", jMDictResult.RRestrictions[i]));
                        }

                        defResult.Append(") ");
                    }

                    defResult.Append(separator);

                    ++count;
                }
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildJmnedictDefinition(JMnedictResult jMnedictResult)
        {
            int count = 1;
            StringBuilder defResult = new();

            if (jMnedictResult.NameTypes != null &&
                (jMnedictResult.NameTypes.Count > 1 || !jMnedictResult.NameTypes.Contains("unclass")))
            {
                for (int i = 0; i < jMnedictResult.NameTypes.Count; i++)
                {
                    defResult.Append('(');
                    defResult.Append(jMnedictResult.NameTypes[i]);
                    defResult.Append(") ");
                }
            }

            for (int i = 0; i < jMnedictResult.Definitions.Count; i++)
            {
                if (jMnedictResult.Definitions.Any())
                {
                    if (jMnedictResult.Definitions.Count > 0)
                        defResult.Append($"({count}) ");

                    defResult.Append($"{string.Join("; ", jMnedictResult.Definitions[i])} ");
                    ++count;
                }
            }

            return defResult.ToString();
        }

        private static string BuildEpwingDefinition(List<string> epwingDefinitions)
        {
            StringBuilder defResult = new();

            for (int i = 0; i < epwingDefinitions.Count; i++)
            {
                //var separator = Storage.Frontend.Storage.Frontend.CoreConfig.NewlineBetweenDefinitions ? "\n" : "; ";
                const string separator = "\n";
                defResult.Append(epwingDefinitions[i] + separator);
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildCustomWordDefinition(CustomWordEntry customWordResult)
        {
            string separator = Storage.Frontend.CoreConfig.NewlineBetweenDefinitions ? "\n" : "";
            int count = 1;
            StringBuilder defResult = new();

            if (customWordResult.WordClasses.Any())
            {
                string tempWordClass;
                if (customWordResult.WordClasses.Contains("adj-i"))
                    tempWordClass = "adjective";
                else if (customWordResult.WordClasses.Contains("v1"))
                    tempWordClass = "verb";
                else if (customWordResult.WordClasses.Contains("noun"))
                    tempWordClass = "noun";
                else
                    tempWordClass = "other";

                defResult.Append($"({tempWordClass}) ");
            }

            for (int i = 0; i < customWordResult.Definitions.Count; i++)
            {
                if (customWordResult.Definitions.Any())
                {
                    defResult.Append($"({count}) ");
                    defResult.Append(string.Join("; ", customWordResult.Definitions[i]) + " ");
                    defResult.Append(separator);

                    ++count;
                }
            }

            return defResult.ToString().Trim('\n');
        }

        private static string BuildCustomNameDefinition(CustomNameEntry customNameDictResult)
        {
            return $"({customNameDictResult.NameType.ToLower()}) {customNameDictResult.Reading}";
        }

        public static List<string> ProcessProcess(IntermediaryResult intermediaryResult)
        {
            StringBuilder deconj = new();
            bool first = true;

            int processListListCount = intermediaryResult.ProcessListList.Count;
            for (int i = 0; i < processListListCount; i++)
            {
                List<string> form = intermediaryResult.ProcessListList[i];

                StringBuilder formText = new();
                int added = 0;

                for (int j = form.Count - 1; j >= 0; j--)
                {
                    string info = form[j];

                    if (info == "")
                        continue;

                    if (info.StartsWith('(') && info.EndsWith(')') && j != 0)
                        continue;

                    if (added > 0)
                        formText.Append('→');

                    ++added;
                    formText.Append(info);
                }

                if (formText.Length != 0)
                {
                    if (first)
                        deconj.Append('～');
                    else
                        deconj.Append("; ");

                    deconj.Append(formText);
                }

                first = false;
            }

            return deconj.Length == 0 ? new List<string>() : new List<string> { deconj.ToString() };
        }
    }
}
