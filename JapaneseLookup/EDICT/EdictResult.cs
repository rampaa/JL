using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class EdictResult
    {
        public string Id { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<(List<string> Definitions, List<string> RRestrictions, List<string> KRestrictions)> DefinitionsList { get; set; }
        public List<string> Readings { get; set; }
        public List<string> OrthographyInfoList { get; set; } //e.g. Ateji, Reading
        public List<string> PriorityList { get; set; } // e.g. gai1
        public List<List<string>> WordClasses { get; set; } //e.g. noun +
        public List<string> RelatedTerms { get; set; } 
        public List<string> Antonyms { get; set; }
        public List<List<string>> TypeList { get; set; } // e.g. "martial arts"
        public List<List<string>> MiscList { get; set; } // e.g. "abbr" +
        public List<string> SpellingInfo { get; set; } // e.g. "often derog" +
        public List<string> Dialects { get; set; } // e.g. ksb
        public Dictionary<string, Frequency> FrequencyDict { get; set; }
        public List<string> KanaSpellings { get; set; }
        public string PrimarySpelling { get; set; }

        public EdictResult()
        {
            Id = null;
            AlternativeSpellings = new List<string>();
            DefinitionsList = new List<(List<string> Definitions, List<string> RRestrictions, List<string> KRestrictions)>();
            Readings = new List<string>();
            OrthographyInfoList = new List<string>();
            PriorityList = new List<string>();
            WordClasses = new List<List<string>>();
            RelatedTerms = new List<string>();
            Antonyms = new List<string>();
            TypeList = new List<List<string>>();
            MiscList = new List<List<string>>();
            SpellingInfo = new List<string>();
            Dialects = new List<string>();
            FrequencyDict = new Dictionary<string, Frequency>();
            KanaSpellings = new List<string>();
            PrimarySpelling = null;
        }
    }
}
