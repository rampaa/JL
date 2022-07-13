using System.Xml;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.KANJIDIC;

public static class KanjiInfoLoader
{
    public static async Task Load(Dict dict)
    {
        if (File.Exists(dict.Path))
        {
            XmlReaderSettings xmlReaderSettings = new()
            {
                Async = true,
                DtdProcessing = DtdProcessing.Parse,
                IgnoreWhitespace = true
            };

            using XmlReader xmlReader = XmlReader.Create(dict.Path, xmlReaderSettings);

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
            while (xmlReader.ReadToFollowing("literal"))
            {
                await ReadCharacter(xmlReader, kanjiCompositionDictionary, dict).ConfigureAwait(false);
            }

            dict.Contents.TrimExcess();
        }

        else if (Storage.Frontend.ShowYesNoDialog(
                     "Couldn't find kanjidic2.xml. Would you like to download it now?",
                     "Download KANJIDIC2?"))
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

    private static async Task ReadCharacter(XmlReader xmlReader, Dictionary<string, string> kanjiCompositionDictionary, Dict dict)
    {
        string key = xmlReader.ReadElementContentAsString();

        KanjiResult entry = new();

        if (kanjiCompositionDictionary.TryGetValue(key, out string? composition))
            entry.Composition = composition;

        while (!xmlReader.EOF)
        {
            if (xmlReader.Name == "character" && xmlReader.NodeType == XmlNodeType.EndElement)
                break;

            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "grade":
                        entry.Grade = xmlReader.ReadElementContentAsInt();
                        break;

                    case "stroke_count":
                        entry.StrokeCount = xmlReader.ReadElementContentAsInt();
                        break;

                    case "freq":
                        entry.Frequency = xmlReader.ReadElementContentAsInt();
                        break;

                    case "meaning":
                        // English definition
                        if (!xmlReader.HasAttributes)
                        {
                            entry.Meanings!.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        }
                        else
                        {
                            await xmlReader.ReadAsync().ConfigureAwait(false);
                        }
                        break;

                    case "nanori":
                        entry.Nanori!.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        break;

                    case "reading":
                        switch (xmlReader.GetAttribute("r_type"))
                        {
                            case "ja_on":
                                entry.OnReadings!.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                break;

                            case "ja_kun":
                                entry.KunReadings!.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                break;

                            default:
                                await xmlReader.ReadAsync().ConfigureAwait(false);
                                break;
                        }
                        break;

                    default:
                        await xmlReader.ReadAsync().ConfigureAwait(false);
                        break;
                }
            }

            else
            {
                await xmlReader.ReadAsync().ConfigureAwait(false);
            }
        }

        entry.Nanori = Utils.TrimStringList(entry.Nanori!);
        entry.Nanori = Utils.TrimStringList(entry.Meanings!);
        entry.Nanori = Utils.TrimStringList(entry.OnReadings!);
        entry.Nanori = Utils.TrimStringList(entry.KunReadings!);

        dict.Contents.Add(key, new List<IResult> { entry });
    }
}
