using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.Frequency
{
    public class FrequencyEntry
    {
        public string Spelling { get; set; }
        public int Frequency { get; set; }

        public FrequencyEntry(string spelling, int frequency)
        {
            Spelling = spelling;
            Frequency = frequency;
        }
    }
}
