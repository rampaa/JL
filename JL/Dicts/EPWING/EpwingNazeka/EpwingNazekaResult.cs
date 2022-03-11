using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JL.Dicts.EPWING.EpwingNazeka
{
    internal class EpwingNazekaResult : IResult
    {
        public string PrimarySpelling { get; set; }
        public string Reading { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<string> Definitions { get; set; }

        public EpwingNazekaResult(string primarySpelling, string reading, List<string> alternativeSpellings, List<string> definitions)
        {
            PrimarySpelling = primarySpelling;
            Reading = reading;
            AlternativeSpellings = alternativeSpellings;
            Definitions = definitions;
        }
    }
}
