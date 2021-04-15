using System.Collections.Generic;

namespace JapaneseLookup.EDICT
{
    class Results
    {
        public string Id { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<string> Definitions { get; set; }
        public List<string> Readings { get; set; }
        public List<string> OrthographyInfo { get; set; }
        public List<string> FrequencyList { get; set; }
        public List<string> WordClasses { get; set; }
        public List<string> RelatedTerms { get; set; }
        public List<string> Antonyms { get; set; }
        public List<string> FieldInfoList { get; set; }
        public List<string> MiscList { get; set; }
        public string SpellingInfo { get; set; }
        public List<string> Dialects { get; set; }

        public Results()
        {
            Id = null;
            AlternativeSpellings = new List<string>();
            Definitions = new List<string>();
            Readings = new List<string>();
            OrthographyInfo = new List<string>();
            FrequencyList = new List<string>();
            WordClasses = new List<string>();
            RelatedTerms = new List<string>();
            Antonyms = new List<string>();
            FieldInfoList = new List<string>();
            MiscList = new List<string>();
            SpellingInfo = null;
            Dialects = new List<string>();
        }
    }
}
