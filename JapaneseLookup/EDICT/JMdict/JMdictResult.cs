using System.Collections.Generic;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.EDICT.JMdict
{
    public class JMdictResult : IResult
    {
        public string Id { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<List<string>> Definitions { get; set; }
        public List<List<string>> RRestrictions { get; set; }
        public List<List<string>> KRestrictions { get; set; }
        public List<string> Readings { get; set; }
        public List<string> POrthographyInfoList { get; set; }
        public List<List<string>> AOrthographyInfoList { get; set; }
        public List<List<string>> ROrthographyInfoList { get; set; }
        public List<List<string>> WordClasses { get; set; } //e.g. noun +
        public List<List<string>> TypeList { get; set; } // e.g. "martial arts"
        public List<List<string>> MiscList { get; set; } // e.g. "abbr" +
        public List<string> SpellingInfo { get; set; } // e.g. "often derog" +
        public List<string> Dialects { get; set; } // e.g. ksb
        public Dictionary<string, int> FrequencyDict { get; set; }
        public List<string> KanaSpellings { get; set; }
        public string PrimarySpelling { get; set; }
        //public List<string> PriorityList { get; set; } // e.g. gai1
        //public List<string> Antonyms { get; set; }
        //public List<string> RelatedTerms { get; set; }

        public JMdictResult()
        {
            Id = null;
            Definitions = new List<List<string>>();
            RRestrictions = new List<List<string>>();
            KRestrictions = new List<List<string>>();
            Readings = new List<string>();
            AlternativeSpellings = new List<string>();
            POrthographyInfoList = new List<string>();
            AOrthographyInfoList = new List<List<string>>();
            ROrthographyInfoList = new List<List<string>>();
            WordClasses = new List<List<string>>();
            TypeList = new List<List<string>>();
            MiscList = new List<List<string>>();
            SpellingInfo = new List<string>();
            Dialects = new List<string>();
            //FrequencyDict = new Dictionary<string, Frequency>();
            KanaSpellings = new List<string>();
            PrimarySpelling = null;
            //PriorityList = new List<string>();
            //RelatedTerms = new List<string>();
            //Antonyms = new List<string>();
        }
    }
}
