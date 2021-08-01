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
    class KanjiInfoLoader
    {
        public static Dictionary<string, KanjiResult> kanjiDictionary = new();
        public static void Load()
        {
            if (File.Exists(Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml")))
            {
                using XmlTextReader edictXml = new(Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml"));

                edictXml.DtdProcessing = DtdProcessing.Parse;
                edictXml.WhitespaceHandling = WhitespaceHandling.None;
                edictXml.EntityHandling = EntityHandling.ExpandCharEntities;

                Dictionary<string, string> kanjiCompositionDictionary = new();

                foreach (string line in File.ReadLines(Path.Join(ConfigManager.ApplicationPath, "Resources/ids.txt")))
                {
                    string[] lParts = line.Split("\t");

                    if (lParts.Length == 3) kanjiCompositionDictionary.Add(lParts[1], lParts[2]);

                    else if (lParts.Length > 3)
                    {
                        int japaneseCompositionIndex = -1;
                        for(int i = 2; i < lParts.Length; i++)
                        {
                            if (lParts[i].Contains("J"))
                            {
                                japaneseCompositionIndex = i;
                                break;
                            }
                        }
                        if (japaneseCompositionIndex != -1) kanjiCompositionDictionary.Add(lParts[1], lParts[japaneseCompositionIndex]);
                    }
                }

                while (edictXml.ReadToFollowing("literal"))
                {
                    ReadCharacter(edictXml, kanjiCompositionDictionary);
                }
            }
            else
            {
                MessageBox.Show("Couldn't find kanjidic2.xml. Please download it by clicking the \"Update KANJIDIC\" button.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
                            entry.Grade = kanjiDicXml.ReadElementContentAsInt();
                            break;

                        case "stroke_count":
                            entry.StrokeCount = kanjiDicXml.ReadElementContentAsInt();
                            break;

                        case "freq":
                            entry.Frequency = kanjiDicXml.ReadElementContentAsInt();
                            break;

                        case "meaning":
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
            kanjiDictionary.Add(key, entry);
        }
    }
}
