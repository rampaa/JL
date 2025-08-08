//using System.Collections.Frozen;
//using JL.Core.Utilities;

//namespace JL.Core.Dicts.KanjiComposition;

//internal static class KanjiCompositionUtils
//{
//    public static IDictionary<string, string[]> KanjiCompositionDict { get; private set; } = new Dictionary<string, string[]>(87627, StringComparer.Ordinal);

//    public static async Task InitializeKanjiCompositionDict()
//    {
//        string filePath = Path.Join(Utils.ResourcesPath, "ids.txt");
//        if (File.Exists(filePath))
//        {
//            string[] lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);
//            foreach (string line in lines)
//            {
//                string[] lParts = line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
//                if (lParts.Length < 3)
//                {
//                    continue;
//                }

//                List<string> components = new(lParts.Length - 2);
//                for (int j = 2; j < lParts.Length; j++)
//                {
//                    string currentPart = lParts[j];

//                    ReadOnlySpan<char> currentPartSpan = currentPart.AsSpan();
//                    int endIndex = currentPartSpan.IndexOf('[');
//                    if (endIndex < 0)
//                    {
//                        components.Add(currentPart);
//                    }
//                    else if (currentPartSpan.Contains('J'))
//                    {
//                        components.Add(currentPart[..endIndex]);
//                    }
//                }

//                if (components.Count > 0)
//                {
//                    KanjiCompositionDict[lParts[1].GetPooledString()] = components.ToArray();
//                }
//            }

//            KanjiCompositionDict = KanjiCompositionDict.ToFrozenDictionary(StringComparer.Ordinal);
//            KanjiCompositionDBManager.CreateDB();
//            KanjiCompositionDBManager.InsertRecordsToDB(KanjiCompositionDict);
//        }
//    }
//}
