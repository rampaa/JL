using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JapaneseLookup.CustomDict
{
    public static class CustomWordLoader
    {
        public static async Task Load(string customWordDictPath)
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, customWordDictPath)))
            {
                var lines = await File.ReadAllLinesAsync(Path.Join(ConfigManager.ApplicationPath, customWordDictPath)).ConfigureAwait(false);
                foreach (string line in lines)
                {
                    string[] lParts = line.Split("\t");

                    if (lParts.Length == 4)
                    {
                        string[] spellings = lParts[0].Split(';').Select(s => s.Trim()).ToArray();
                        List<string> readings = lParts[1].Split(';').Select(r => r.Trim()).ToList();
                        List<string> definitions = lParts[2].Split(';').Select(d => d.Trim()).ToList();
                        string wordClass = lParts[3].Trim();

                        AddToDictionary(spellings, readings, definitions, wordClass);
                    }
                }
            }
        }

        public static void AddToDictionary(string[] spellings, List<string> readings, List<string> definitions,
            string rawWordClass)
        {
            foreach (string spelling in spellings)
            {
                List<string> alternativeSpellings = spellings.ToList();
                alternativeSpellings.Remove(spelling);

                List<string> wordClass = new();

                if (rawWordClass == "Verb")
                {
                    wordClass.Add("v1");
                    wordClass.Add("v1-s");
                    wordClass.Add("v5aru");
                    wordClass.Add("v5b");
                    wordClass.Add("v5g");
                    wordClass.Add("v5k");
                    wordClass.Add("v5k-s");
                    wordClass.Add("v5m");
                    wordClass.Add("v5n");
                    wordClass.Add("v5r");
                    wordClass.Add("v5r-i");
                    wordClass.Add("v5s");
                    wordClass.Add("v5t");
                    wordClass.Add("v5u");
                    wordClass.Add("v5u-s");
                    wordClass.Add("vi");
                    wordClass.Add("vk");
                    wordClass.Add("vs");
                    wordClass.Add("vz");
                }
                else if (rawWordClass == "Adjective")
                {
                    wordClass.Add("adj-i");
                    wordClass.Add("adj-na");
                }
                else if (rawWordClass == "Noun")
                {
                    wordClass.Add("noun");
                }
                else
                {
                    wordClass.Add("other");
                }

                CustomWordEntry newWordEntry = new(spelling, alternativeSpellings, readings, definitions, wordClass);

                var customWordDictionary = Storage.Dicts[DictType.CustomWordDictionary].Contents;

                if (customWordDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(spelling), out var result))
                {
                    if (result.Contains(newWordEntry))
                    {
                        break;
                    }
                    else
                    {
                        result.Add(newWordEntry);
                    }
                }
                else
                {
                    customWordDictionary.Add(Kana.KatakanaToHiraganaConverter(spelling), new List<IResult> { newWordEntry });
                }
            }
        }
    }
}