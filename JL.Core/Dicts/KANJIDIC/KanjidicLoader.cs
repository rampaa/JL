using System.Collections.Frozen;
using System.Diagnostics;
using System.Xml;
using JL.Core.Frontend;
using JL.Core.Utilities;

namespace JL.Core.Dicts.KANJIDIC;

internal static class KanjidicLoader
{
    // 2022/05/11: 13108, 2023/12/16: 13108, 2024/02/02 13108
    public const int Size = 13108;

    public static async Task Load(Dict dict)
    {
        string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            // ReSharper disable once UseAwaitUsing
            using (FileStream fileStream = new(fullPath, FileStreamOptionsPresets.s_syncRead64KBufferFso))
            {
                XmlReaderSettings xmlReaderSettings = new()
                {
                    DtdProcessing = DtdProcessing.Parse,
                    IgnoreWhitespace = true
                };

                using XmlReader xmlReader = XmlReader.Create(fileStream, xmlReaderSettings);
                while (xmlReader.ReadToFollowing("literal"))
                {
                    (string key, KanjidicRecord record) = ReadCharacter(xmlReader);
                    dict.Contents[key] = [record];
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
            if (await FrontendManager.Frontend.ShowYesNoDialogAsync(
                "Couldn't find kanjidic2.xml. Would you like to download it now?",
                "Download KANJIDIC2?").ConfigureAwait(false))
            {
                Uri? uri = dict.Url;
                Debug.Assert(uri is not null);

                bool downloaded = await ResourceUpdater.DownloadBuiltInDict(fullPath,
                    uri,
                    nameof(DictType.Kanjidic), false, false).ConfigureAwait(false);

                if (downloaded)
                {
                    try
                    {
                        await Load(dict).ConfigureAwait(false);
                    }
                    finally
                    {
                        dict.Updating = false;
                    }
                }
                else
                {
                    dict.Updating = false;
                }
            }
            else
            {
                dict.Active = false;
                dict.Updating = false;
            }
        }
    }

    public static (string key, KanjidicRecord record) ReadCharacter(XmlReader xmlReader)
    {
        string key = xmlReader.ReadElementContentAsString().GetPooledString();

        byte grade = 0;
        byte strokeCount = 0;
        bool strokeCountRead = false;
        int frequency = 0;
        List<string> definitionList = [];
        List<string> onReadingList = [];
        List<string> kunReadingList = [];
        List<string>? nanoriReadingList = null;
        List<string>? radicalNameList = null;

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
                        if (!strokeCountRead)
                        {
                            strokeCount = (byte)xmlReader.ReadElementContentAsInt();
                            strokeCountRead = true;
                        }
                        else
                        {
                            xmlReader.Skip();
                        }

                        break;

                    case "freq":
                        frequency = xmlReader.ReadElementContentAsInt();
                        break;

                    case "meaning":
                        // English definition
                        if (!xmlReader.HasAttributes || xmlReader.GetAttribute("m_lang") is "en")
                        {
                            definitionList.Add(xmlReader.ReadElementContentAsString());
                        }
                        else
                        {
                            xmlReader.Skip();
                        }

                        break;

                    case "nanori":
                        nanoriReadingList ??= [];
                        nanoriReadingList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    case "reading":
                        switch (xmlReader.GetAttribute("r_type"))
                        {
                            case "ja_on":
                                onReadingList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                                break;

                            case "ja_kun":
                                kunReadingList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                                break;

                            default:
                                xmlReader.Skip();
                                break;
                        }

                        break;

                    case "rad_name":
                        radicalNameList ??= [];
                        radicalNameList.Add(xmlReader.ReadElementContentAsString().GetPooledString());
                        break;

                    // Old JLPT, has 4 levels instead of 5
                    //case "jlpt":
                    //    jlpt = xmlReader.ReadElementContentAsInt();
                    //    break;

                    case "misc":
                    case "reading_meaning":
                    case "rmgroup":
                        _ = xmlReader.Read();
                        break;

                    default:
                        xmlReader.Skip();
                        break;
                }
            }

            else
            {
                _ = xmlReader.Read();
            }
        }

        string[]? definitions = definitionList.TrimToArray();
        string[]? onReadings = onReadingList.TrimToArray();
        string[]? kunReadings = kunReadingList.TrimToArray();
        string[]? nanoriReadings = nanoriReadingList?.ToArray();
        string[]? radicalNames = radicalNameList?.ToArray();

        KanjidicRecord record = new(definitions, onReadings, kunReadings, nanoriReadings, radicalNames, strokeCount, grade, frequency);

        return (key, record);
    }
}
