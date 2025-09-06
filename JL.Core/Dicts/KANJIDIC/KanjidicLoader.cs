using System.Collections.Frozen;
using System.Diagnostics;
using System.Xml;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KANJIDIC;

internal static class KanjidicLoader
{
    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
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

            dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
        }

        else
        {
            if (dict.Updating)
            {
                return;
            }

            dict.Updating = true;
            if (Utils.Frontend.ShowYesNoDialog(
                "Couldn't find kanjidic2.xml. Would you like to download it now?",
                "Download KANJIDIC2?"))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.Kanjidic), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    await Load(dict).ConfigureAwait(false);
                }
            }
            else
            {
                dict.Active = false;
            }

            dict.Updating = false;
        }
    }

    private static async Task ReadCharacter(XmlReader xmlReader, IDictionary<string, IList<IDictRecord>> kanjidicDictionary)
    {
        string key = (await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false)).GetPooledString();

        byte grade = 0;
        byte strokeCount = 0;
        int frequency = 0;
        List<string> definitionList = [];
        List<string> nanoriReadingList = [];
        List<string> onReadingList = [];
        List<string> kunReadingList = [];
        List<string> radicalNameList = [];

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
                        grade = (byte)xmlReader.ReadElementContentAsInt();
                        break;

                    case "stroke_count":
                        strokeCount = (byte)xmlReader.ReadElementContentAsInt();
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

                    case "rad_name":
                        radicalNameList.Add((await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false)).GetPooledString());
                        break;

                    // Old JLPT, has 4 levels instead of 5
                    //case "jlpt":
                    //    jlpt = xmlReader.ReadElementContentAsInt();
                    //    break;

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

        string[]? definitions = definitionList.TrimToArray();
        string[]? onReadings = onReadingList.TrimToArray();
        string[]? kunReadings = kunReadingList.TrimToArray();
        string[]? nanoriReadings = nanoriReadingList.TrimToArray();
        string[]? radicalNames = radicalNameList.TrimToArray();

        KanjidicRecord entry = new(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);

        kanjidicDictionary[key] = [entry];
    }
}
