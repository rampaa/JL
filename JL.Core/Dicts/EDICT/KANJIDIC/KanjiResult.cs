namespace JL.Core.Dicts.EDICT.KANJIDIC
{
    public class KanjiResult : IResult
    {
        public List<string> Meanings { get; set; }
        public List<string> OnReadings { get; set; }
        public List<string> KunReadings { get; set; }
        public List<string> Nanori { get; set; }
        public int StrokeCount { get; set; }
        public int Grade { get; set; }
        public string Composition { get; set; }
        public int Frequency { get; set; }

        public KanjiResult()
        {
            Meanings = new List<string>();
            OnReadings = new List<string>();
            KunReadings = new List<string>();
            Nanori = new List<string>();
        }
    }
}
