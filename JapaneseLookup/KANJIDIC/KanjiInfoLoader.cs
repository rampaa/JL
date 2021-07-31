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
                while (edictXml.ReadToFollowing("literal"))
                {
                    ReadCharacter(edictXml);
                }
            }
            else
            {
                MessageBox.Show("Couldn't find kanjidic2.xml. Please download it by clicking the \"Update KANJIDIC\" button.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }
        private static void ReadCharacter(XmlReader kanjiDicXml)
        {
            string key = kanjiDicXml.ReadString();

            //if (EDICT.JMdictLoader.jMdictDictionary.ContainsKey(key))
            //    return;

            KanjiResult entry = new();

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
