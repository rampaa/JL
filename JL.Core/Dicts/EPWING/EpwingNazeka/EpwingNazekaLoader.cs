using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.EpwingNazeka;

internal static class EpwingNazekaLoader
{
    public static async Task Load(Dict dict)
    {
        try
        {
            List<object>? jsonObjects;

            FileStream openStream = File.OpenRead(dict.Path);
            await using (openStream.ConfigureAwait(false))
            {
                jsonObjects = await JsonSerializer.DeserializeAsync<List<object>>(openStream)
                    .ConfigureAwait(false);
            }

            Dictionary<string, List<IResult>> nazekaEpwingDict = dict.Contents;

            foreach (JsonElement jsonObj in jsonObjects!.Skip(1))
            {
                string reading = jsonObj.GetProperty("r").ToString();

                List<string>? spellings = jsonObj.GetProperty("s").ToString().TrimStart('[').TrimEnd(']')
                    .Split("\",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(select => select.Trim('\n', ' ', '"')).ToList();

                List<string>? definitions = jsonObj.GetProperty("l").ToString().TrimStart('[').TrimEnd(']')
                    .Split("\",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(select => select.Trim('\n', ' ', '"')).ToList();

                if (!definitions.Any())
                {
                    definitions = null;
                }

                if (spellings.Count == 1 && spellings[0] == "")
                {
                    spellings = null;
                }

                if (spellings != null)
                {
                    string primarySpelling = spellings[0];

                    List<string>? alternativeSpellings = spellings.ToList();
                    alternativeSpellings.RemoveAt(0);

                    string key = Kana.KatakanaToHiraganaConverter(reading);

                    EpwingNazekaResult tempResult = new(primarySpelling, reading, alternativeSpellings,
                        definitions);

                    if (nazekaEpwingDict.TryGetValue(key, out List<IResult>? result))
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

                        if (!IsValidEpwingResultForDictType(tempResult, dict))
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

                    if (!IsValidEpwingResultForDictType(tempResult, dict))
                        continue;

                    if (nazekaEpwingDict.TryGetValue(key, out List<IResult>? result))
                    {
                        result.Add(tempResult);
                    }
                    else
                    {
                        nazekaEpwingDict.Add(key, new List<IResult> { tempResult });
                    }
                }
            }

            nazekaEpwingDict.TrimExcess();
        }

        catch (Exception e)
        {
            Utils.Logger.Fatal(e, "Error loading nazeka dict");
            throw;
        }
    }

    private static bool IsValidEpwingResultForDictType(EpwingNazekaResult result, Dict dict)
    {
        string[] badCharacters = { "�", "(", "=", "＝", "［", "〔", "「", "『", "（", "【", "[" };

        foreach (string badCharacter in badCharacters)
        {
            if (result.PrimarySpelling.Contains(badCharacter))
                return false;
        }

        if (!Storage.JapaneseRegex.IsMatch(result.PrimarySpelling))
            return false;

        // TODO
        switch (dict.Type)
        {
            case DictType.KenkyuushaNazeka:
            case DictType.DaijirinNazeka:
            case DictType.ShinmeikaiNazeka:
            case DictType.NonspecificNazeka:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(dict), dict.Name, null);
        }

        return true;
    }
}
