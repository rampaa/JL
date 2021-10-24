using System.Collections.Generic;
using System.IO;
using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.CustomDict
{
    public static class CustomNameLoader
    {
        public static void Load(string customNameDictPath)
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, customNameDictPath)))
            {
                foreach (string line in File.ReadLines(
                    Path.Join(ConfigManager.ApplicationPath, customNameDictPath)))
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

            var customNameDictionary = ConfigManager.Dicts[DictType.CustomNameDictionary].Contents;

            if (customNameDictionary.TryGetValue(spelling, out var entry))
            {
                if (!entry.Contains(newNameEntry))
                {
                    entry.Add(newNameEntry);
                }
            }

            else
            {
                customNameDictionary.Add(spelling, new List<IResult> { newNameEntry });
            }
        }
    }
}