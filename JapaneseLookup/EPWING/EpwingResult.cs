using System.Collections.Generic;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.EPWING
{
    public class EpwingResult : IResult
    {
        public List<string> Definitions { get; init; }
        public string Reading { get; init; }
        public List<string> WordClasses { get; init; } //e.g. noun +
        public string PrimarySpelling { get; init; }
        public string KanaSpelling { get; set; }

        public EpwingResult()
        {
            Definitions = new List<string>();
            Reading = null;
            WordClasses = new List<string>();
            PrimarySpelling = null;
            KanaSpelling = null;
        }
    }
}