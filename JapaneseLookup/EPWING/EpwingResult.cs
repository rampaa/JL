using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.EPWING
{
    public class EpwingResult : IResult
    {
        public List<string> Definitions { get; set; }
        public string Reading { get; set; }
        public List<string> WordClasses { get; set; }
        public string PrimarySpelling { get; init; }

        //public string KanaSpelling { get; set; }
        //public string DefinitionTags { get; init; }
        //public int Score { get; init; }
        //public int Sequence { get; init; }
        //public string TermTags { get; init; }

        public EpwingResult(List<JsonElement> jsonElement)
        {
            PrimarySpelling = jsonElement[0].ToString();
            Reading = jsonElement[1].ToString();
            //DefinitionTags = jsonElement[2].ToString();

            WordClasses = jsonElement[3].ToString().Split(" ").ToList();

            if (!WordClasses.Any())
                WordClasses = null;

            //jsonElement[4].TryGetInt32(out int score);
            //Score = score;

            Definitions = jsonElement[5].ToString()[2..^2]
                .Split("\\n", StringSplitOptions.TrimEntries).ToList();

            if (!Definitions.Any())
                Definitions = null;

            //jsonElement[6].TryGetInt32(out int sequence);
            //Sequence = sequence;

            //TermTags = jsonElement[7].ToString();
        }

        public EpwingResult(string primarySpelling, string reading, List<string> definitions, List<string> wordClasses)
        {
            Definitions = definitions;
            Reading = reading;
            WordClasses = wordClasses;
            PrimarySpelling = primarySpelling;
            // KanaSpelling = null;
        }
    }
}