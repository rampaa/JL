using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JapaneseLookup.Dicts.CustomDict
{
    public class CustomWordEntry : IResult
    {
        public string PrimarySpelling { get; }
        public List<string> AlternativeSpellings { get; }
        public List<string> Readings { get; }
        public List<string> Definitions { get; }
        public List<string> WordClasses { get; }

        public CustomWordEntry(string primarySpelling, List<string> alternativeSpellings, List<string> readings,
            List<string> definitions, List<string> wordClasses)
        {
            PrimarySpelling = primarySpelling;
            AlternativeSpellings = alternativeSpellings;
            Readings = readings;
            Definitions = definitions;
            WordClasses = wordClasses;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            CustomWordEntry customWordEntryObj = obj as CustomWordEntry;

            Debug.Assert(customWordEntryObj != null, nameof(customWordEntryObj) + " != null");
            return PrimarySpelling == customWordEntryObj.PrimarySpelling
                   && (customWordEntryObj.AlternativeSpellings?.SequenceEqual(AlternativeSpellings) ?? false)
                   && (customWordEntryObj.Readings?.SequenceEqual(Readings) ?? false)
                   && (customWordEntryObj.Definitions?.SequenceEqual(Definitions) ?? false)
                   && (customWordEntryObj.WordClasses?.SequenceEqual(WordClasses) ?? false);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 37 + PrimarySpelling?.GetHashCode() ?? 0;

                if (AlternativeSpellings == null)
                    hash *= 37;

                else
                    foreach (string spelling in AlternativeSpellings)
                        hash = hash * 37 + spelling?.GetHashCode() ?? 0;

                if (Readings == null)
                    hash *= 37;

                else
                    foreach (string readings in Readings)
                        hash = hash * 37 + readings?.GetHashCode() ?? 0;

                if (Definitions == null)
                    hash *= 37;
                else
                    foreach (string definition in Definitions)
                        hash = hash * 37 + definition?.GetHashCode() ?? 0;

                if (WordClasses == null)
                    hash *= 37;
                else
                    foreach (string wordClass in WordClasses)
                        hash = hash * 37 + wordClass?.GetHashCode() ?? 0;

                return hash;
            }
        }
    }
}
