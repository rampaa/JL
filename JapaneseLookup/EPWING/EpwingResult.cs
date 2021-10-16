using System.Collections.Generic;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.EPWING
{
    public class EpwingResult : IResult
    {
        public List<List<string>> Definitions { get; set; }

        public List<string> Readings { get; set; }

        public List<List<string>> WordClasses { get; set; } //e.g. noun +

        public string PrimarySpelling { get; set; }

        public EpwingResult()
        {
            Definitions = new List<List<string>>();
            Readings = new List<string>();
            WordClasses = new List<List<string>>();
            PrimarySpelling = null;
        }
    }
}