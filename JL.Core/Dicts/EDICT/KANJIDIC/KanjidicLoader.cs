using System.Xml;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EDICT.KANJIDIC;

internal static class KanjidicLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
        if (File.Exists(fullPath))
        {
            XmlReaderSettings xmlReaderSettings = new()
            {
                Async = true,
                DtdProcessing = DtdProcessing.Parse,
                IgnoreWhitespace = true
            };

            using (XmlReader xmlReader = XmlReader.Create(fullPath, xmlReaderSettings))
            {
                while (xmlReader.ReadToFollowing("literal"))
                {
                    await ReadCharacter(xmlReader, dict.Contents).ConfigureAwait(false);
                }
            }

            foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
            {
                dict.Contents[key] = recordList.ToArray();
            }

            dict.Contents.TrimExcess();
        }

        else if (Utils.Frontend.ShowYesNoDialog(
                     "Couldn't find kanjidic2.xml. Would you like to download it now?",
                     "Download KANJIDIC2?"))
        {
            bool downloaded = await ResourceUpdater.UpdateResource(fullPath,
                DictUtils.s_kanjidicUrl,
                DictType.Kanjidic.ToString(), false, false).ConfigureAwait(false);

            if (downloaded)
            {
                await Load(dict).ConfigureAwait(false);
            }
        }

        else
        {
            dict.Active = false;
        }
    }

    private static async Task ReadCharacter(XmlReader xmlReader, Dictionary<string, IList<IDictRecord>> kanjidicDictionary)
    {
        string key = await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false);

        int grade = -1;
        int strokeCount = 0;
        int frequency = 0;
        List<string> definitionList = new();
        List<string> nanoriReadingList = new();
        List<string> onReadingList = new();
        List<string> kunReadingList = new();

        while (!xmlReader.EOF)
        {
            if (xmlReader is { Name: "character", NodeType: XmlNodeType.EndElement })
            {
                break;
            }

            if (xmlReader.NodeType is XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "grade":
                        grade = xmlReader.ReadElementContentAsInt();
                        break;

                    case "stroke_count":
                        strokeCount = xmlReader.ReadElementContentAsInt();
                        break;

                    case "freq":
                        frequency = xmlReader.ReadElementContentAsInt();
                        break;

                    case "meaning":
                        // English definition
                        if (!xmlReader.HasAttributes)
                        {
                            definitionList.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        }
                        else
                        {
                            _ = await xmlReader.ReadAsync().ConfigureAwait(false);
                        }

                        break;

                    case "nanori":
                        nanoriReadingList.Add((await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false)).GetPooledString());
                        break;

                    case "reading":
                        switch (xmlReader.GetAttribute("r_type"))
                        {
                            case "ja_on":
                                onReadingList.Add((await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false)).GetPooledString());
                                break;

                            case "ja_kun":
                                kunReadingList.Add((await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false)).GetPooledString());
                                break;

                            default:
                                _ = await xmlReader.ReadAsync().ConfigureAwait(false);
                                break;
                        }

                        break;

                    default:
                        _ = await xmlReader.ReadAsync().ConfigureAwait(false);
                        break;
                }
            }

            else
            {
                _ = await xmlReader.ReadAsync().ConfigureAwait(false);
            }
        }

        string[]? definitions = definitionList.TrimStringListToStringArray();
        string[]? onReadings = onReadingList.TrimStringListToStringArray();
        string[]? kunReadings = kunReadingList.TrimStringListToStringArray();
        string[]? nanoriReadings = nanoriReadingList.TrimStringListToStringArray();

        KanjidicRecord entry = new(definitions, onReadings, kunReadings, nanoriReadings, strokeCount, grade, frequency);

        kanjidicDictionary.Add(key, new List<IDictRecord> { entry });
    }
}
