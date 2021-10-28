using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.Frequency
{
    public class FrequencyEntry
    {
        public string Spelling { get; init; }
        public int Frequency { get; init; }

        public FrequencyEntry(string spelling, int frequency)
        {
            Spelling = spelling;
            Frequency = frequency;
        }
    }
}
