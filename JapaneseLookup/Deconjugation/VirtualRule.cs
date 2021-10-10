namespace JapaneseLookup.Deconjugation
{
    public readonly struct VirtualRule
    {
        public VirtualRule(string decEnd, string conEnd, string decTag,
            string conTag, string detail)
        {
            DecEnd = decEnd;
            ConEnd = conEnd;
            DecTag = decTag;
            ConTag = conTag;
            Detail = detail;
        }

        public string DecEnd { get; }
        public string ConEnd { get; }
        public string DecTag { get; }
        public string ConTag { get; }
        public string Detail { get; }
    }
}