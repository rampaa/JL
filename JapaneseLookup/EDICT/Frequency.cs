using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class Frequency
    {
        public int FrequencyRank { get; set; }
        public double FrequencyPPM { get; set; }

        public Frequency (int frequencyRank, double frequencyPPM)
        {
            FrequencyRank = frequencyRank;
            FrequencyPPM = frequencyPPM;
        }
    }
}