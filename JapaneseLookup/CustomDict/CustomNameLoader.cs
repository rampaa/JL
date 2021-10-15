using System.Collections.Generic;
using System.IO;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.CustomDict
{
    public static class CustomNameLoader
    {
        public static void Load()
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/custom_names.txt")))
            {
                foreach (string line in File.ReadLines(
                    Path.Join(ConfigManager.ApplicationPath, "Resources/custom_names.txt")))
                {
                    string[] lParts = line.Split("\t");
                    AddToDictionary(lParts[0], lParts[1], lParts[2]);
                }

                ConfigManager.Dicts[DictType.CustomNameDictionary].Contents.TrimExcess();
            }
        }

        public static void AddToDictionary(string spelling, string reading, string definition)
        {
            CustomNameEntry newNameEntry = new(spelling, reading, definition);

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