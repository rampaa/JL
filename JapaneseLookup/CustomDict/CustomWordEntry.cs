﻿using System.Collections.Generic;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.CustomDict
{
    public class CustomWordEntry : IResult
    {
        public string PrimarySpelling { get; set; }
        public List<string> AlternativeSpellings { get; set; }
        public List<string> Readings { get; set; }
        public List<string> Definitions { get; set; }
        public List<string> WordClasses { get; set; }

        public CustomWordEntry(string primarySpelling, List<string> alternativeSpellings, List<string> readings,
            List<string> definitions, List<string> wordClasses)
        {
            PrimarySpelling = primarySpelling;
            AlternativeSpellings = alternativeSpellings;
            Readings = readings;
            Definitions = definitions;
            WordClasses = wordClasses;
        }
    }
}