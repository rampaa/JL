using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace JapaneseLookup.KANJIDIC
{
    public static class KanjiInfoLoader
    {
        public static readonly Dictionary<string, KanjiResult> KanjiDictionary = new();

        public static void Load()
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml")))
            {
                using XmlTextReader edictXml = new(Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml"))
                {
                    DtdProcessing = DtdProcessing.Parse,
                    WhitespaceHandling = WhitespaceHandling.None,
                    EntityHandling = EntityHandling.ExpandCharEntities
                };

                Dictionary<string, string> kanjiCompositionDictionary = new();

                if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/ids.txt")))
                {
                    foreach (string line in File.ReadLines(
                        Path.Join(ConfigManager.ApplicationPath, "Resources/ids.txt")))
                    {
                        string[] lParts = line.Split("\t");

                        if (lParts.Length == 3)
                        {
                            int endIndex = lParts[2].IndexOf("[");
                            if (endIndex == -1)
                                kanjiCompositionDictionary.Add(lParts[1], lParts[2]);
                            else
                                kanjiCompositionDictionary.Add(lParts[1], lParts[2].Substring(0, endIndex));
                        }

                        else if (lParts.Length > 3)
                        {
                            for (int i = 2; i < lParts.Length; i++)
                            {
                                if (lParts[i].Contains("J"))
                                {
                                    int endIndex = lParts[i].IndexOf("[", StringComparison.Ordinal);
                                    if (endIndex != -1)
                                    {
                                        kanjiCompositionDictionary.Add(lParts[1], lParts[i].Substring(0, endIndex));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                while (edictXml.ReadToFollowing("literal"))
                {
                    ReadCharacter(edictXml, kanjiCompositionDictionary);
                }
            }
            else
            {
                MessageBox.Show(
                    "Couldn't find kanjidic2.xml. Please download it by clicking the \"Update KANJIDIC\" button.", "",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
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

            KanjiDictionary.Add(key, entry);
        }
    }
}