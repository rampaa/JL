using System.Xml;

namespace JL.Core.Dicts.EDICT.KANJIDIC;

public static class KanjiInfoLoader
{
    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            using XmlTextReader edictXml = new(dict.Path)
            {
                DtdProcessing = DtdProcessing.Parse,
                WhitespaceHandling = WhitespaceHandling.None,
                EntityHandling = EntityHandling.ExpandCharEntities
            };

            Dictionary<string, string> kanjiCompositionDictionary = new();
            if (File.Exists($"{Storage.ResourcesPath}/ids.txt"))
            {
                string[] lines = await File
                    .ReadAllLinesAsync($"{Storage.ResourcesPath}/ids.txt")
                    .ConfigureAwait(false);

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] lParts = lines[i].Split("\t");

                    if (lParts.Length == 3)
                    {
                        int endIndex = lParts[2].IndexOf("[", StringComparison.Ordinal);

                        kanjiCompositionDictionary.Add(lParts[1],
                            endIndex == -1 ? lParts[2] : lParts[2][..endIndex]);
                    }

                    else if (lParts.Length > 3)
                    {
                        for (int j = 2; j < lParts.Length; j++)
                        {
                            if (lParts[j].Contains('J'))
                            {
                                int endIndex = lParts[j].IndexOf("[", StringComparison.Ordinal);
                                if (endIndex != -1)
                                {
                                    kanjiCompositionDictionary.Add(lParts[1], lParts[j][..endIndex]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            dict.Contents = new Dictionary<string, List<IResult>>();
            while (edictXml.ReadToFollowing("literal"))
            {
                ReadCharacter(edictXml, kanjiCompositionDictionary, dict);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find kanjidic2.xml. Would you like to download it now?",
                     ""))
        {
            await ResourceUpdater.UpdateResource(dict.Path,
                Storage.KanjidicUrl,
                DictType.Kanjidic.ToString(), false, false).ConfigureAwait(false);
            await Load(dict).ConfigureAwait(false);
        }

        else
        {
            dict.Active = false;
        }
    }

    private static void ReadCharacter(XmlReader kanjiDicXml, Dictionary<string, string> kanjiCompositionDictionary, Dict dict)
    {
        string key = kanjiDicXml.ReadString();

        KanjiResult entry = new();

        if (kanjiCompositionDictionary.TryGetValue(key, out string? composition))
            entry.Composition = composition;

        while (kanjiDicXml.Read())
        {
            if (kanjiDicXml.Name == "character" && kanjiDicXml.NodeType == XmlNodeType.EndElement)
                break;

            if (kanjiDicXml.NodeType == XmlNodeType.Element)
            {
                switch (kanjiDicXml.Name)
                {
                    case "grade":
                        //kanjiDicXml.ReadElementContentAsInt();
                        entry.Grade = int.Parse(kanjiDicXml.ReadString());
                        break;

                    case "stroke_count":
                        entry.StrokeCount = int.Parse(kanjiDicXml.ReadString());
                        break;

                    case "freq":
                        entry.Frequency = int.Parse(kanjiDicXml.ReadString());
                        break;

                    case "meaning":
                        if (!kanjiDicXml.HasAttributes)
                            entry.Meanings!.Add(kanjiDicXml.ReadString());
                        break;

                    case "nanori":
                        entry.Nanori!.Add(kanjiDicXml.ReadString());
                        break;

                    case "reading":
                        switch (kanjiDicXml.GetAttribute("r_type"))
                        {
                            case "ja_on":
                                entry.OnReadings!.Add(kanjiDicXml.ReadString());
                                break;

                            case "ja_kun":
                                entry.KunReadings!.Add(kanjiDicXml.ReadString());
                                break;
                        }
                        break;
                }
            }
        }

        if (!entry.Nanori!.Any() || entry.Nanori!.All(l => !l.Any()))
            entry.Nanori = null;
        else
            entry.Nanori!.TrimExcess();

        if (!entry.Meanings!.Any() || entry.Meanings!.All(l => !l.Any()))
            entry.Meanings = null;
        else
            entry.Meanings!.TrimExcess();

        if (!entry.OnReadings!.Any() || entry.OnReadings!.All(l => !l.Any()))
            entry.OnReadings = null;
        else
            entry.OnReadings!.TrimExcess();

        if (!entry.KunReadings!.Any() || entry.KunReadings!.All(l => !l.Any()))
            entry.KunReadings = null;
        else
            entry.KunReadings!.TrimExcess();

        dict.Contents.Add(key, new List<IResult> { entry });
    }
}
