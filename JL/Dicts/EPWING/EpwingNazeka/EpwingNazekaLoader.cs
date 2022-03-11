using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JL.Utilities;

namespace JL.Dicts.EPWING.EpwingNazeka
{
    internal class EpwingNazekaLoader
    {
        public static async Task Load(DictType dictType, string dictPath)
        {
            await using FileStream openStream = File.OpenRead(dictPath);
            List<object> jsonObjects = await JsonSerializer.DeserializeAsync<List<object>>(openStream)
                .ConfigureAwait(false);

            Dictionary<string, List<IResult>> nazekaEpwingDict = Storage.Dicts[dictType].Contents;

            foreach (JsonElement jsonObj in jsonObjects.Skip(1))
            {
                string reading = jsonObj.GetProperty("r").ToString();
                List<string> spellings = jsonObj.GetProperty("s").ToString().TrimStart('[').TrimEnd(']').Split("\",", StringSplitOptions.RemoveEmptyEntries).Select(select => select.Trim('\n', ' ', '"')).ToList();
                List<string> definitions = jsonObj.GetProperty("l").ToString().TrimStart('[').TrimEnd(']').Split("\",", StringSplitOptions.RemoveEmptyEntries).Select(select => select.TrimStart('\n', ' ', '"')).ToList();

                if (spellings.Count == 1 && spellings[0] == "")
                {
                    spellings = null;
                }

                if (spellings != null)
                {
                    string primarySpelling = spellings[0];

                    List<string> alternativeSpellings = spellings.ToList();
                    alternativeSpellings.RemoveAt(0);

                    string key = Kana.KatakanaToHiraganaConverter(reading);

                    EpwingNazekaResult tempResult = new(primarySpelling, reading, alternativeSpellings, definitions);

                    if (nazekaEpwingDict.TryGetValue(key, out List<IResult> result))
                    {
                        result.Add(tempResult);
                    }

                    else
                    {
                        nazekaEpwingDict.Add(key, new List<IResult> { tempResult });
                    }

                    for (int i = 0; i < spellings.Count; i++)
                    {
                        primarySpelling = spellings[i];

                        alternativeSpellings = spellings.ToList();
                        alternativeSpellings.RemoveAt(i);

                        if (!alternativeSpellings.Any())
                            alternativeSpellings = null;

                        key = Kana.KatakanaToHiraganaConverter(primarySpelling);

                        tempResult = new(primarySpelling, reading, alternativeSpellings, definitions);

                        if (!IsValidEpwingResultForDictType(tempResult, dictType))
                            continue;

                        if (nazekaEpwingDict.TryGetValue(key, out result))
                        {
                            result.Add(tempResult);
                        }

                        else
                        {
                            nazekaEpwingDict.Add(key, new List<IResult> { tempResult });
                        }
                    }
                }

                else
                {
                    string primarySpelling = reading;
                    string key = Kana.KatakanaToHiraganaConverter(primarySpelling);

                    EpwingNazekaResult tempResult = new(primarySpelling, null, null, definitions);

                    if (!IsValidEpwingResultForDictType(tempResult, dictType))
                        continue;

                    if (nazekaEpwingDict.TryGetValue(key, out List<IResult> result))
                    {
                        result.Add(tempResult);
                    }

                    else
                    {
                        nazekaEpwingDict.Add(key, new List<IResult> { tempResult });
                    }
                }
            }
        }

        private static bool IsValidEpwingResultForDictType(EpwingNazekaResult result, DictType dictType)
        {
            string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

            foreach (string badCharacter in badCharacters)
            {
                if (result.PrimarySpelling.Contains(badCharacter))
                    return false;
            }

            if (!MainWindowUtilities.JapaneseRegex.IsMatch(result.PrimarySpelling))
                return false;


            // TODO
            switch (dictType)
            {
                case DictType.KenkyuushaNazeka:
                    break;
                case DictType.DaijirinNazeka:
                    break;
                case DictType.ShinmeikaiNazeka:
                    break;
            }

            return true;
        }
    }
}
