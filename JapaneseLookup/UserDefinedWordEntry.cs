using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    class UserDefinedWordEntry
    {
        public string PrimarySpelling { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<string> Readings { get; set; }
        public List<List<string>> Definitions { get; set; }
        public UserDefinedWordEntry()
        {
            PrimarySpelling = null;
            AlternativeSpellings = new List<string>();
            Readings = new List<string>();
            Definitions = new List<List<string>>();
        }
    }
}
