using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace JapaneseLookup.EDICT
{
    //TODO: Refactor
    class JMdictLoader
    {
        // public static List<JMdictEntry> jMdict;
        public static Dictionary<String, List<Results>> jMdictDictionary;
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

        private static void DictionaryBuilder(JMdictEntry entry)
        {
            // entry (k_ele*, r_ele+, sense+)
            // k_ele (keb, ke_inf*, ke_pri*)
            // r_ele (reb, re_restr*, re_inf*, re_pri*)
            // sense (stagk*, stagr*, pos*, xref*, ant*, field*, misc*, s_inf*, dial*, gloss*)

            Dictionary<string, Results> resultList = new();
            List<string> alternativeSpellings = new();

            foreach (KEle kEle in entry.KEleList)
            {
                Results result = new();
                string key = kEle.Keb;

                alternativeSpellings.Add(key);

                result.OrthographyInfo = kEle.KeInfList;
                result.FrequencyList = kEle.KePriList;

                foreach (REle rEle in entry.REleList)
                {
                    if (!rEle.ReRestrList.Any() || rEle.ReRestrList.Contains(key))
                        result.Readings.Add(rEle.Reb);
                }

                foreach (Sense sense in entry.SenseList)
                {
                    if ((!sense.StagKList.Any() && !sense.StagRList.Any()) || sense.StagKList.Contains(key))
                    {
                        result.Definitions.AddRange(sense.GlossList);
                        result.WordClasses.AddRange(sense.PosList);
                        result.RelatedTerms.AddRange(sense.XRefList);
                        result.Antonyms.AddRange(sense.AntList);
                        result.FieldInfoList.AddRange(sense.FieldList);
                        result.MiscList.AddRange(sense.MiscList);
                        result.Dialects.AddRange(sense.DialList);
                        if (sense.SInf != null)
                            result.SpellingInfo = sense.SInf;
                    }
                }
                resultList.Add(key, result);
            }

            foreach (KeyValuePair<string, Results> item in resultList)
            {
                foreach (string s in alternativeSpellings)
                {
                    if (item.Key != s)
                    {
                        item.Value.AlternativeSpellings.Add(s);
                    }
                }
            }

            foreach (REle rEle in entry.REleList)
            {
                Results result = new();
                string key = rEle.Reb;

                //result.Readings.Add(rEle.Reb);

                if (rEle.ReRestrList.Any())
                    result.AlternativeSpellings = rEle.ReRestrList;
                else
                    result.AlternativeSpellings = alternativeSpellings;

                foreach (Sense sense in entry.SenseList)
                {
                    // if((!sense.StagList.Any() && !sense.StagRList.Any()) || sense.StagRList.Contains(key))
                    result.Definitions.AddRange(sense.GlossList);
                    result.WordClasses.AddRange(sense.PosList);
                    result.RelatedTerms.AddRange(sense.XRefList);
                    result.Antonyms.AddRange(sense.AntList);
                    result.FieldInfoList.AddRange(sense.FieldList);
                    result.MiscList.AddRange(sense.MiscList);
                    result.Dialects.AddRange(sense.DialList);
                    if (sense.SInf != null)
                        result.SpellingInfo = sense.SInf;
                }
                resultList.Add(key, result);
            }

            foreach (KeyValuePair<String, Results> rl in resultList)
            {
                rl.Value.Id = entry.Id;

                if (jMdictDictionary.TryGetValue(rl.Key, out List<Results> tempList))
                {
                    tempList.Add(rl.Value);
                    jMdictDictionary[rl.Key] = tempList;
                }
                else
                {
                    tempList = new();
                    tempList.Add(rl.Value);
                    jMdictDictionary.Add(rl.Key, tempList);
                }
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
            DictionaryBuilder(entry);

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
