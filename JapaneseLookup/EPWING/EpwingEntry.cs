using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JapaneseLookup.EPWING
{
    public class EpwingEntry
    {
        public string Expression { get; init; }
        public string Reading { get; init; }
        public string DefinitionTags { get; init; }
        public List<string> Rules { get; init; }
        public int Score { get; init; }
        public List<string> Glossary { get; init; }
        public int Sequence { get; init; }
        public string TermTags { get; init; }

        public EpwingEntry(List<JsonElement> jsonElement)
        {
            Expression = jsonElement[0].ToString() == "" ? null : jsonElement[0].ToString();
            Reading = jsonElement[1].ToString() == "" ? null : jsonElement[1].ToString();
            DefinitionTags = jsonElement[2].ToString();

            Rules = jsonElement[3].ToString().Split(" ").ToList();

            jsonElement[4].TryGetInt32(out int score);
            Score = score;

            Glossary = jsonElement[5].ToString()[2..^2]
                .Split("\\n", StringSplitOptions.TrimEntries).ToList();

            jsonElement[6].TryGetInt32(out int sequence);
            Sequence = sequence;

            TermTags = jsonElement[7].ToString();
        }
    }
}