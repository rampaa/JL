using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using JapaneseLookup.Abstract;
using JapaneseLookup.Dicts;

namespace JapaneseLookup.EDICT.KANJIDIC
{
    public static class KanjiInfoLoader
    {
        public static async Task Load(string dictPath)
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, dictPath)))
            {
                using XmlTextReader edictXml = new(Path.Join(ConfigManager.ApplicationPath, dictPath))
                {
                    DtdProcessing = DtdProcessing.Parse,
                    WhitespaceHandling = WhitespaceHandling.None,
                    EntityHandling = EntityHandling.ExpandCharEntities
                };

                Dictionary<string, string> kanjiCompositionDictionary = new();
                if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/ids.txt")))
                {
                    var lines = await File.ReadAllLinesAsync(Path.Join(ConfigManager.ApplicationPath, "Resources/ids.txt")).ConfigureAwait(false);

                    foreach (string line in lines)
                    {
                        string[] lParts = line.Split("\t");

                        if (lParts.Length == 3)
                        {
                            int endIndex = lParts[2].IndexOf("[", StringComparison.Ordinal);
                            if (endIndex == -1)
                                kanjiCompositionDictionary.Add(lParts[1], lParts[2]);
                            else
                                kanjiCompositionDictionary.Add(lParts[1], lParts[2][..endIndex]);
                        }

                        else if (lParts.Length > 3)
                        {
                            for (int i = 2; i < lParts.Length; i++)
                            {
                                if (lParts[i].Contains('J'))
                                {
                                    int endIndex = lParts[i].IndexOf("[", StringComparison.Ordinal);
                                    if (endIndex != -1)
                                    {
                                        kanjiCompositionDictionary.Add(lParts[1], lParts[i][..endIndex]);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                ConfigManager.Dicts[DictType.Kanjidic].Contents = new Dictionary<string, List<IResult>>();
                while (edictXml.ReadToFollowing("literal"))
                {
                    ReadCharacter(edictXml, kanjiCompositionDictionary);
                }

                ConfigManager.Dicts[DictType.Kanjidic].Contents.TrimExcess();
            }

            else if (MessageBox.Show(
                    "Couldn't find kanjidic2.xml. Would you like to download it now?", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes,
                    MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                await ResourceUpdater.UpdateResource(ConfigManager.Dicts[DictType.Kanjidic].Path,
                    new Uri("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz"),
                    DictType.Kanjidic.ToString(), false, false).ConfigureAwait(false);
                await Load(ConfigManager.Dicts[DictType.Kanjidic].Path).ConfigureAwait(false);
            }

            else
            {
                ConfigManager.Dicts[DictType.Kanjidic].Active = false;
                ConfigManager.SerializeDicts();
            }
        }

        private static void ReadCharacter(XmlReader kanjiDicXml, Dictionary<string, string> kanjiCompositionDictionary)
        {
            string key = kanjiDicXml.ReadString();

            KanjiResult entry = new();

            if (kanjiCompositionDictionary.TryGetValue(key, out string composition))
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
                                entry.Meanings.Add(kanjiDicXml.ReadString());
                            break;

                        case "nanori":
                            entry.Nanori.Add(kanjiDicXml.ReadString());
                            break;

                        case "reading":
                            switch (kanjiDicXml.GetAttribute("r_type"))
                            {
                                case "ja_on":
                                    entry.OnReadings.Add(kanjiDicXml.ReadString());
                                    break;

                                case "ja_kun":
                                    entry.KunReadings.Add(kanjiDicXml.ReadString());
                                    break;
                            }

                            break;
                    }
                }
            }

            if (!entry.Nanori.Any())
                entry.Nanori = null;
            if (!entry.Meanings.Any())
                entry.Meanings = null;
            if (!entry.OnReadings.Any())
                entry.OnReadings = null;
            if (!entry.KunReadings.Any())
                entry.KunReadings = null;

            ConfigManager.Dicts[DictType.Kanjidic].Contents.Add(key, new List<IResult> { entry });
        }
    }
}