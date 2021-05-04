using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JapaneseLookup.EPWING
{
    class EpwingEntry
    {
        public string Expression { get; set; }
        public string Reading { get; set; }
        public string DefinitionTags { get; set; }
        public string Rules { get; set; }
        public int Score { get; set; }
        public List<string> Glosssary { get; set; }
        public int Sequence { get; set; }
        public string TermTags { get; set; }

        public EpwingEntry(List<JsonElement> jsonElement)
        {
            Expression = jsonElement[0].ToString();
            Reading = jsonElement[1].ToString();
            DefinitionTags = jsonElement[2].GetRawText();

            Rules = jsonElement[3].GetRawText();

            jsonElement[4].TryGetInt32(out int score);
            Score = score;

            Glosssary = jsonElement[5].ToString()[2..^2].Split("\\n", StringSplitOptions.TrimEntries).ToList();

            jsonElement[6].TryGetInt32(out int sequence);
            Sequence = sequence;

            TermTags = jsonElement[7].GetRawText();
        }
    }
}
