namespace JapaneseLookup.CustomDict
{
    public class CustomNameEntry : IResult
    {
        public string PrimarySpelling { get; set; }
        public string Reading { get; set; }
        public string NameType { get; set; }

        public CustomNameEntry() : this(null, null, null)
        {
        }

        public CustomNameEntry(string primarySpelling, string reading, string nameType)
        {
            PrimarySpelling = primarySpelling;
            Reading = reading;
            NameType = nameType;
        }
    }
}