namespace JL.Core.Dicts.CustomDict
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

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            CustomWordEntry customWordEntryObj = (obj as CustomWordEntry)!;

            return PrimarySpelling == customWordEntryObj.PrimarySpelling
                   && customWordEntryObj.AlternativeSpellings.SequenceEqual(AlternativeSpellings)
                   && customWordEntryObj.Readings.SequenceEqual(Readings)
                   && customWordEntryObj.Definitions.SequenceEqual(Definitions)
                   && customWordEntryObj.WordClasses.SequenceEqual(WordClasses);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 37 + PrimarySpelling.GetHashCode();

                foreach (string spelling in AlternativeSpellings)
                    hash = hash * 37 + spelling.GetHashCode();

                foreach (string readings in Readings)
                    hash = hash * 37 + readings.GetHashCode();

                foreach (string definition in Definitions)
                    hash = hash * 37 + definition.GetHashCode();

                foreach (string wordClass in WordClasses)
                    hash = hash * 37 + wordClass.GetHashCode();

                return hash;
            }
        }
    }
}
