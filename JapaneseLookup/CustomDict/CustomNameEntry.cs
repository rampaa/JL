using System.Diagnostics;
using JapaneseLookup.Abstract;

namespace JapaneseLookup.CustomDict
{
    public class CustomNameEntry : IResult
    {
        public string PrimarySpelling { get; }
        public string Reading { get; }
        public string NameType { get; }

        public CustomNameEntry(string primarySpelling, string reading, string nameType)
        {
            PrimarySpelling = primarySpelling;
            Reading = reading;
            NameType = nameType;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            CustomNameEntry customNameEntryObj = obj as CustomNameEntry;

            Debug.Assert(customNameEntryObj != null, nameof(customNameEntryObj) + " != null");
            return PrimarySpelling == customNameEntryObj.PrimarySpelling
                   && Reading == customNameEntryObj.Reading
                   && NameType == customNameEntryObj.NameType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 37 + PrimarySpelling?.GetHashCode() ?? 0;
                hash = hash * 37 + Reading?.GetHashCode() ?? 0;
                hash = hash * 37 + NameType?.GetHashCode() ?? 0;

                return hash;
            }
        }
    }
}