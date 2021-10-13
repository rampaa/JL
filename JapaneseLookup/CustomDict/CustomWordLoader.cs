using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.CustomDict
{
    public static class CustomWordLoader
    {
        public static void Load()
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/custom_words.txt")))
            {
                foreach (string line in File.ReadLines(
                    Path.Join(ConfigManager.ApplicationPath, "Resources/custom_words.txt")))
                {
                    string[] lParts = line.Split("\t");

                    string[] spellings = lParts[0].Split(';');
                    List<string> readings = lParts[1].Split(';').ToList();
                    List<string> definitions = lParts[2].Split(';').ToList();
                    string wordClass = lParts[3];

                    AddToDictionary(spellings, readings, definitions, wordClass);
                }

                ConfigManager.Dicts[DictType.CustomWordDictionary].Contents.TrimExcess();
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
                    wordClass.Add("aux-v");
                    wordClass.Add("iv");
                    wordClass.Add("v-unspec");
                    wordClass.Add("v1");
                    wordClass.Add("v1-s");
                    wordClass.Add("v2a-s");
                    wordClass.Add("v2b-k");
                    wordClass.Add("v2b-s");
                    wordClass.Add("v2d-k");
                    wordClass.Add("v2d-s");
                    wordClass.Add("v2g-k");
                    wordClass.Add("v2g-s");
                    wordClass.Add("v2h-k");
                    wordClass.Add("v2h-s");
                    wordClass.Add("v2k-k");
                    wordClass.Add("v2k-s");
                    wordClass.Add("v2m-k");
                    wordClass.Add("v2m-s");
                    wordClass.Add("v2n-s");
                    wordClass.Add("v2r-k");
                    wordClass.Add("v2r-s");
                    wordClass.Add("v2s-s");
                    wordClass.Add("v2t-k");
                    wordClass.Add("v2t-s");
                    wordClass.Add("v2w-s");
                    wordClass.Add("v2y-k");
                    wordClass.Add("v2y-s");
                    wordClass.Add("v2z-s");
                    wordClass.Add("v4b");
                    wordClass.Add("v4g");
                    wordClass.Add("v4h");
                    wordClass.Add("v4k");
                    wordClass.Add("v4m");
                    wordClass.Add("v4n");
                    wordClass.Add("v4r");
                    wordClass.Add("v4s");
                    wordClass.Add("v4t");
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
                    wordClass.Add("v5uru");
                    wordClass.Add("vi");
                    wordClass.Add("vk");
                    wordClass.Add("vn");
                    wordClass.Add("vr");
                    wordClass.Add("vs-c");
                    wordClass.Add("vs-i");
                    wordClass.Add("vs-s");
                    wordClass.Add("vt");
                    wordClass.Add("vz");
                }
                else if (rawWordClass == "Adjective")
                {
                    wordClass.Add("adj-f");
                    wordClass.Add("adj-i");
                    wordClass.Add("adj-ix");
                    wordClass.Add("adj-kari");
                    wordClass.Add("adj-ku");
                    wordClass.Add("adj-na");
                    wordClass.Add("adj-nari");
                    wordClass.Add("adj-no");
                    wordClass.Add("adj-pn");
                    wordClass.Add("adj-shiku");
                    wordClass.Add("adj-t");
                    wordClass.Add("aux-adj");
                }
                else if (rawWordClass == "Name")
                {
                    wordClass.Add("noun");
                }
                else
                {
                    wordClass.Add("other");
                }

                CustomWordEntry newWordEntry = new(spelling, alternativeSpellings, readings, definitions, wordClass);

                var customWordDictionary = ConfigManager.Dicts[DictType.CustomWordDictionary].Contents;

                if (customWordDictionary.TryGetValue(spelling, out var result))
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
                    customWordDictionary.Add(spelling, new List<IResult>() { newWordEntry });
                }
            }
        }
    }
}