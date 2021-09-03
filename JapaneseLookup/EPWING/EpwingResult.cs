using System.Collections.Generic;

namespace JapaneseLookup.EPWING
{
    public class EpwingResult : IResult
    {
        public List<List<string>> Definitions { get; set; }

        public List<string> Readings { get; set; }

         public List<List<string>> WordClasses { get; set; } //e.g. noun +

        // public Dictionary<string, int> FrequencyDict { get; set; }

        public string PrimarySpelling { get; set; }

        public EpwingResult()
        {
            Definitions = new List<List<string>>();
            Readings = new List<string>();
             WordClasses = new List<List<string>>();
            //FrequencyDict = new Dictionary<string, Frequency>();
            PrimarySpelling = null;
        }
    }
}