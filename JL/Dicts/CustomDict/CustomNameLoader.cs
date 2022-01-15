using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JL.Dicts.CustomDict
{
    public static class CustomNameLoader
    {
        public static async Task Load(string customNameDictPath)
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, customNameDictPath)))
            {
                string[] lines = await File.ReadAllLinesAsync(Path.Join(ConfigManager.ApplicationPath, customNameDictPath))
                    .ConfigureAwait(false);
                foreach (string line in lines)
                {
                    string[] lParts = line.Split("\t");

                    if (lParts.Length == 3)
                    {
                        AddToDictionary(lParts[0].Trim(), lParts[1].Trim(), lParts[2].Trim());
                    }
                }
            }
        }

        public static void AddToDictionary(string spelling, string reading, string nameType)
        {
            CustomNameEntry newNameEntry = new(spelling, reading, nameType);

            Dictionary<string, List<IResult>> customNameDictionary = Storage.Dicts[DictType.CustomNameDictionary].Contents;

            if (customNameDictionary.TryGetValue(Kana.KatakanaToHiraganaConverter(spelling), out List<IResult> entry))
            {
                if (!entry.Contains(newNameEntry))
                {
                    entry.Add(newNameEntry);
                }
            }

            else
            {
                customNameDictionary.Add(Kana.KatakanaToHiraganaConverter(spelling),
                    new List<IResult> { newNameEntry });
            }
        }
    }
}
