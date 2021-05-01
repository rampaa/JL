using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace JapaneseLookup.EDICT
{
    class JMdictLoader
    {
        // public static List<JMdictEntry> jMdict;
        public static Dictionary<string, List<Results>> jMdictDictionary;
        public static void Loader()
        {
            // jMdict = new List<JMdictEntry>();
            jMdictDictionary = new Dictionary<string, List<Results>>();
            using XmlTextReader jMDictXML = new("../net5.0-windows/Resources/JMdict.xml");
            jMDictXML.DtdProcessing = DtdProcessing.Parse;
            jMDictXML.WhitespaceHandling = WhitespaceHandling.None;
            jMDictXML.EntityHandling = EntityHandling.ExpandCharEntities;
            while (jMDictXML.ReadToFollowing("entry"))
            {
                EntryReader(jMDictXML);
            }
        }
        private static void EntryReader(XmlTextReader jMDictXML)
        {
            JMdictEntry entry = new();
            while (jMDictXML.Read())
            {
                if (jMDictXML.Name == "entry" && jMDictXML.NodeType == XmlNodeType.EndElement)
                    break;

                if (jMDictXML.NodeType == XmlNodeType.Element)
                {
                    switch (jMDictXML.Name)
                    {
                        case "ent_seq":
                            entry.Id = jMDictXML.ReadString();
                            break;

                        case "k_ele":
                            KEleReader(jMDictXML, entry);
                            break;

                        case "r_ele":
                            REleReader(jMDictXML, entry);
                            break;

                        case "sense":
                            SenseReader(jMDictXML, entry);
                            break;
                    }
                }
            }
            JMDictBuilder.DictionaryBuilder(entry, jMdictDictionary);
        }

        private static void KEleReader(XmlTextReader jMDictXML, JMdictEntry entry)
        {
            KEle kEle = new();
            while (jMDictXML.Read())
            {
                if (jMDictXML.Name == "k_ele" && jMDictXML.NodeType == XmlNodeType.EndElement)
                    break;

                if (jMDictXML.NodeType == XmlNodeType.Element)
                {
                    switch (jMDictXML.Name)
                    {
                        case "keb":
                            kEle.Keb = jMDictXML.ReadString();
                            break;

                        case "ke_inf":
                            kEle.KeInfList.Add(EntityReader(jMDictXML));
                            break;

                        case "ke_pri":
                            kEle.KePriList.Add(jMDictXML.ReadString());
                            break;
                    }
                }
            }
            entry.KEleList.Add(kEle);
        }

        private static void REleReader(XmlTextReader jMDictXML, JMdictEntry entry)
        {
            REle rEle = new();
            while (jMDictXML.Read())
            {
                if (jMDictXML.Name == "r_ele" && jMDictXML.NodeType == XmlNodeType.EndElement)
                    break;

                if (jMDictXML.NodeType == XmlNodeType.Element)
                {
                    switch (jMDictXML.Name)
                    {
                        case "reb":
                            rEle.Reb = jMDictXML.ReadString();
                            break;

                        case "re_restr":
                            rEle.ReRestrList.Add(jMDictXML.ReadString());
                            break;

                        case "re_inf":
                            rEle.ReInfList.Add(EntityReader(jMDictXML));
                            break;

                        case "re_pri":
                            rEle.ReInfList.Add(jMDictXML.ReadString());
                            break;
                    }
                }
            }
            entry.REleList.Add(rEle);
        }

        private static void SenseReader(XmlTextReader jMDictXML, JMdictEntry entry)
        {
            Sense sense = new();
            while (jMDictXML.Read())
            {
                if (jMDictXML.Name == "sense" && jMDictXML.NodeType == XmlNodeType.EndElement)
                    break;

                if (jMDictXML.NodeType == XmlNodeType.Element)
                {
                    switch (jMDictXML.Name)
                    {
                        case "stagk":
                            sense.StagKList.Add(jMDictXML.ReadString());
                            break;

                        case "stagr":
                            sense.StagRList.Add(jMDictXML.ReadString());
                            break;

                        case "pos":
                            sense.PosList.Add(EntityReader(jMDictXML));
                            break;

                        case "xref":
                            sense.XRefList.Add(jMDictXML.ReadString());
                            break;

                        case "ant":
                            sense.AntList.Add(jMDictXML.ReadString());
                            break;

                        case "field":
                            sense.FieldList.Add(EntityReader(jMDictXML));
                            break;

                        case "misc":
                            sense.MiscList.Add(EntityReader(jMDictXML));
                            break;

                        case "s_inf":
                            sense.SInf = jMDictXML.ReadString();
                            break;

                        case "dial":
                            sense.DialList.Add(EntityReader(jMDictXML));
                            break;

                        case "gloss":
                            sense.GlossList.Add(jMDictXML.ReadString());
                            break;
                    }
                }
            }
            entry.SenseList.Add(sense);
        }

        private static string EntityReader(XmlTextReader jMDictXML)
        {
            jMDictXML.Read();
            if (jMDictXML.NodeType == XmlNodeType.EntityReference)
            {
                jMDictXML.ResolveEntity();
                return jMDictXML.Name;
            }
            else return null;
        }
    }
}
