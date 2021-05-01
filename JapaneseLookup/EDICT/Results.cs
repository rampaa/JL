using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class Results
    {
        public string Id { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<(List<string> Definitions, List<string> RRestrictions, List<string> KRestrictions)> DefinitionsList { get; set; }
        public List<string> Readings { get; set; }
        public List<string> OrthographyInfoList { get; set; }
        public List<string> PriorityList { get; set; }
        public List<List<string>> WordClasses { get; set; }
        public List<string> RelatedTerms { get; set; }
        public List<string> Antonyms { get; set; }
        public List<string> FieldInfoList { get; set; }
        public List<List<string>> MiscList { get; set; }
        public List<string> SpellingInfo { get; set; }
        public List<string> Dialects { get; set; }
        public Dictionary<string, Frequency> FrequencyDict { get; set; }
        public List<string> KanaSpellings { get; set; }
        public string PrimarySpelling { get; set; }

        public Results()
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
            FieldInfoList = new List<string>();
            MiscList = new List<List<string>>();
            SpellingInfo = new List<string>();
            Dialects = new List<string>();
            FrequencyDict = new Dictionary<string, Frequency>();
            KanaSpellings = new List<string>();
            PrimarySpelling = null;
        }
    }
}
